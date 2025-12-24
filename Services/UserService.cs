using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Identity;
using SciSubmit.Models.Content;
using SciSubmit.Models.Account;
using SciSubmit.Controllers;
using System.Security.Cryptography;
using System.Text;

namespace SciSubmit.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hash password using SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SciSubmitSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Verify password
        private bool VerifyPassword(string password, string passwordHash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == passwordHash;
        }

        // Authentication
        public async Task<User?> AuthenticateAsync(string emailOrPhone, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => 
                    (u.Email == emailOrPhone || u.PhoneNumber == emailOrPhone) && 
                    u.IsActive);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null)
            {
                return false; // Email already exists
            }

            // Create new user
            var user = new User
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = HashPassword(model.Password),
                FullName = model.Email.Split('@')[0], // Use email prefix as default name
                Affiliation = null,
                Role = Models.Enums.UserRole.Guest,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !VerifyPassword(currentPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // Profile Management
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.UserKeywords)
                    .ThenInclude(uk => uk.Keyword)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<bool> UpdateProfileAsync(int userId, ProfileViewModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Check if email is being changed and if it's already taken
            if (user.Email != model.Email)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == model.Email && u.Id != userId);
                if (emailExists)
                {
                    return false; // Email already taken
                }
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Affiliation = model.Affiliation;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update keywords
            await UpdateUserKeywordsAsync(userId, model.Keywords ?? new List<string>());

            return true;
        }

        public async Task<List<string>> GetUserKeywordsAsync(int userId)
        {
            var userKeywords = await _context.UserKeywords
                .Include(uk => uk.Keyword)
                .Where(uk => uk.UserId == userId)
                .Select(uk => uk.Keyword!.Name)
                .ToListAsync();

            return userKeywords;
        }

        public async Task<bool> UpdateUserKeywordsAsync(int userId, List<string> keywords)
        {
            // Get active conference
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return false;
            }

            // Remove existing user keywords
            var existingUserKeywords = await _context.UserKeywords
                .Where(uk => uk.UserId == userId)
                .ToListAsync();
            _context.UserKeywords.RemoveRange(existingUserKeywords);

            // Add new keywords
            foreach (var keywordName in keywords)
            {
                // Find or create keyword
                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => 
                        k.ConferenceId == activeConference.Id && 
                        k.Name == keywordName && 
                        k.Status == Models.Enums.KeywordStatus.Approved);

                if (keyword != null)
                {
                    var userKeyword = new UserKeyword
                    {
                        UserId = userId,
                        KeywordId = keyword.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserKeywords.Add(userKeyword);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Settings
        public async Task<UserSettingsViewModel?> GetUserSettingsAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return null;
            }

            // Get settings from SystemSettings or use defaults
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            var settings = new UserSettingsViewModel
            {
                EmailNotificationsEnabled = true, // Default
                ReviewAssignmentNotifications = true,
                SubmissionStatusNotifications = true,
                DeadlineReminders = true,
                Language = "vi-VN"
            };

            // TODO: Load from SystemSettings if needed

            return settings;
        }

        public async Task<bool> UpdateUserSettingsAsync(int userId, UserSettingsViewModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // TODO: Save to SystemSettings or UserSettings table if needed

            return true;
        }

        // Session Management
        public void SetUserSession(HttpContext context, User user)
        {
            context.Session.SetInt32("UserId", user.Id);
            context.Session.SetString("UserEmail", user.Email);
            context.Session.SetString("UserName", user.FullName);
            context.Session.SetString("UserRole", user.Role.ToString());
        }

        public User? GetCurrentUser(HttpContext context)
        {
            var userId = context.Session.GetInt32("UserId");
            if (userId == null)
            {
                return null;
            }

            // Use synchronous Find for session-based lookup
            // For better performance, consider caching user data in session
            return _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId && u.IsActive);
        }

        public void ClearUserSession(HttpContext context)
        {
            context.Session.Clear();
        }

        /// <summary>
        /// Lấy ID của admin hiện tại từ session
        /// </summary>
        public int? GetCurrentAdminId(HttpContext context)
        {
            var userId = context.Session.GetInt32("UserId");
            if (userId == null)
            {
                return null;
            }

            // Kiểm tra user có role Admin không
            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId && u.IsActive);

            if (user == null || user.Role != Models.Enums.UserRole.Admin)
            {
                return null;
            }

            return user.Id;
        }
    }
}

