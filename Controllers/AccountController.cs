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

            // TODO: Implement actual authentication logic
            // For now, just redirect to home
            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(returnUrl);
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

            // TODO: Implement actual registration logic
            // For now, redirect to login
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
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
                TempData["ErrorMessage"] = "Đăng nhập Google chưa được cấu hình.";
                return RedirectToAction("Login", new { returnUrl });
            }

            // Kiểm tra xem Google scheme có được đăng ký không
            var schemes = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var googleScheme = schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme).Result;
            
            if (googleScheme == null)
            {
                TempData["ErrorMessage"] = "Đăng nhập Google chưa được cấu hình.";
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
                    TempData["ErrorMessage"] = "Đăng nhập Google thất bại. Vui lòng thử lại.";
                    return RedirectToAction("Login");
                }

                var claims = result.Principal?.Claims.ToList();
                if (claims == null || !claims.Any())
                {
                    _logger.LogWarning("No claims received from Google");
                    TempData["ErrorMessage"] = "Không nhận được thông tin từ Google.";
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
                    TempData["ErrorMessage"] = "Không tìm thấy email từ Google.";
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
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng nhập với Google. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }
        }
    }

    // View Models
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email hoặc SĐT là bắt buộc")]
        [Display(Name = "Email hoặc Số điện thoại")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản")]
        [Display(Name = "Tôi đồng ý với điều khoản và điều kiện")]
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
