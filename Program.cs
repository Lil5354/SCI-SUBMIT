using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.CookiePolicy;
using SciSubmit.Data;
using SciSubmit.Services;
using SciSubmit.Jobs;
using Hangfire;
using Hangfire.SqlServer;
using System.IO;
using SciSubmit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Configure antiforgery to accept token from JSON body
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Configure Antiforgery options
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.FormFieldName = "__RequestVerificationToken";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

// Configure Cookie Policy - Allow cookies on HTTP for development
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    // HttpOnly is handled by individual cookie configurations (Session, Authentication cookies)
});

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Data Protection - Persist keys to file system for OAuth state
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
if (!Directory.Exists(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("SciSubmit");

// Authentication - Cookie & Google OAuth
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication(options =>
{
    // Default scheme for authentication (after login)
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // Default challenge scheme (when user needs to login)
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
    {
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    }
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Google OAuth - Only if configured
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        
        // QUAN TRỌNG 1: Khai báo đúng đường dẫn Callback giống Google Console
        options.CallbackPath = "/Account/GoogleCallback";
        
        // QUAN TRỌNG 2: Cấu hình TOÀN BỘ cookie cho HTTP Localhost
        // Configure Correlation Cookie (OAuth state cookie) - CRITICAL for OAuth to work
        options.CorrelationCookie.Name = ".AspNetCore.Correlation.Google";
        options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest; // Chấp nhận HTTP
        options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax; // Nới lỏng check site
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.IsEssential = true; // Cookie essential cho OAuth
        options.CorrelationCookie.Path = "/"; // Đảm bảo cookie áp dụng cho toàn bộ site
        
        // Save tokens in authentication cookie for later use
        options.SaveTokens = true;
        
        // Enable detailed logging for debugging OAuth failures
        options.Events.OnRemoteFailure = context =>
        {
            Console.WriteLine($"[GOOGLE OAUTH ERROR] {context.Failure?.Message}");
            Console.WriteLine($"[GOOGLE OAUTH ERROR] Stack: {context.Failure?.StackTrace}");
            return Task.CompletedTask;
        };
    });
}

// Add Services
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVnpayService, VnpayService>();
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure role-based authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ReviewerOnly", policy => policy.RequireRole("Reviewer"));
    options.AddPolicy("AuthorOnly", policy => policy.RequireRole("Author"));
});

// HttpClient for external APIs
builder.Services.AddHttpClient<MomoService>();

// Hangfire - Background Jobs
builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            SchemaName = "HangFire"
        });
});

builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// HTTPS Redirection - Enable for OAuth to work properly
app.UseHttpsRedirection();
app.UseStaticFiles();

// IMPORTANT: Cookie Policy must be before Session and Authentication
app.UseCookiePolicy();

// IMPORTANT: Session must be before Authentication
app.UseSession();

app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

// Recurring Jobs
RecurringJob.AddOrUpdate<EmailQueueJob>(
    "process-email-queue",
    job => job.ProcessEmailQueueAsync(),
    Cron.Minutely);

RecurringJob.AddOrUpdate<EmailQueueJob>(
    "send-deadline-reminders",
    job => job.SendDeadlineRemindersAsync(),
    Cron.Daily(8)); // Run daily at 8 AM

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Auto-seed database in Development mode
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Checking database for seed data...");
            await DbInitializer.SeedAsync(context);
            logger.LogInformation("Database seeding completed.");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

app.Run();
