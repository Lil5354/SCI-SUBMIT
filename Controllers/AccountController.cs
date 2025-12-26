using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using SciSubmit.Services;
using SciSubmit.Data;
using SciSubmit.Models.Identity;
using SciSubmit.Models.Enums;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace SciSubmit.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly IEmailService _emailService;

        public AccountController(
            IUserService userService,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            IFileStorageService fileStorageService,
            IEmailService emailService)
        {
            _userService = userService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _emailService = emailService;
        }
        // GET: Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Authenticate user
                var user = await _userService.AuthenticateAsync(model.EmailOrPhone, model.Password);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email/phone number or password.");
                    return View(model);
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
                    return View(model);
                }

                // Set session
                _userService.SetUserSession(HttpContext, user);

                // Sign in with Cookie authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Email} with role {Role} logged in successfully", user.Email, user.Role);

                // Redirect based on user role
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                    return Redirect(returnUrl);
                }

                // Redirect admin to admin dashboard
                // Check role by enum, string, or int value (Admin = 3)
                bool isAdmin = user.Role == UserRole.Admin || 
                              user.Role.ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                              (int)user.Role == 3;
                
                if (isAdmin)
                {
                    _logger.LogInformation("Redirecting admin user {Email} (Role: {Role}, RoleInt: {RoleInt}) to Admin Dashboard", 
                        user.Email, user.Role, (int)user.Role);
                    return RedirectToAction("Dashboard", "Admin");
                }

                _logger.LogInformation("Redirecting user {Email} with role {Role} (RoleInt: {RoleInt}) to Home", 
                    user.Email, user.Role, (int)user.Role);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {EmailOrPhone}", model.EmailOrPhone);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "This email is already registered. Please use a different email or try logging in.");
                    return View(model);
                }

                // Check if phone number already exists
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    var existingPhone = await _context.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

                    if (existingPhone != null)
                    {
                        ModelState.AddModelError(nameof(model.PhoneNumber), "This phone number is already registered. Please use a different phone number.");
                        return View(model);
                    }
                }

                // Register user using UserService
                var success = await _userService.RegisterAsync(model);

                if (!success)
                {
                    ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                    return View(model);
                }

                _logger.LogInformation("New user registered: {Email}", model.Email);

                TempData["SuccessMessage"] = "Registration successful! Please log in with your credentials.";
            return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                // Always show success message (security best practice - don't reveal if email exists)
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Generate reset token
                    var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", "");

                    // Set token and expiry (24 hours)
                    user.ResetPasswordToken = token;
                    user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);
                    await _context.SaveChangesAsync();

                    // Generate reset URL
                    var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5234";
                    var resetUrl = $"{baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";

                    // Send email
                    var emailBody = EmailTemplates.GetPasswordResetEmail(
                        user.FullName,
                        resetUrl,
                        baseUrl);

                    var emailSent = await _emailService.SendEmailAsync(
                        user.Email,
                        "Password Reset Request - SciSubmit",
                        emailBody,
                        isHtml: true);

                    if (emailSent)
                    {
                        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send password reset email to {Email}", user.Email);
                    }
                }

                // Always show success message (security best practice)
                TempData["SuccessMessage"] = "If an account with that email exists, we have sent a password reset link.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password request for {Email}", model.Email);
                TempData["ErrorMessage"] = "An error occurred. Please try again later.";
                return View(model);
            }
        }

        // GET: Account/ResetPassword
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Find user by email and token
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && 
                                             u.ResetPasswordToken == model.Token);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid or expired reset token.");
                    return View(model);
                }

                // Check if token is expired
                if (user.ResetPasswordTokenExpiry == null || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                {
                    // Clear expired token
                    user.ResetPasswordToken = null;
                    user.ResetPasswordTokenExpiry = null;
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError(string.Empty, "The reset token has expired. Please request a new one.");
                    return View(model);
                }

                // Hash new password using same method as UserService
                string newPasswordHash;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Password + "SciSubmitSalt"));
                    newPasswordHash = Convert.ToBase64String(hashedBytes);
                }

                // Update password and clear reset token
                user.PasswordHash = newPasswordHash;
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successful for user {Email}", user.Email);

                TempData["SuccessMessage"] = "Your password has been reset successfully. Please login with your new password.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again later.");
                return View(model);
            }
        }

        // GET: Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var keywords = await _userService.GetUserKeywordsAsync(userId);

            var model = new ProfileViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Affiliation = user.Affiliation,
                Keywords = keywords,
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model, IFormFile? avatarFile)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                // Reload keywords if validation fails
                var keywords = await _userService.GetUserKeywordsAsync(userId);
                model.Keywords = keywords;
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    model.AvatarUrl = user.AvatarUrl;
                }
                return View(model);
            }

            try
            {
                // Handle avatar upload
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận file ảnh (JPG, JPEG, PNG, GIF)");
                        var keywords = await _userService.GetUserKeywordsAsync(userId);
                        model.Keywords = keywords;
                        var user = await _userService.GetUserByIdAsync(userId);
                        if (user != null)
                        {
                            model.AvatarUrl = user.AvatarUrl;
                        }
                        return View(model);
                    }

                    // Validate file size (max 5MB)
                    if (avatarFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("AvatarFile", "Kích thước file không được vượt quá 5MB");
                        var keywords = await _userService.GetUserKeywordsAsync(userId);
                        model.Keywords = keywords;
                        var user = await _userService.GetUserByIdAsync(userId);
                        if (user != null)
                        {
                            model.AvatarUrl = user.AvatarUrl;
                        }
                        return View(model);
                    }

                    // Delete old avatar if exists
                    var userForAvatar = await _context.Users.FindAsync(userId);
                    if (userForAvatar != null && !string.IsNullOrEmpty(userForAvatar.AvatarUrl))
                    {
                        await _fileStorageService.DeleteFileAsync(userForAvatar.AvatarUrl);
                    }

                    // Upload new avatar
                    using (var stream = avatarFile.OpenReadStream())
                    {
                        var avatarPath = await _fileStorageService.UploadFileAsync(stream, avatarFile.FileName, "avatars");
                        model.AvatarUrl = avatarPath;
                    }
                }

                // Update profile
                var success = await _userService.UpdateProfileAsync(userId, model);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, "Cập nhật hồ sơ thất bại. Vui lòng thử lại.");
                    var keywords = await _userService.GetUserKeywordsAsync(userId);
                    model.Keywords = keywords;
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        model.AvatarUrl = user.AvatarUrl;
                    }
                    return View(model);
                }

            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật hồ sơ. Vui lòng thử lại.");
                var keywords = await _userService.GetUserKeywordsAsync(userId);
                model.Keywords = keywords;
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    model.AvatarUrl = user.AvatarUrl;
                }
            return View(model);
            }
        }

        // GET: Account/Settings
        public IActionResult Settings()
        {
            // TODO: Get current user settings
            var model = new Models.Account.UserSettingsViewModel
            {
                EmailNotificationsEnabled = true,
                ReviewAssignmentNotifications = true,
                SubmissionStatusNotifications = true,
                DeadlineReminders = true,
                ShowEmailPublicly = false,
                ShowPhonePublicly = false,
                Language = "vi-VN"
            };

            return View(model);
        }

        // POST: Account/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(Models.Account.UserSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Implement settings update logic
            TempData["SuccessMessage"] = "Cập nhật cài đặt thành công!";
            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _userService.ClearUserSession(HttpContext);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/GoogleLogin
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            // Kiểm tra Google OAuth có được cấu hình không
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];

            if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
            {
                TempData["ErrorMessage"] = "Google login is not configured.";
                return RedirectToAction("Login", new { returnUrl });
            }

            // Kiểm tra xem Google scheme có được đăng ký không
            var schemes = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var googleScheme = schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme).Result;
            
            if (googleScheme == null)
            {
                TempData["ErrorMessage"] = "Google login is not configured.";
                return RedirectToAction("Login", new { returnUrl });
            }

            // CRITICAL: Don't set RedirectUri manually, let the CallbackPath in Program.cs handle it
            // This prevents "state missing or invalid" errors
            var properties = new AuthenticationProperties
            {
                // Don't set RedirectUri here - use the CallbackPath configured in Program.cs
                Items = { { "returnUrl", returnUrl ?? "/" } }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: Account/GoogleCallback
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Google authentication failed");
                    TempData["ErrorMessage"] = "Google login failed. Please try again.";
                    return RedirectToAction("Login");
                }

                var claims = result.Principal?.Claims.ToList();
                if (claims == null || !claims.Any())
                {
                    _logger.LogWarning("No claims received from Google");
                    TempData["ErrorMessage"] = "Failed to receive information from Google.";
                    return RedirectToAction("Login");
                }

                // Lấy thông tin từ Google
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "name")?.Value;
                var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email not found in Google claims");
                    TempData["ErrorMessage"] = "Email not found from Google.";
                    return RedirectToAction("Login");
                }

                // Tìm hoặc tạo user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email || u.GoogleId == googleId);

                if (user == null)
                {
                    // Tạo user mới - Mặc định role là Author cho đăng ký mới
                    user = new User
                    {
                        Email = email,
                        FullName = name ?? email.Split('@')[0],
                        GoogleId = googleId,
                        Role = UserRole.Author, // Default role for new registrations
                        EmailConfirmed = true, // Google đã xác thực email
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        PasswordHash = "GOOGLE_OAUTH" // Placeholder since Google OAuth doesn't use password
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new user from Google OAuth: {Email} with role {Role}", email, user.Role);
                }
                else
                {
                    // Cập nhật GoogleId nếu chưa có
                    if (string.IsNullOrEmpty(user.GoogleId) && !string.IsNullOrEmpty(googleId))
                    {
                        user.GoogleId = googleId;
                        await _context.SaveChangesAsync();
                    }

                    // Cập nhật last login
                    user.LastLoginAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Set session
                _userService.SetUserSession(HttpContext, user);

                // Sign in với Cookie authentication
                var claimsIdentity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Sign out Google scheme
                await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);

                // Get returnUrl from properties
                if (result.Properties?.Items != null && result.Properties.Items.TryGetValue("returnUrl", out var returnUrlFromProps))
                {
                    returnUrl = returnUrlFromProps;
                }
                
                // Redirect based on user role
                if (string.IsNullOrEmpty(returnUrl))
                {
                    // Check if user is admin
                    bool isAdmin = user.Role == UserRole.Admin || 
                                  user.Role.ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                  (int)user.Role == 3;
                    
                    if (isAdmin)
                    {
                        _logger.LogInformation("Redirecting admin user {Email} from Google OAuth to Admin Dashboard", user.Email);
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    
                    _logger.LogInformation("Redirecting user {Email} from Google OAuth to Home", user.Email);
                    return RedirectToAction("Index", "Home");
                }

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleCallback");
                TempData["ErrorMessage"] = "An error occurred during Google login. Please try again.";
                return RedirectToAction("Login");
            }
        }
    }

    // View Models
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email or phone number is required")]
        [Display(Name = "Email or Phone Number")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must agree to the terms and conditions")]
        [Display(Name = "I agree to the terms and conditions")]
        public bool AgreeToTerms { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Đơn vị công tác")]
        public string? Affiliation { get; set; }

        [Display(Name = "Từ khóa chuyên môn")]
        public List<string> Keywords { get; set; } = new List<string>();

        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }
    }
}
