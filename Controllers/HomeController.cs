using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            // Pass Google Maps API Key to view
            ViewBag.GoogleMapsApiKey = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["GoogleMaps:ApiKey"] ?? "";
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

            // Load Keynote Speakers
            var keynoteSpeakers = await _context.KeynoteSpeakers
                .Where(k => k.ConferenceId == activeConference.Id && k.IsActive)
                .OrderBy(k => k.OrderIndex)
                .ThenBy(k => k.Id)
                .Select(k => new
                {
                    id = k.Id,
                    name = k.Name,
                    title = k.Title,
                    affiliation = k.Affiliation,
                    biography = k.Biography,
                    topic = k.Topic,
                    photoUrl = k.PhotoUrl,
                    orderIndex = k.OrderIndex
                })
                .ToListAsync();

            // Load Conference Location
            var conferenceLocation = await _context.ConferenceLocations
                .Where(l => l.ConferenceId == activeConference.Id && l.IsActive)
                .FirstOrDefaultAsync();

            var locationData = conferenceLocation != null ? new
            {
                id = conferenceLocation.Id,
                name = conferenceLocation.Name,
                address = conferenceLocation.Address,
                city = conferenceLocation.City,
                country = conferenceLocation.Country,
                latitude = conferenceLocation.Latitude,
                longitude = conferenceLocation.Longitude,
                googleMapsEmbedUrl = conferenceLocation.GoogleMapsEmbedUrl,
                description = conferenceLocation.Description
            } : null;

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
                timeline = timelineItems,
                keynoteSpeakers = keynoteSpeakers,
                location = locationData,
                conferenceName = activeConference.Name,
                conferenceDescription = activeConference.Description,
                conferenceStartDate = activeConference.StartDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                conferenceEndDate = activeConference.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                googleMapsApiKey = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["GoogleMaps:ApiKey"] ?? ""
            });
        }

        public async Task<IActionResult> Committee()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return View(new Models.Admin.CommitteeViewModel());
            }

            var sections = await _context.CommitteeSections
                .Where(s => s.ConferenceId == activeConference.Id && s.IsActive)
                .Include(s => s.Members.Where(m => m.IsActive))
                .OrderBy(s => s.OrderIndex)
                .ThenBy(s => s.Id)
                .ToListAsync();

            var viewModel = new Models.Admin.CommitteeViewModel
            {
                Sections = sections.Select(s => new Models.Admin.CommitteeSectionViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    SectionType = s.SectionType,
                    OrderIndex = s.OrderIndex,
                    IsActive = s.IsActive,
                    Members = s.Members
                        .OrderBy(m => m.OrderIndex)
                        .ThenBy(m => m.Id)
                        .Select(m => new Models.Admin.CommitteeMemberViewModel
                        {
                            Id = m.Id,
                            CommitteeSectionId = m.CommitteeSectionId,
                            Name = m.Name,
                            Title = m.Title,
                            Affiliation = m.Affiliation,
                            Country = m.Country,
                            Description = m.Description,
                            PhotoUrl = m.PhotoUrl,
                            Topic = m.Topic,
                            TrackName = m.TrackName,
                            TrackDescription = m.TrackDescription,
                            OrderIndex = m.OrderIndex,
                            IsActive = m.IsActive
                        }).ToList()
                }).ToList()
            };

            ViewBag.ConferenceName = activeConference.Name;
            return View(viewModel);
        }

        public async Task<IActionResult> KeynoteSpeakers()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return View(new List<Models.Conference.KeynoteSpeaker>());
            }

            var speakers = await _context.KeynoteSpeakers
                .Where(k => k.ConferenceId == activeConference.Id && k.IsActive)
                .OrderBy(k => k.OrderIndex)
                .ThenBy(k => k.Id)
                .ToListAsync();

            ViewBag.ConferenceName = activeConference.Name;
            return View(speakers);
        }

        public async Task<IActionResult> Program()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return View(new Models.Admin.ProgramViewModel());
            }

            var schedule = await _context.ProgramSchedules
                .Where(p => p.ConferenceId == activeConference.Id)
                .Include(p => p.Items.Where(i => i.IsActive))
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return View(new Models.Admin.ProgramViewModel());
            }

            var viewModel = new Models.Admin.ProgramViewModel
            {
                Id = schedule.Id,
                Time = schedule.Time,
                Venue = schedule.Venue,
                PresentationsLink = schedule.PresentationsLink,
                PapersLink = schedule.PapersLink,
                ProgramLink = schedule.ProgramLink,
                Items = schedule.Items
                    .OrderBy(i => i.OrderIndex)
                    .ThenBy(i => i.Id)
                    .Select(i => new Models.Admin.ProgramItemViewModel
                    {
                        Id = i.Id,
                        ProgramScheduleId = i.ProgramScheduleId,
                        Time = i.Time,
                        Contents = i.Contents,
                        OrderIndex = i.OrderIndex,
                        IsActive = i.IsActive
                    }).ToList()
            };

            ViewBag.ConferenceName = activeConference.Name;
            return View(viewModel);
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
