using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SciSubmit.Data;
using SciSubmit.Models.Submission;
using SciSubmit.Models.Payment;
using SciSubmit.Models.Enums;
using SciSubmit.Services;
using System.Security.Claims;
using System.IO;

namespace SciSubmit.Controllers
{
    [Authorize(Roles = "Admin,Reviewer,Author")]
    public class SubmissionController : Controller
    {
        private readonly ILogger<SubmissionController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ISubmissionService _submissionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IWebHostEnvironment _environment;

        public SubmissionController(
            ILogger<SubmissionController> logger,
            ApplicationDbContext context,
            ISubmissionService submissionService,
            IFileStorageService fileStorageService,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _submissionService = submissionService;
            _fileStorageService = fileStorageService;
            _environment = environment;
        }

        private async Task<int> GetCurrentUserIdAsync()
        {
            // CÁCH 1: Lấy trực tiếp từ NameIdentifier (TỐT NHẤT)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogInformation($"GetCurrentUserIdAsync: Found user ID from NameIdentifier: {userId}");
                return userId;
            }
            
            // CÁCH 2: Fallback - Lấy từ Email claim
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("GetCurrentUserIdAsync: No NameIdentifier or Email claim found. User may not be authenticated.");
                return 0;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning($"GetCurrentUserIdAsync: User with email {email} not found in database.");
                return 0;
            }
            
            _logger.LogInformation($"GetCurrentUserIdAsync: Found user ID from Email: {user.Id}");
            return user.Id;
        }

        private async Task<int> GetActiveConferenceIdAsync()
        {
            var conference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();
            return conference?.Id ?? 0;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var authorId = await GetCurrentUserIdAsync();
                if (authorId == 0)
                {
                    _logger.LogWarning("Dashboard: User not authenticated or not found");
                    return RedirectToAction("Login", "Account");
                }

                var conferenceId = await GetActiveConferenceIdAsync();
                if (conferenceId == 0)
                {
                    _logger.LogWarning("Dashboard: No active conference found");
                    TempData["ErrorMessage"] = "No active conference found";
                    return View(new AuthorDashboardViewModel());
                }

                // Get all submissions for this author
                var allSubmissions = await _context.Submissions
                    .Where(s => s.AuthorId == authorId && s.ConferenceId == conferenceId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                // Calculate statistics
                var totalSubmissions = allSubmissions.Count;
                var underReview = allSubmissions.Count(s => s.Status == SubmissionStatus.UnderReview);
                var accepted = allSubmissions.Count(s => s.Status == SubmissionStatus.Accepted);
                var rejected = allSubmissions.Count(s => s.Status == SubmissionStatus.Rejected || s.Status == SubmissionStatus.AbstractRejected);

                // Determine current progress step based on latest submission
                int currentStep = 1; // Default: Abstract
                var latestSubmission = allSubmissions.FirstOrDefault();
                if (latestSubmission != null)
                {
                    // Step 1: Abstract (Draft, PendingAbstractReview, AbstractApproved, AbstractRejected)
                    if (latestSubmission.Status == SubmissionStatus.Draft || 
                        latestSubmission.Status == SubmissionStatus.PendingAbstractReview ||
                        latestSubmission.Status == SubmissionStatus.AbstractApproved ||
                        latestSubmission.Status == SubmissionStatus.AbstractRejected)
                    {
                        currentStep = 1;
                    }
                    // Step 2: Full Paper (FullPaperSubmitted)
                    else if (latestSubmission.Status == SubmissionStatus.FullPaperSubmitted)
                    {
                        currentStep = 2;
                    }
                    // Step 3: Review (UnderReview, RevisionRequired)
                    else if (latestSubmission.Status == SubmissionStatus.UnderReview ||
                             latestSubmission.Status == SubmissionStatus.RevisionRequired)
                    {
                        currentStep = 3;
                    }
                    // Step 4: Payment/Completed (Accepted, Rejected, Withdrawn)
                    else if (latestSubmission.Status == SubmissionStatus.Accepted ||
                             latestSubmission.Status == SubmissionStatus.Rejected ||
                             latestSubmission.Status == SubmissionStatus.Withdrawn)
                    {
                        currentStep = 4;
                    }
                }

                // Get all submissions for dropdown (ordered by creation date, newest first)
                var allSubmissionsForDropdown = allSubmissions
                    .Select(s => new RecentSubmissionViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Status = s.Status,
                        SubmittedAt = s.AbstractSubmittedAt,
                        AbstractReviewedAt = s.AbstractReviewedAt,
                        FullPaperSubmittedAt = s.FullPaperSubmittedAt
                    })
                    .ToList();

                // Get recent submissions (top 5) for table
                var recentSubmissions = allSubmissionsForDropdown.Take(5).ToList();

                // Get tracked submission
                var user = await _context.Users.FindAsync(authorId);
                RecentSubmissionViewModel? trackedSubmission = null;
                int trackedStep = 1;

                if (user?.TrackedSubmissionId.HasValue == true)
                {
                    var tracked = allSubmissions.FirstOrDefault(s => s.Id == user.TrackedSubmissionId.Value);
                    if (tracked != null)
                    {
                        trackedSubmission = new RecentSubmissionViewModel
                        {
                            Id = tracked.Id,
                            Title = tracked.Title,
                            Status = tracked.Status,
                            SubmittedAt = tracked.AbstractSubmittedAt,
                            AbstractReviewedAt = tracked.AbstractReviewedAt,
                            FullPaperSubmittedAt = tracked.FullPaperSubmittedAt
                        };

                        // Calculate progress step for tracked submission
                        if (tracked.Status == SubmissionStatus.Draft || 
                            tracked.Status == SubmissionStatus.PendingAbstractReview ||
                            tracked.Status == SubmissionStatus.AbstractApproved ||
                            tracked.Status == SubmissionStatus.AbstractRejected)
                        {
                            trackedStep = 1;
                        }
                        else if (tracked.Status == SubmissionStatus.FullPaperSubmitted)
                        {
                            trackedStep = 2;
                        }
                        else if (tracked.Status == SubmissionStatus.UnderReview ||
                                 tracked.Status == SubmissionStatus.RevisionRequired)
                        {
                            trackedStep = 3;
                        }
                        else if (tracked.Status == SubmissionStatus.Accepted ||
                                 tracked.Status == SubmissionStatus.Rejected ||
                                 tracked.Status == SubmissionStatus.Withdrawn)
                        {
                            trackedStep = 4;
                        }
                        currentStep = trackedStep; // Use tracked submission's step
                    }
                }

                var viewModel = new AuthorDashboardViewModel
                {
                    TotalSubmissions = totalSubmissions,
                    UnderReview = underReview,
                    Accepted = accepted,
                    Rejected = rejected,
                    CurrentProgressStep = currentStep,
                    RecentSubmissions = recentSubmissions,
                    TrackedSubmission = trackedSubmission
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading author dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard";
                return View(new AuthorDashboardViewModel());
            }
        }

        public async Task<IActionResult> Index(string? search = null, string? status = null, int? topicId = null)
        {
            try
            {
                var authorId = await GetCurrentUserIdAsync();
                if (authorId == 0)
                {
                    _logger.LogWarning("User not authenticated or not found");
                    return RedirectToAction("Login", "Account");
                }

                var conferenceId = await GetActiveConferenceIdAsync();
                if (conferenceId == 0)
                {
                    _logger.LogWarning("No active conference found");
                    TempData["ErrorMessage"] = "Không tìm thấy hội nghị đang hoạt động";
                    return View(new List<Submission>());
                }

                // Load submissions for current author (INCLUDE ALL STATUSES INCLUDING DRAFT)
                var query = _context.Submissions
                    .Include(s => s.SubmissionAuthors)
                    .Include(s => s.SubmissionTopics)
                        .ThenInclude(st => st.Topic)
                    .Where(s => s.AuthorId == authorId && s.ConferenceId == conferenceId);

                _logger.LogInformation($"Index: Loading submissions for AuthorId={authorId}, ConferenceId={conferenceId}");
                
                // Count total before filters
                var totalBeforeFilter = await query.CountAsync();
                _logger.LogInformation($"Index: Total submissions before filter: {totalBeforeFilter}");

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => s.Title.Contains(search) || 
                                             s.SubmissionAuthors.Any(a => a.FullName.Contains(search)));
                    _logger.LogInformation($"Index: Applied search filter: {search}");
                }

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<Models.Enums.SubmissionStatus>(status, out var statusEnum))
                {
                    query = query.Where(s => s.Status == statusEnum);
                    _logger.LogInformation($"Index: Applied status filter: {statusEnum}");
                }

                if (topicId.HasValue && topicId.Value > 0)
                {
                    query = query.Where(s => s.SubmissionTopics.Any(st => st.TopicId == topicId.Value));
                    _logger.LogInformation($"Index: Applied topic filter: {topicId.Value}");
                }

                var submissions = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
                
                _logger.LogInformation($"Index: Loaded {submissions.Count} submissions. Statuses: {string.Join(", ", submissions.Select(s => s.Status))}");

                // Load topics for filter dropdown
                var topics = await _context.Topics
                    .Where(t => t.ConferenceId == conferenceId)
                    .OrderBy(t => t.OrderIndex)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

                // Calculate statistics
                var totalSubmissions = await _context.Submissions
                    .CountAsync(s => s.AuthorId == authorId && s.ConferenceId == conferenceId);
                
                var pendingReview = await _context.Submissions
                    .CountAsync(s => s.AuthorId == authorId && 
                                    s.ConferenceId == conferenceId && 
                                    (s.Status == Models.Enums.SubmissionStatus.PendingAbstractReview || 
                                     s.Status == Models.Enums.SubmissionStatus.UnderReview));
                
                var accepted = await _context.Submissions
                    .CountAsync(s => s.AuthorId == authorId && 
                                    s.ConferenceId == conferenceId && 
                                    s.Status == Models.Enums.SubmissionStatus.Accepted);
                
                var revisionRequested = await _context.Submissions
                    .CountAsync(s => s.AuthorId == authorId && 
                                    s.ConferenceId == conferenceId && 
                                    s.Status == Models.Enums.SubmissionStatus.RevisionRequired);

                ViewBag.Topics = topics;
                ViewBag.Search = search;
                ViewBag.Status = status;
                ViewBag.TopicId = topicId;
                ViewBag.TotalSubmissions = totalSubmissions;
                ViewBag.PendingReview = pendingReview;
                ViewBag.Accepted = accepted;
                ViewBag.RevisionRequested = revisionRequested;

                _logger.LogInformation($"Loaded {submissions.Count} submissions for author {authorId}");
                return View(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading submissions");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách bài nộp";
                return View(new List<Submission>());
            }
        }

        public async Task<IActionResult> Create(int? id = null)
        {
            try
            {
                _logger.LogInformation("=== GET CREATE PAGE ===");
                
                var conferenceId = await GetActiveConferenceIdAsync();
                if (conferenceId == 0)
                {
                    _logger.LogWarning("No active conference found");
                    TempData["ErrorMessage"] = "Không tìm thấy hội nghị đang hoạt động";
                    return RedirectToAction(nameof(Index));
                }

                var topics = await _submissionService.GetActiveTopicsAsync(conferenceId);
                ViewData["Topics"] = topics;
                _logger.LogInformation($"Loaded {topics.Count} topics");

                AbstractSubmissionViewModel? model = null;
                if (id.HasValue)
                {
                    var authorId = await GetCurrentUserIdAsync();
                    model = await _submissionService.GetDraftAsync(id.Value, authorId);
                    if (model != null)
                    {
                        _logger.LogInformation($"Loaded draft submission {id.Value}");
                    }
                }

                ViewData["Model"] = model;
                _logger.LogInformation("=== GET CREATE PAGE SUCCESS ===");
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft([FromForm] AbstractSubmissionViewModel model)
        {
            try
            {
                _logger.LogInformation("=== SAVE DRAFT API CALL ===");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning($"Validation errors: {string.Join(", ", errors)}");
                    return Json(new { success = false, errors = errors });
                }

                // Validate authors
                if (model.Authors == null || model.Authors.Count == 0)
                {
                    return Json(new { success = false, errors = new[] { "Vui lòng thêm ít nhất một tác giả" } });
                }

                if (!model.Authors.Any(a => a.IsCorrespondingAuthor))
                {
                    return Json(new { success = false, errors = new[] { "Phải có ít nhất một tác giả liên hệ" } });
                }

                // Validate keywords
                if (model.Keywords == null || model.Keywords.Count < 5)
                {
                    return Json(new { success = false, errors = new[] { "Vui lòng thêm ít nhất 5 từ khóa" } });
                }

                if (model.Keywords.Count > 6)
                {
                    return Json(new { success = false, errors = new[] { "Tối đa 6 từ khóa" } });
                }

                var authorId = await GetCurrentUserIdAsync();
                var conferenceId = await GetActiveConferenceIdAsync();

                // Validate Topic belongs to the same conference
                if (conferenceId > 0)
                {
                    var topicExists = await _context.Topics
                        .AnyAsync(t => t.Id == model.TopicId && t.ConferenceId == conferenceId && t.IsActive);
                    if (!topicExists)
                    {
                        _logger.LogWarning($"Topic {model.TopicId} does not belong to conference {conferenceId} or is not active");
                        return Json(new { success = false, errors = new[] { "Chủ đề không hợp lệ hoặc không thuộc hội nghị này" } });
                    }
                    _logger.LogInformation($"Validated Topic {model.TopicId} belongs to Conference {conferenceId}");
                }

                if (authorId == 0 || conferenceId == 0)
                {
                    return Json(new { success = false, errors = new[] { "Không xác định được tác giả hoặc hội nghị" } });
                }

                // Handle file upload if provided
                string? fileUrl = null;
                if (Request.Form.Files.Count > 0)
                {
                    var file = Request.Form.Files[0];
                    if (file != null && file.Length > 0)
                    {
                        // Validate file type (only DOC/DOCX)
                        var allowedExtensions = new[] { ".doc", ".docx" };
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            return Json(new { success = false, errors = new[] { "Chỉ chấp nhận file DOC hoặc DOCX" } });
                        }

                        // Validate file size (< 10MB)
                        if (file.Length > 10 * 1024 * 1024)
                        {
                            return Json(new { success = false, errors = new[] { "File không được vượt quá 10MB" } });
                        }

                        try
                        {
                            using var stream = file.OpenReadStream();
                            fileUrl = await _fileStorageService.UploadFileAsync(stream, file.FileName, "abstracts");
                            _logger.LogInformation($"File uploaded: {fileUrl}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading file");
                            return Json(new { success = false, errors = new[] { "Lỗi khi upload file" } });
                        }
                    }
                }

                var submission = await _submissionService.SaveDraftAsync(model, authorId, conferenceId, fileUrl);
                
                if (submission == null)
                {
                    return Json(new { success = false, errors = new[] { "Không thể lưu nháp" } });
                }

                // File URL is already handled in SaveDraftAsync

                _logger.LogInformation($"=== SAVE DRAFT SUCCESS - SubmissionId: {submission.Id} ===");
                
                return Json(new { success = true, submissionId = submission.Id, message = "Đã lưu nháp thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SAVE DRAFT ERROR ===");
                return Json(new { success = false, errors = new[] { "Có lỗi xảy ra khi lưu nháp" } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromForm] AbstractSubmissionViewModel model)
        {
            try
            {
                _logger.LogInformation("=== SUBMIT ABSTRACT API CALL ===");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning($"Validation errors: {string.Join(", ", errors)}");
                    return Json(new { success = false, errors = errors });
                }

                // Validate authors
                if (model.Authors == null || model.Authors.Count == 0)
                {
                    return Json(new { success = false, errors = new[] { "Vui lòng thêm ít nhất một tác giả" } });
                }

                if (!model.Authors.Any(a => a.IsCorrespondingAuthor))
                {
                    return Json(new { success = false, errors = new[] { "Phải có ít nhất một tác giả liên hệ" } });
                }

                // Validate keywords
                if (model.Keywords == null || model.Keywords.Count < 5)
                {
                    return Json(new { success = false, errors = new[] { "Vui lòng thêm ít nhất 5 từ khóa" } });
                }

                if (model.Keywords.Count > 6)
                {
                    return Json(new { success = false, errors = new[] { "Tối đa 6 từ khóa" } });
                }

                // Validate abstract length (300 words max)
                var wordCount = model.Abstract.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount > 300)
                {
                    return Json(new { success = false, errors = new[] { "Tóm tắt không được vượt quá 300 từ" } });
                }

                var authorId = await GetCurrentUserIdAsync();
                var conferenceId = await GetActiveConferenceIdAsync();

                // Validate Topic belongs to the same conference
                if (conferenceId > 0)
                {
                    var topicExists = await _context.Topics
                        .AnyAsync(t => t.Id == model.TopicId && t.ConferenceId == conferenceId && t.IsActive);
                    if (!topicExists)
                    {
                        _logger.LogWarning($"Topic {model.TopicId} does not belong to conference {conferenceId} or is not active");
                        return Json(new { success = false, errors = new[] { "Chủ đề không hợp lệ hoặc không thuộc hội nghị này" } });
                    }
                    _logger.LogInformation($"Validated Topic {model.TopicId} belongs to Conference {conferenceId}");
                }

                if (authorId == 0 || conferenceId == 0)
                {
                    return Json(new { success = false, errors = new[] { "Không xác định được tác giả hoặc hội nghị" } });
                }

                // Handle file upload if provided
                string? fileUrl = null;
                if (Request.Form.Files.Count > 0)
                {
                    var file = Request.Form.Files[0];
                    if (file != null && file.Length > 0)
                    {
                        // Validate file type (only DOC/DOCX)
                        var allowedExtensions = new[] { ".doc", ".docx" };
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            return Json(new { success = false, errors = new[] { "Chỉ chấp nhận file DOC hoặc DOCX" } });
                        }

                        // Validate file size (< 10MB)
                        if (file.Length > 10 * 1024 * 1024)
                        {
                            return Json(new { success = false, errors = new[] { "File không được vượt quá 10MB" } });
                        }

                        try
                        {
                            using var stream = file.OpenReadStream();
                            fileUrl = await _fileStorageService.UploadFileAsync(stream, file.FileName, "abstracts");
                            _logger.LogInformation($"File uploaded: {fileUrl}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading file");
                            return Json(new { success = false, errors = new[] { "Lỗi khi upload file" } });
                        }
                    }
                }

                var submission = await _submissionService.SubmitAbstractAsync(model, authorId, conferenceId, fileUrl);
                
                if (submission == null)
                {
                    _logger.LogError($"SubmitAbstractAsync returned null for AuthorId={authorId}, ConferenceId={conferenceId}");
                    return Json(new { success = false, errors = new[] { "Không thể nộp bài" } });
                }

                _logger.LogInformation($"=== SUBMIT ABSTRACT SUCCESS - SubmissionId: {submission.Id}, Status: {submission.Status}, AuthorId: {submission.AuthorId}, ConferenceId: {submission.ConferenceId} ===");
                
                // Verify submission was saved correctly
                var verifySubmission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.Id == submission.Id);
                
                if (verifySubmission == null)
                {
                    _logger.LogError($"CRITICAL: Submission {submission.Id} not found in database after save!");
                }
                else
                {
                    _logger.LogInformation($"Verified: Submission {verifySubmission.Id} exists with Status={verifySubmission.Status}, AuthorId={verifySubmission.AuthorId}, ConferenceId={verifySubmission.ConferenceId}");
                }
                
                return Json(new { 
                    success = true, 
                    submissionId = submission.Id, 
                    message = "Đã nộp tóm tắt thành công! Email xác nhận đã được gửi." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SUBMIT ABSTRACT ERROR ===");
                return Json(new { success = false, errors = new[] { "Có lỗi xảy ra khi nộp bài" } });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            ViewData["HideMainHeader"] = true;
            
            var authorId = await GetCurrentUserIdAsync();
            if (authorId == 0)
            {
                _logger.LogWarning("Details: User not authenticated or not found");
                return RedirectToAction("Login", "Account");
            }

            var submission = await _context.Submissions
                .Include(s => s.SubmissionAuthors)
                .Include(s => s.SubmissionTopics)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.SubmissionKeywords)
                    .ThenInclude(sk => sk.Keyword)
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Review)
                        .ThenInclude(r => r.ReviewScores)
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Reviewer)
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Review)
                        .ThenInclude(r => r.Reviewer)
                .Include(s => s.FinalDecision)
                    .ThenInclude(fd => fd.DecisionMaker)
                .Include(s => s.FullPaperVersions)
                .FirstOrDefaultAsync(s => s.Id == id && s.AuthorId == authorId);

            if (submission == null)
            {
                _logger.LogWarning($"Details: Submission {id} not found or not owned by author {authorId}");
                TempData["ErrorMessage"] = "Không tìm thấy bài nộp.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"Details: Loading submission {id} for author {authorId}");
            
            // Get review criteria for MaxScore
            var conferenceId = submission.ConferenceId;
            var reviewCriterias = await _context.ReviewCriterias
                .Where(rc => rc.ConferenceId == conferenceId && rc.IsActive)
                .ToDictionaryAsync(rc => rc.Name, rc => rc.MaxScore);
            
            // Get completed reviews for this submission
            var completedReviews = submission.ReviewAssignments
                .Where(ra => ra.Review != null && ra.Status == Models.Enums.ReviewAssignmentStatus.Completed)
                .Select(ra => ra.Review!)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            // Check if this submission is being tracked
            var user = await _context.Users.FindAsync(authorId);
            bool isTracked = user?.TrackedSubmissionId == id;

            ViewData["Submission"] = submission;
            ViewData["SubmissionId"] = id;
            ViewData["ReviewResults"] = completedReviews;
            ViewData["ReviewCriterias"] = reviewCriterias;
            ViewData["FinalDecision"] = submission.FinalDecision;
            ViewData["IsTracked"] = isTracked;
            return View();
        }

        public async Task<IActionResult> Payment(int id)
        {
            ViewData["HideMainHeader"] = true;
            
            var authorId = await GetCurrentUserIdAsync();
            if (authorId == 0)
            {
                TempData["ErrorMessage"] = "You must be logged in to access payment page.";
                return RedirectToAction("Login", "Account");
            }

            var submission = await _context.Submissions
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.Id == id && s.AuthorId == authorId);

            if (submission == null)
            {
                TempData["ErrorMessage"] = "Submission not found or you don't have permission.";
                return RedirectToAction(nameof(Index));
            }

            if (submission.Status != Models.Enums.SubmissionStatus.AbstractApproved)
        {
                TempData["ErrorMessage"] = "Only approved abstracts can proceed to payment.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if payment already completed
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.SubmissionId == id && p.Status == Models.Enums.PaymentStatus.Completed);

            if (existingPayment != null)
            {
                // Payment already completed, redirect to FullPaper
                return RedirectToAction(nameof(FullPaper), new { id });
            }

            ViewData["Submission"] = submission;
            ViewData["SubmissionId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var authorId = await GetCurrentUserIdAsync();
            if (authorId == 0)
            {
                return Json(new { success = false, message = "You must be logged in." });
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == id && s.AuthorId == authorId);

            if (submission == null)
            {
                return Json(new { success = false, message = "Submission not found." });
            }

            if (submission.Status != Models.Enums.SubmissionStatus.AbstractApproved)
            {
                return Json(new { success = false, message = "Only approved abstracts can proceed to payment." });
            }

            // Check if payment already exists
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.SubmissionId == id);

            if (existingPayment != null && existingPayment.Status == Models.Enums.PaymentStatus.Completed)
            {
                return Json(new { success = true, message = "Payment already confirmed.", redirectUrl = Url.Action(nameof(FullPaper), new { id }) });
            }

            // Create or update payment record
            if (existingPayment == null)
            {
                existingPayment = new Models.Payment.Payment
                {
                    SubmissionId = id,
                    UserId = authorId,
                    Amount = 2000000, // Default fee: 2,000,000 VND
                    PaymentMethod = Models.Enums.PaymentMethod.BankTransfer,
                    Status = Models.Enums.PaymentStatus.Completed,
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Payments.Add(existingPayment);
            }
            else
            {
                existingPayment.Status = Models.Enums.PaymentStatus.Completed;
                existingPayment.PaymentDate = DateTime.UtcNow;
                existingPayment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment confirmed successfully!", redirectUrl = Url.Action(nameof(FullPaper), new { id }) });
        }

        public async Task<IActionResult> FullPaper(int id)
        {
            ViewData["HideMainHeader"] = true;
            
            var authorId = await GetCurrentUserIdAsync();
            if (authorId == 0)
            {
                TempData["ErrorMessage"] = "You must be logged in to submit full paper.";
                return RedirectToAction("Login", "Account");
            }

            var submission = await _context.Submissions
                .Include(s => s.Author)
                .Include(s => s.SubmissionAuthors)
                .Include(s => s.SubmissionTopics)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.SubmissionKeywords)
                    .ThenInclude(sk => sk.Keyword)
                .FirstOrDefaultAsync(s => s.Id == id && s.AuthorId == authorId);

            if (submission == null)
            {
                TempData["ErrorMessage"] = "Submission not found or you don't have permission.";
                return RedirectToAction(nameof(Index));
            }

            // Check if abstract is approved
            if (submission.Status != Models.Enums.SubmissionStatus.AbstractApproved)
        {
                TempData["ErrorMessage"] = "Only approved abstracts can submit full paper.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if payment is completed
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.SubmissionId == id && p.Status == Models.Enums.PaymentStatus.Completed);

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Please complete payment before submitting full paper.";
                return RedirectToAction(nameof(Payment), new { id });
            }

            // Get Full Paper deadline from ConferencePlan
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            DateTime? fullPaperDeadline = null;
            if (activeConference != null)
            {
                var plan = await _context.ConferencePlans
                    .Where(p => p.ConferenceId == activeConference.Id)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
                
                fullPaperDeadline = plan?.FullPaperSubmissionDeadline;
            }

            ViewData["Submission"] = submission;
            ViewData["SubmissionId"] = id;
            ViewData["HasCurrentVersion"] = submission.FullPaperVersions.Any(v => v.IsCurrentVersion);
            ViewData["FullPaperDeadline"] = fullPaperDeadline;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFullPaper(int submissionId, IFormFile file)
        {
            try
            {
                _logger.LogInformation($"=== UPLOAD FULL PAPER START ===");
                _logger.LogInformation($"SubmissionId: {submissionId}");

                var authorId = await GetCurrentUserIdAsync();
                if (authorId == 0)
                {
                    TempData["ErrorMessage"] = "You must be logged in to submit full paper.";
                    return RedirectToAction("Login", "Account");
                }

                // Validate file
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file to upload.";
                    return RedirectToAction(nameof(FullPaper), new { id = submissionId });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Only DOC and DOCX files are allowed.";
                    return RedirectToAction(nameof(FullPaper), new { id = submissionId });
                }

                // Validate file size (max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    TempData["ErrorMessage"] = "File size must not exceed 10MB.";
                    return RedirectToAction(nameof(FullPaper), new { id = submissionId });
                }

                // Check submission exists and belongs to author
                var submission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.Id == submissionId && s.AuthorId == authorId);

                if (submission == null)
                {
                    TempData["ErrorMessage"] = "Submission not found or you don't have permission.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if abstract is approved
                if (submission.Status != Models.Enums.SubmissionStatus.AbstractApproved)
                {
                    TempData["ErrorMessage"] = "Only approved abstracts can submit full paper.";
            return RedirectToAction(nameof(Details), new { id = submissionId });
                }

                // Check if payment is completed
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.SubmissionId == submissionId && p.Status == Models.Enums.PaymentStatus.Completed);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Please complete payment before submitting full paper.";
                    return RedirectToAction(nameof(Payment), new { id = submissionId });
                }

                // Check deadline
                var activeConference = await _context.Conferences
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (activeConference != null)
                {
                    var plan = await _context.ConferencePlans
                        .Where(p => p.ConferenceId == activeConference.Id)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (plan?.FullPaperSubmissionDeadline != null && plan.FullPaperSubmissionDeadline.Value < DateTime.UtcNow)
                    {
                        TempData["ErrorMessage"] = "The full paper submission deadline has passed.";
                        return RedirectToAction(nameof(Details), new { id = submissionId });
                    }
                }

                // Upload file
                string fileUrl;
                using (var stream = file.OpenReadStream())
                {
                    fileUrl = await _fileStorageService.UploadFileAsync(stream, file.FileName, "fullpapers");
                }
                if (string.IsNullOrEmpty(fileUrl))
                {
                    TempData["ErrorMessage"] = "Failed to upload file. Please try again.";
                    return RedirectToAction(nameof(FullPaper), new { id = submissionId });
                }

                // Submit full paper
                var success = await _submissionService.SubmitFullPaperAsync(
                    submissionId, 
                    authorId, 
                    fileUrl, 
                    file.FileName, 
                    file.Length);

                if (success)
                {
                    TempData["SuccessMessage"] = "Full paper submitted successfully!";
                    return RedirectToAction(nameof(Details), new { id = submissionId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to submit full paper. Please try again.";
                    return RedirectToAction(nameof(FullPaper), new { id = submissionId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading full paper for submission {submissionId}");
                TempData["ErrorMessage"] = "An error occurred while submitting full paper. Please try again.";
                return RedirectToAction(nameof(FullPaper), new { id = submissionId });
            }
        }

        public IActionResult Feedback(int id)
        {
            ViewData["SubmissionId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadFinalVersion(int id, IFormFile file)
        {
            // TODO: Implement final version upload
            TempData["SuccessMessage"] = "Đã nộp bản cuối thành công!";
            return RedirectToAction(nameof(Feedback), new { id });
        }

        public IActionResult Edit(int id)
        {
            ViewData["SubmissionId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, object model)
        {
            // TODO: Implement edit logic
            TempData["SuccessMessage"] = "Đã cập nhật bài nộp thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleTrackProgress(int submissionId, bool isTracking)
        {
            try
            {
                var authorId = await GetCurrentUserIdAsync();
                if (authorId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify submission belongs to this author
                var submission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.Id == submissionId && s.AuthorId == authorId);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Submission not found or access denied" });
                }

                var user = await _context.Users.FindAsync(authorId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (isTracking)
                {
                    // If tracking another submission, the new one will replace the old one
                    user.TrackedSubmissionId = submissionId;
                }
                else
                {
                    // Only untrack if this is the currently tracked submission
                    if (user.TrackedSubmissionId == submissionId)
                    {
                        user.TrackedSubmissionId = null;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = isTracking ? "Submission is now being tracked" : "Tracking removed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling track progress");
                return Json(new { success = false, message = "An error occurred while updating tracking status" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id, string? reason = null)
        {
            try
            {
                _logger.LogInformation($"=== WITHDRAW REQUEST ===");
                _logger.LogInformation($"SubmissionId: {id}");

                var authorId = await GetCurrentUserIdAsync();
                if (authorId == 0)
                {
                    _logger.LogWarning("User not authenticated");
                    TempData["ErrorMessage"] = "You must be logged in to withdraw a submission.";
                    return RedirectToAction("Login", "Account");
                }

                var success = await _submissionService.WithdrawSubmissionAsync(id, authorId, reason);
                
                if (success)
                {
                    _logger.LogInformation($"Submission {id} withdrawn successfully by author {authorId}");
                    TempData["SuccessMessage"] = "Your submission has been withdrawn successfully. A confirmation email has been sent.";
            return RedirectToAction(nameof(Index));
        }
                else
                {
                    _logger.LogWarning($"Failed to withdraw submission {id} for author {authorId}");
                    TempData["ErrorMessage"] = "Failed to withdraw submission. The submission may not exist, may not belong to you, or cannot be withdrawn in its current status.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error withdrawing submission {id}");
                TempData["ErrorMessage"] = "An error occurred while withdrawing the submission. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        public async Task<IActionResult> Download(int id)
        {
            try
            {
                _logger.LogInformation($"=== DOWNLOAD REQUEST ===");
                _logger.LogInformation($"SubmissionId: {id}");

                var authorId = await GetCurrentUserIdAsync();
                if (authorId == 0)
                {
                    _logger.LogWarning("User not authenticated");
                    TempData["ErrorMessage"] = "You must be logged in to download files.";
                    return RedirectToAction("Login", "Account");
                }

                // Get submission with file URL
                var submission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.Id == id && s.AuthorId == authorId);

                if (submission == null)
                {
                    _logger.LogWarning($"Submission {id} not found or not owned by author {authorId}");
                    TempData["ErrorMessage"] = "Submission not found or you don't have permission to download this file.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if file exists
                if (string.IsNullOrWhiteSpace(submission.AbstractFileUrl))
                {
                    _logger.LogWarning($"Submission {id} has no file URL");
                    TempData["ErrorMessage"] = "No file available for download.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _logger.LogInformation($"Downloading file: {submission.AbstractFileUrl}");

                // If file URL is a full URL (http/https), redirect to it
                if (submission.AbstractFileUrl.StartsWith("http://") || submission.AbstractFileUrl.StartsWith("https://"))
                {
                    _logger.LogInformation($"Redirecting to external URL: {submission.AbstractFileUrl}");
                    return Redirect(submission.AbstractFileUrl);
                }

                // If file is stored locally, download it
                try
                {
                    var fileUrl = submission.AbstractFileUrl;
                    
                    // File URL is relative path like "/abstracts/guid_filename.doc"
                    // Need to combine with WebRootPath
                    var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/'));
                    
                    _logger.LogInformation($"Looking for file at: {filePath}");
                    
                    // Check if file exists
                    if (!System.IO.File.Exists(filePath))
                    {
                        _logger.LogWarning($"File not found at path: {filePath}");
                        TempData["ErrorMessage"] = "File not found on server.";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    var fileName = System.IO.Path.GetFileName(filePath);
                    var contentType = "application/octet-stream";

                    // Determine content type based on file extension
                    var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
                    contentType = extension switch
                    {
                        ".doc" => "application/msword",
                        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        ".pdf" => "application/pdf",
                        ".txt" => "text/plain",
                        _ => "application/octet-stream"
                    };

                    _logger.LogInformation($"File downloaded successfully: {fileName} ({fileBytes.Length} bytes)");
                    return File(fileBytes, contentType, fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error downloading file: {submission.AbstractFileUrl}");
                    _logger.LogError($"Exception: {ex.Message}");
                    _logger.LogError($"Stack: {ex.StackTrace}");
                    TempData["ErrorMessage"] = "An error occurred while downloading the file.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Download action for submission {id}");
                TempData["ErrorMessage"] = "An error occurred while processing your download request.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

