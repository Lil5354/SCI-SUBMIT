using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Enums;
using SciSubmit.Models.Identity;
using SciSubmit.Models.Conference;
using SciSubmit.Models.Content;
using SciSubmit.Models.Submission;
using SciSubmit.Models.Review;

namespace SciSubmit.Controllers
{
    public class SeedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SeedController> _logger;

        public SeedController(ApplicationDbContext context, ILogger<SeedController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                await DbInitializer.SeedAsync(_context);
                TempData["SuccessMessage"] = "Đã import dữ liệu mẫu thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                TempData["ErrorMessage"] = $"Lỗi khi import dữ liệu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> SeedReviewTestData()
        {
            try
            {
                _logger.LogInformation("Starting to seed review test data...");
                
                // First, ensure basic seed data exists
                await DbInitializer.SeedAsync(_context);
                
                // Then create/update review assignment for testing
                await EnsureReviewTestDataAsync(_context);
                
                _logger.LogInformation("Review test data seeded successfully");
                
                if (Request.Headers["Accept"].ToString().Contains("application/json") || Request.Query["format"] == "json")
                {
                    return Json(new { success = true, message = "Đã tạo data test thành công!" });
                }
                TempData["SuccessMessage"] = "Đã tạo data test cho review flow thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding review test data: {Message}", ex.Message);
                var errorMessage = $"Lỗi: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                
                if (Request.Headers["Accept"].ToString().Contains("application/json") || Request.Query["format"] == "json")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index");
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> CheckReviewTestData(string? email = null)
        {
            try
            {
                var reviewerEmail = email ?? "reviewer1@scisubmit.com";
                var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Email == reviewerEmail && u.Role == UserRole.Reviewer);
                if (reviewer == null)
                {
                    return Json(new { success = false, message = $"Reviewer {reviewerEmail} not found. Please run seed data first." });
                }
                
                var assignments = await _context.ReviewAssignments
                    .Include(ra => ra.Submission)
                    .Where(ra => ra.ReviewerId == reviewer.Id)
                    .ToListAsync();
                
                var pendingCount = assignments.Count(ra => ra.Status == ReviewAssignmentStatus.Accepted && ra.CompletedAt == null);
                
                return Json(new 
                { 
                    success = true,
                    reviewerId = reviewer.Id,
                    reviewerEmail = reviewer.Email,
                    totalAssignments = assignments.Count,
                    pendingAssignments = pendingCount,
                    assignments = assignments.Select(ra => new
                    {
                        id = ra.Id,
                        submissionId = ra.SubmissionId,
                        submissionTitle = ra.Submission?.Title ?? "N/A",
                        status = ra.Status.ToString(),
                        acceptedAt = ra.AcceptedAt,
                        completedAt = ra.CompletedAt,
                        deadline = ra.Deadline
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CreateDataForReviewer(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email is required" });
                }
                
                _logger.LogInformation("Creating data for reviewer: {Email}", email);
                
                // Password hashing method
                string HashPassword(string password)
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "SciSubmitSalt"));
                        return Convert.ToBase64String(hashedBytes);
                    }
                }
                
                // Get or create reviewer
                var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (reviewer == null)
                {
                    // Create new reviewer
                    reviewer = new User
                    {
                        Email = email,
                        PasswordHash = HashPassword("Reviewer@123"), // Default password
                        FullName = email.Split('@')[0], // Use email prefix as name
                        Affiliation = "Đại học Kinh tế Tài chính",
                        Role = UserRole.Reviewer,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(reviewer);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created reviewer user: {Email}", email);
                }
                else
                {
                    // Update existing user to be reviewer
                    reviewer.Role = UserRole.Reviewer;
                    reviewer.IsActive = true;
                    reviewer.EmailConfirmed = true;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated user to reviewer: {Email}", email);
                }
                
                // Get conference
                var conference = await _context.Conferences.FirstOrDefaultAsync(c => c.IsActive);
                if (conference == null)
                {
                    return Json(new { success = false, message = "No active conference found. Please run seed data first." });
                }
                
                // Get or create review criteria
                if (!await _context.ReviewCriterias.AnyAsync(rc => rc.ConferenceId == conference.Id && rc.IsActive))
                {
                    var reviewCriterias = new List<ReviewCriteria>
                    {
                        new ReviewCriteria { ConferenceId = conference.Id, Name = "Tính mới", Description = "Tính mới và sáng tạo của nghiên cứu", MaxScore = 5, OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ReviewCriteria { ConferenceId = conference.Id, Name = "Độ sâu nghiên cứu", Description = "Độ sâu và chi tiết của nghiên cứu", MaxScore = 5, OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ReviewCriteria { ConferenceId = conference.Id, Name = "Phương pháp nghiên cứu", Description = "Tính hợp lý và khoa học của phương pháp", MaxScore = 5, OrderIndex = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ReviewCriteria { ConferenceId = conference.Id, Name = "Trình bày", Description = "Cách trình bày và cấu trúc bài báo", MaxScore = 5, OrderIndex = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ReviewCriteria { ConferenceId = conference.Id, Name = "Kết quả và đóng góp", Description = "Giá trị và đóng góp của kết quả nghiên cứu", MaxScore = 5, OrderIndex = 5, IsActive = true, CreatedAt = DateTime.UtcNow }
                    };
                    _context.ReviewCriterias.AddRange(reviewCriterias);
                    await _context.SaveChangesAsync();
                }
                
                // Find submission with Full Paper
                var submission = await _context.Submissions
                    .Include(s => s.FullPaperVersions)
                    .Where(s => s.Status == SubmissionStatus.UnderReview && s.FullPaperSubmittedAt != null)
                    .FirstOrDefaultAsync();
                
                if (submission == null)
                {
                    // Get any submission and update it
                    submission = await _context.Submissions
                        .Include(s => s.FullPaperVersions)
                        .FirstOrDefaultAsync();
                    
                    if (submission == null)
                    {
                        return Json(new { success = false, message = "No submission found. Please run seed data first." });
                    }
                    
                    submission.Status = SubmissionStatus.UnderReview;
                    submission.FullPaperSubmittedAt = DateTime.UtcNow.AddDays(-10);
                    
                    if (!submission.FullPaperVersions.Any(v => v.IsCurrentVersion))
                    {
                        var fullPaperVersion = new FullPaperVersion
                        {
                            SubmissionId = submission.Id,
                            FileUrl = "https://example.com/fullpaper.pdf",
                            FileName = "fullpaper.pdf",
                            FileSize = 1024000,
                            VersionNumber = 1,
                            IsCurrentVersion = true,
                            UploadedAt = DateTime.UtcNow.AddDays(-10),
                            UploadedBy = submission.AuthorId
                        };
                        _context.FullPaperVersions.Add(fullPaperVersion);
                    }
                    await _context.SaveChangesAsync();
                }
                
                // Get Admin
                var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
                
                // Create or update Review Assignment
                var assignment = await _context.ReviewAssignments
                    .FirstOrDefaultAsync(ra => ra.SubmissionId == submission.Id && ra.ReviewerId == reviewer.Id);
                
                if (assignment == null)
                {
                    assignment = new ReviewAssignment
                    {
                        SubmissionId = submission.Id,
                        ReviewerId = reviewer.Id,
                        Status = ReviewAssignmentStatus.Accepted,
                        InvitedAt = DateTime.UtcNow.AddDays(-7),
                        InvitedBy = admin?.Id ?? reviewer.Id,
                        Deadline = DateTime.UtcNow.AddDays(14),
                        AcceptedAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    };
                    _context.ReviewAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    assignment.Status = ReviewAssignmentStatus.Accepted;
                    assignment.AcceptedAt = DateTime.UtcNow.AddDays(-5);
                    assignment.CompletedAt = null;
                    assignment.Deadline = DateTime.UtcNow.AddDays(14);
                    
                    // Delete existing review if any
                    var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewAssignmentId == assignment.Id);
                    if (existingReview != null)
                    {
                        var reviewScores = await _context.ReviewScores.Where(rs => rs.ReviewId == existingReview.Id).ToListAsync();
                        _context.ReviewScores.RemoveRange(reviewScores);
                        _context.Reviews.Remove(existingReview);
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
                _logger.LogInformation("Created data for reviewer {Email}. Assignment ID: {AssignmentId}", email, assignment.Id);
                
                if (Request.Headers["Accept"].ToString().Contains("application/json") || Request.Query["format"] == "json")
                {
                    return Json(new { success = true, message = $"Đã tạo data cho reviewer {email} thành công!", assignmentId = assignment.Id });
                }
                TempData["SuccessMessage"] = $"Đã tạo data cho reviewer {email} thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data for reviewer {Email}", email);
                var errorMessage = $"Lỗi: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                
                if (Request.Headers["Accept"].ToString().Contains("application/json") || Request.Query["format"] == "json")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index");
            }
        }
        
        private async Task EnsureReviewTestDataAsync(ApplicationDbContext context)
        {
            // Password hashing method
            string HashPassword(string password)
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "SciSubmitSalt"));
                    return Convert.ToBase64String(hashedBytes);
                }
            }

            // Get or create reviewer
            var reviewer = await context.Users.FirstOrDefaultAsync(u => u.Email == "reviewer1@scisubmit.com" && u.Role == UserRole.Reviewer);
            if (reviewer == null)
            {
                reviewer = new User
                {
                    Email = "reviewer1@scisubmit.com",
                    PasswordHash = HashPassword("Reviewer@123"),
                    FullName = "GS. Nguyễn Văn Reviewer",
                    Affiliation = "Đại học Khoa học",
                    Role = UserRole.Reviewer,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(reviewer);
                await context.SaveChangesAsync();
                _logger.LogInformation("Created reviewer: {Email}", reviewer.Email);
            }

            // Get a submission that has Full Paper
            var submission = await context.Submissions
                .Include(s => s.FullPaperVersions)
                .Where(s => s.Status == SubmissionStatus.UnderReview && s.FullPaperSubmittedAt != null)
                .FirstOrDefaultAsync();
            
            if (submission == null)
            {
                // Get any submission and update it
                submission = await context.Submissions
                    .Include(s => s.FullPaperVersions)
                    .FirstOrDefaultAsync();
                
                if (submission == null)
                {
                    throw new Exception("No submission found. Please run basic seed data first.");
                }
                
                // Update submission to have Full Paper
                submission.Status = SubmissionStatus.UnderReview;
                submission.FullPaperSubmittedAt = DateTime.UtcNow.AddDays(-10);
                
                if (!submission.FullPaperVersions.Any(v => v.IsCurrentVersion))
                {
                    var fullPaperVersion = new FullPaperVersion
                    {
                        SubmissionId = submission.Id,
                        FileUrl = "https://example.com/fullpaper.pdf",
                        FileName = "fullpaper.pdf",
                        FileSize = 1024000,
                        VersionNumber = 1,
                        IsCurrentVersion = true,
                        UploadedAt = DateTime.UtcNow.AddDays(-10),
                        UploadedBy = submission.AuthorId
                    };
                    context.FullPaperVersions.Add(fullPaperVersion);
                }
                await context.SaveChangesAsync();
            }

            // Get Admin
            var admin = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);

            // Get or create Review Assignment (Accepted status, ready to review)
            var assignment = await context.ReviewAssignments
                .FirstOrDefaultAsync(ra => ra.SubmissionId == submission.Id && ra.ReviewerId == reviewer.Id);

            if (assignment == null)
            {
                assignment = new ReviewAssignment
                {
                    SubmissionId = submission.Id,
                    ReviewerId = reviewer.Id,
                    Status = ReviewAssignmentStatus.Accepted,
                    InvitedAt = DateTime.UtcNow.AddDays(-7),
                    InvitedBy = admin?.Id ?? reviewer.Id,
                    Deadline = DateTime.UtcNow.AddDays(14),
                    AcceptedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                };
                context.ReviewAssignments.Add(assignment);
                await context.SaveChangesAsync();
            }
            else
            {
                // Reset assignment to Accepted and not completed
                assignment.Status = ReviewAssignmentStatus.Accepted;
                assignment.AcceptedAt = DateTime.UtcNow.AddDays(-5);
                assignment.CompletedAt = null;
                assignment.Deadline = DateTime.UtcNow.AddDays(14);

                // Delete existing review if any (to allow re-testing)
                var existingReview = await context.Reviews.FirstOrDefaultAsync(r => r.ReviewAssignmentId == assignment.Id);
                if (existingReview != null)
                {
                    var reviewScores = await context.ReviewScores.Where(rs => rs.ReviewId == existingReview.Id).ToListAsync();
                    context.ReviewScores.RemoveRange(reviewScores);
                    context.Reviews.Remove(existingReview);
                }

                await context.SaveChangesAsync();
            }
            
            _logger.LogInformation("Review test data ensured. Reviewer ID: {ReviewerId}, Assignment ID: {AssignmentId}, Submission ID: {SubmissionId}", 
                reviewer.Id, assignment.Id, submission.Id);
        }
    }
}
