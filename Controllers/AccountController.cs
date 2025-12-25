using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using SciSubmit.Services;
using SciSubmit.Data;
using SciSubmit.Models.Identity;
using SciSubmit.Models.Enums;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace SciSubmit.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IUserService userService,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userService = userService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
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

            // TODO: Implement password reset email logic
            TempData["SuccessMessage"] = "Nếu email tồn tại, chúng tôi đã gửi link đặt lại mật khẩu.";
            return RedirectToAction("Login");
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

            // TODO: Implement password reset logic
            TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // GET: Account/Profile
        public IActionResult Profile()
        {
            // TODO: Get current user profile
            var model = new ProfileViewModel
            {
                Email = "user@example.com",
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0123456789",
                Affiliation = "Đại học ABC",
                Keywords = new List<string> { "AI", "Machine Learning", "Data Science" }
            };

            return View(model);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Implement profile update logic
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return View(model);
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

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "Account", new { returnUrl }),
                Items = { { "returnUrl", returnUrl ?? Url.Action("Index", "Home") } }
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
                    // Tạo user mới
                    user = new User
                    {
                        Email = email,
                        FullName = name ?? email.Split('@')[0],
                        GoogleId = googleId,
                        Role = UserRole.Guest,
                        EmailConfirmed = true, // Google đã xác thực email
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new user from Google OAuth: {Email}", email);
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
                if (string.IsNullOrEmpty(returnUrl))
                {
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
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
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
    }
}
