using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Models;
using SciSubmit.Data;
using SciSubmit.Models.Admin;
using SciSubmit.Models.Enums;
using System.Text.Json;

namespace SciSubmit.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetInterfaceSettings()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return Json(new
                {
                    heroTitle = "Welcome to Scientific Conference Management System",
                    heroSubtitle = "A professional platform for managing, submitting, and evaluating scientific research works.",
                    conferenceDate = activeConference?.StartDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    conferenceLocation = activeConference?.Location,
                    statistics = new
                    {
                        submissions = await _context.Submissions.CountAsync(),
                        underReview = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.UnderReview),
                        accepted = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.Accepted),
                        rejected = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.Rejected)
                    }
                });
            }

            var settings = await _context.SystemSettings
                .Where(s => s.ConferenceId == activeConference.Id)
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var stats = new
            {
                submissions = await _context.Submissions.CountAsync(),
                underReview = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.UnderReview),
                accepted = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.Accepted),
                rejected = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.Rejected)
            };

            // Load timeline items
            var timelineItems = new List<object>();
            var timelineSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.ConferenceId == activeConference.Id && s.Key == "Dashboard.Timeline");

            if (timelineSetting != null && !string.IsNullOrEmpty(timelineSetting.Value))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<TimelineItem>>(timelineSetting.Value) ?? new List<TimelineItem>();
                    timelineItems = items
                        .Where(i => i.IsActive)
                        .OrderBy(i => i.Order)
                        .Select(i => new
                        {
                            id = i.Id,
                            title = i.Title,
                            description = i.Description,
                            date = i.Date?.ToString("yyyy-MM-ddTHH:mm"),
                            icon = i.Icon,
                            color = i.Color,
                            order = i.Order
                        })
                        .ToList<object>();
                }
                catch
                {
                    timelineItems = new List<object>();
                }
            }

            return Json(new
            {
                heroTitle = settings.GetValueOrDefault("Interface.HeroTitle", "Welcome to Scientific Conference Management System"),
                heroSubtitle = settings.GetValueOrDefault("Interface.HeroSubtitle", "A professional platform for managing, submitting, and evaluating scientific research works."),
                conferenceDate = activeConference.StartDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                conferenceLocation = settings.GetValueOrDefault("Interface.ConferenceLocation", activeConference.Location ?? "TBA"),
                heroBackgroundColor = settings.GetValueOrDefault("Interface.HeroBackgroundColor"),
                heroBackgroundImage = settings.GetValueOrDefault("Interface.HeroBackgroundImage"),
                enableAnimations = bool.Parse(settings.GetValueOrDefault("Interface.EnableAnimations", "true") ?? "true"),
                animationSpeed = settings.GetValueOrDefault("Interface.AnimationSpeed", "normal"),
                enableParticles = bool.Parse(settings.GetValueOrDefault("Interface.EnableParticles", "true") ?? "true"),
                particleDensity = settings.GetValueOrDefault("Interface.ParticleDensity", "medium"),
                enableLightStreaks = bool.Parse(settings.GetValueOrDefault("Interface.EnableLightStreaks", "true") ?? "true"),
                lightIntensity = settings.GetValueOrDefault("Interface.LightIntensity", "medium"),
                gradientColor = settings.GetValueOrDefault("Interface.GradientColor", "#3b82f6"),
                statistics = stats,
                timeline = timelineItems
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
