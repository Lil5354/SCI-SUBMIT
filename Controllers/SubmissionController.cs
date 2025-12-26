using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SciSubmit.Data;
using SciSubmit.Models.Submission;
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

        public IActionResult Dashboard()
        {
            return View();
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

            ViewData["Submission"] = submission;
            ViewData["SubmissionId"] = id;
            ViewData["ReviewResults"] = completedReviews;
            ViewData["ReviewCriterias"] = reviewCriterias;
            return View();
        }

        public IActionResult FullPaper(int id)
        {
            ViewData["SubmissionId"] = id;
            ViewData["HasCurrentVersion"] = false; // TODO: Check if has current version
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadFullPaper(int submissionId, IFormFile file)
        {
            // TODO: Implement file upload logic
            TempData["SuccessMessage"] = "Đã nộp bài đầy đủ thành công!";
            return RedirectToAction(nameof(Details), new { id = submissionId });
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

