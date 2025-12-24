using SciSubmit.Models.Identity;
using SciSubmit.Models.Account;
using SciSubmit.Controllers;

namespace SciSubmit.Services
{
    public interface IUserService
    {
        // Authentication
        Task<User?> AuthenticateAsync(string emailOrPhone, string password);
        Task<bool> RegisterAsync(RegisterViewModel model);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        
        // Profile Management
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UpdateProfileAsync(int userId, ProfileViewModel model);
        Task<List<string>> GetUserKeywordsAsync(int userId);
        Task<bool> UpdateUserKeywordsAsync(int userId, List<string> keywords);
        
        // Settings
        Task<UserSettingsViewModel?> GetUserSettingsAsync(int userId);
        Task<bool> UpdateUserSettingsAsync(int userId, UserSettingsViewModel model);
        
        // Session Management
        void SetUserSession(HttpContext context, User user);
        User? GetCurrentUser(HttpContext context);
        void ClearUserSession(HttpContext context);
        int? GetCurrentAdminId(HttpContext context);
    }
}

