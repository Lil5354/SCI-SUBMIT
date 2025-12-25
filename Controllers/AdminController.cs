using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Services;
using SciSubmit.Models.Admin;
using SciSubmit.Models.Enums;
using SciSubmit.Data;
using System.Text.Json;
using System;

namespace SciSubmit.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            var deadlines = await _adminService.GetUpcomingDeadlinesAsync();

            var viewModel = new DashboardViewModel
            {
                Stats = stats,
                Deadlines = deadlines
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Submissions(SubmissionFilterViewModel? filter)
        {
            filter ??= new SubmissionFilterViewModel();
            var submissions = await _adminService.GetSubmissionsAsync(filter);
            
            // Load topics and keywords for filter dropdowns
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();
            
            if (activeConference != null)
            {
                ViewBag.Topics = await _context.Topics
                    .Where(t => t.ConferenceId == activeConference.Id && t.IsActive)
                    .OrderBy(t => t.OrderIndex)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
                
                ViewBag.Keywords = await _context.Keywords
                    .Where(k => k.ConferenceId == activeConference.Id && k.Status == Models.Enums.KeywordStatus.Approved)
                    .OrderBy(k => k.Name)
                    .ToListAsync();
            }
            
            ViewBag.Filter = filter;
            ViewBag.StatusOptions = Enum.GetNames(typeof(Models.Enums.SubmissionStatus));
            return View(submissions);
        }

        public async Task<IActionResult> SubmissionDetails(int id)
        {
            var submission = await _adminService.GetSubmissionDetailsAsync(id);
            if (submission == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài nộp.";
                return RedirectToAction(nameof(Submissions));
            }
            return View(submission);
        }

        public IActionResult ReviewSubmission(int id)
        {
            ViewData["SubmissionId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAbstract(int id)
        {
            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder
            var result = await _adminService.ApproveAbstractAsync(id, adminId);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã chấp nhận tóm tắt thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể chấp nhận tóm tắt. Vui lòng kiểm tra lại trạng thái bài nộp.";
            }
            return RedirectToAction(nameof(SubmissionDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAbstract(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(SubmissionDetails), new { id });
            }

            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder
            var result = await _adminService.RejectAbstractAsync(id, adminId, reason);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã từ chối tóm tắt và gửi email thông báo cho tác giả.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể từ chối tóm tắt. Vui lòng kiểm tra lại trạng thái bài nộp.";
            }
            return RedirectToAction(nameof(SubmissionDetails), new { id });
        }

        public async Task<IActionResult> Assignments(AssignmentFilterViewModel? filter)
        {
            filter ??= new AssignmentFilterViewModel();
            var assignments = await _adminService.GetReviewAssignmentsAsync(filter);
            
            // Load submissions for dropdown
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();
            
            if (activeConference != null)
            {
                ViewBag.Submissions = await _context.Submissions
                    .Where(s => s.ConferenceId == activeConference.Id && 
                               (s.Status == Models.Enums.SubmissionStatus.AbstractApproved ||
                                s.Status == Models.Enums.SubmissionStatus.FullPaperSubmitted ||
                                s.Status == Models.Enums.SubmissionStatus.UnderReview))
                    .Select(s => new { s.Id, s.Title })
                    .ToListAsync();
            }
            
            ViewBag.Filter = filter;
            ViewBag.StatusOptions = Enum.GetNames(typeof(Models.Enums.ReviewAssignmentStatus));
            return View(assignments);
        }

        public async Task<IActionResult> AssignReviewer(int? submissionId)
        {
            if (!submissionId.HasValue || submissionId.Value <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn bài báo.";
                return RedirectToAction(nameof(Assignments));
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == submissionId.Value);
            
            if (submission == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài báo với ID: " + submissionId.Value;
                return RedirectToAction(nameof(Assignments));
            }

            // Check if submission is in valid status for assignment
            var validStatuses = new[] 
            { 
                Models.Enums.SubmissionStatus.AbstractApproved,
                Models.Enums.SubmissionStatus.FullPaperSubmitted,
                Models.Enums.SubmissionStatus.UnderReview
            };
            
            if (!validStatuses.Contains(submission.Status))
            {
                TempData["ErrorMessage"] = $"Không thể phân công phản biện. Trạng thái bài báo hiện tại: {submission.Status}. Chỉ có thể phân công khi bài báo ở trạng thái: Đã duyệt tóm tắt, Đã nộp Full-text, hoặc Đang phản biện.";
                return RedirectToAction(nameof(SubmissionDetails), new { id = submissionId.Value });
            }

            var availableReviewers = await _adminService.GetAvailableReviewersAsync(submissionId.Value);

            // Check if there are any reviewers in the system at all
            var totalReviewers = await _context.Users
                .CountAsync(u => u.Role == Models.Enums.UserRole.Reviewer && u.IsActive);
            
            if (totalReviewers == 0)
            {
                TempData["WarningMessage"] = "Không có Reviewer nào trong hệ thống. Vui lòng tạo Reviewer trong Quản lý Người dùng trước.";
            }
            else if (!availableReviewers.Any())
            {
                TempData["WarningMessage"] = $"Có {totalReviewers} Reviewer trong hệ thống nhưng không có Reviewer nào được trả về. Vui lòng kiểm tra lại.";
            }

            ViewBag.AvailableReviewers = availableReviewers;
            ViewBag.Submission = submission;
            ViewBag.SubmissionId = submissionId.Value;
            return View(submissionId.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReviewer(int submissionId, int reviewerId, DateTime deadline)
        {
            // Validate inputs - check if values are actually provided
            if (submissionId <= 0)
            {
                TempData["ErrorMessage"] = "Lỗi: Không tìm thấy bài báo. Vui lòng quay lại và chọn bài báo.";
                return RedirectToAction(nameof(Assignments));
            }
            
            if (reviewerId <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn reviewer.";
                return RedirectToAction(nameof(AssignReviewer), new { submissionId });
            }

            if (deadline == default(DateTime))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn deadline.";
                return RedirectToAction(nameof(AssignReviewer), new { submissionId });
            }

            // Convert local time to UTC if needed (datetime-local sends as Unspecified)
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            DateTime utcDeadline;
            if (deadline.Kind == DateTimeKind.Unspecified)
            {
                // Treat as local time and convert to UTC
                utcDeadline = TimeZoneInfo.ConvertTimeToUtc(deadline, localTimeZone);
            }
            else if (deadline.Kind == DateTimeKind.Local)
            {
                utcDeadline = deadline.ToUniversalTime();
            }
            else if (deadline.Kind == DateTimeKind.Utc)
            {
                utcDeadline = deadline;
            }
            else
            {
                // Fallback: treat as local
                utcDeadline = TimeZoneInfo.ConvertTimeToUtc(deadline, localTimeZone);
            }

            // Validate deadline is in the future
            if (utcDeadline <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Deadline phải trong tương lai.";
                return RedirectToAction(nameof(AssignReviewer), new { submissionId });
            }

            // Check submission status before assigning
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài báo với ID: " + submissionId;
                return RedirectToAction(nameof(AssignReviewer), new { submissionId });
            }

            // Check if submission is in valid status for assignment
            var validStatuses = new[] 
            { 
                Models.Enums.SubmissionStatus.AbstractApproved,
                Models.Enums.SubmissionStatus.FullPaperSubmitted,
                Models.Enums.SubmissionStatus.UnderReview
            };
            
            if (!validStatuses.Contains(submission.Status))
            {
                TempData["ErrorMessage"] = $"Không thể phân công phản biện. Trạng thái bài báo hiện tại: {submission.Status}. Chỉ có thể phân công khi bài báo ở trạng thái: Đã duyệt tóm tắt, Đã nộp Full-text, hoặc Đang phản biện.";
                return RedirectToAction(nameof(AssignReviewer), new { submissionId });
            }

            // Check if reviewer already assigned to this submission
            var existingAssignment = await _context.ReviewAssignments
                .FirstOrDefaultAsync(ra => ra.SubmissionId == submissionId && ra.ReviewerId == reviewerId);
            
            if (existingAssignment != null)
            {
                TempData["ErrorMessage"] = "Reviewer này đã được phân công cho bài báo này rồi.";
                return RedirectToAction(nameof(AssignReviewer), new { submissionId });
            }

            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder
            
            try
            {
                var result = await _adminService.AssignReviewerAsync(submissionId, reviewerId, utcDeadline, adminId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đã phân công phản biện thành công!";
                    return RedirectToAction(nameof(SubmissionDetails), new { id = submissionId });
                }
                else
                {
                    // More specific error message
                    var reviewer = await _context.Users.FindAsync(reviewerId);
                    var errorMsg = "Không thể phân công phản biện. ";
                    
                    if (reviewer == null)
                    {
                        errorMsg += "Reviewer không tồn tại.";
                    }
                    else if (reviewer.Role != Models.Enums.UserRole.Reviewer)
                    {
                        errorMsg += "Người dùng này không phải là Reviewer.";
                    }
                    else if (!reviewer.IsActive)
                    {
                        errorMsg += "Reviewer này đã bị vô hiệu hóa.";
                    }
                    else if (utcDeadline <= DateTime.UtcNow)
                    {
                        errorMsg += "Deadline phải trong tương lai.";
                    }
                    else
                    {
                        errorMsg += "Vui lòng kiểm tra lại (có thể reviewer đã được phân công rồi hoặc có lỗi xảy ra).";
                    }
                    
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(AssignReviewer), new { submissionId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi phân công phản biện: {ex.Message}";
                return RedirectToAction(nameof(AssignReviewer), new { submissionId });
            }
        }

        public async Task<IActionResult> FinalDecision(int id)
        {
            var viewModel = await _adminService.GetSubmissionForDecisionAsync(id);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài nộp hoặc bài nộp chưa có phản biện.";
                return RedirectToAction(nameof(Submissions));
            }
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeFinalDecision(int id, string decision, string? comments)
        {
            if (string.IsNullOrEmpty(decision))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn quyết định.";
                return RedirectToAction(nameof(FinalDecision), new { id });
            }

            // Parse decision string to enum
            FinalDecisionType decisionType = decision switch
            {
                "Accept" => FinalDecisionType.Accepted,
                "Minor" => FinalDecisionType.MinorRevision,
                "Major" => FinalDecisionType.MajorRevision,
                "Reject" => FinalDecisionType.Rejected,
                _ => throw new ArgumentException("Invalid decision type")
            };

            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder

            var result = await _adminService.MakeFinalDecisionAsync(id, decisionType, comments, adminId);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã ra quyết định cuối cùng và gửi email thông báo!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể ra quyết định. Vui lòng kiểm tra lại.";
            }
            return RedirectToAction(nameof(Submissions));
        }

        public async Task<IActionResult> Conference()
        {
            var config = await _adminService.GetConferenceConfigAsync();
            if (config == null)
            {
                TempData["ErrorMessage"] = "Chưa có hội thảo nào được cấu hình.";
                return View(new Models.Admin.ConferenceConfigViewModel());
            }
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConference(Models.Admin.ConferenceConfigViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Conference", model);
            }

            var result = await _adminService.UpdateConferenceAsync(model);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã cập nhật thông tin hội thảo thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật thông tin hội thảo.";
            }
            return RedirectToAction(nameof(Conference));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConferencePlan(Models.Admin.ConferencePlanViewModel model)
        {
            // Validate required fields
            if (model.AbstractSubmissionOpenDate == default(DateTime) || model.AbstractSubmissionDeadline == default(DateTime))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ ngày mở và deadline nộp tóm tắt.";
                return RedirectToAction(nameof(Conference));
            }

            // Convert local datetime to UTC for storage
            // datetime-local sends as Unspecified, treat as local time
            // Use TimeZoneInfo to properly convert
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            
            DateTime abstractOpen = model.AbstractSubmissionOpenDate;
            if (abstractOpen.Kind == DateTimeKind.Unspecified)
            {
                // Treat as local time and convert to UTC
                abstractOpen = TimeZoneInfo.ConvertTimeToUtc(abstractOpen, localTimeZone);
            }
            else if (abstractOpen.Kind == DateTimeKind.Local)
            {
                abstractOpen = abstractOpen.ToUniversalTime();
            }
            // If already UTC, keep as is

            DateTime abstractDeadline = model.AbstractSubmissionDeadline;
            if (abstractDeadline.Kind == DateTimeKind.Unspecified)
            {
                abstractDeadline = TimeZoneInfo.ConvertTimeToUtc(abstractDeadline, localTimeZone);
            }
            else if (abstractDeadline.Kind == DateTimeKind.Local)
            {
                abstractDeadline = abstractDeadline.ToUniversalTime();
            }

            model.AbstractSubmissionOpenDate = abstractOpen;
            model.AbstractSubmissionDeadline = abstractDeadline;

            // Handle nullable dates
            if (model.FullPaperSubmissionOpenDate.HasValue && model.FullPaperSubmissionOpenDate.Value != default(DateTime))
            {
                var date = model.FullPaperSubmissionOpenDate.Value;
                if (date.Kind == DateTimeKind.Unspecified)
                {
                    model.FullPaperSubmissionOpenDate = TimeZoneInfo.ConvertTimeToUtc(date, localTimeZone);
                }
                else if (date.Kind == DateTimeKind.Local)
                {
                    model.FullPaperSubmissionOpenDate = date.ToUniversalTime();
                }
                // If already UTC, keep as is
            }
            else
            {
                model.FullPaperSubmissionOpenDate = null;
            }

            if (model.FullPaperSubmissionDeadline.HasValue && model.FullPaperSubmissionDeadline.Value != default(DateTime))
            {
                var date = model.FullPaperSubmissionDeadline.Value;
                if (date.Kind == DateTimeKind.Unspecified)
                {
                    model.FullPaperSubmissionDeadline = TimeZoneInfo.ConvertTimeToUtc(date, localTimeZone);
                }
                else if (date.Kind == DateTimeKind.Local)
                {
                    model.FullPaperSubmissionDeadline = date.ToUniversalTime();
                }
            }
            else
            {
                model.FullPaperSubmissionDeadline = null;
            }

            if (model.ReviewDeadline.HasValue && model.ReviewDeadline.Value != default(DateTime))
            {
                var date = model.ReviewDeadline.Value;
                if (date.Kind == DateTimeKind.Unspecified)
                {
                    model.ReviewDeadline = TimeZoneInfo.ConvertTimeToUtc(date, localTimeZone);
                }
                else if (date.Kind == DateTimeKind.Local)
                {
                    model.ReviewDeadline = date.ToUniversalTime();
                }
            }
            else
            {
                model.ReviewDeadline = null;
            }

            if (model.ResultAnnouncementDate.HasValue && model.ResultAnnouncementDate.Value != default(DateTime))
            {
                var date = model.ResultAnnouncementDate.Value;
                if (date.Kind == DateTimeKind.Unspecified)
                {
                    model.ResultAnnouncementDate = TimeZoneInfo.ConvertTimeToUtc(date, localTimeZone);
                }
                else if (date.Kind == DateTimeKind.Local)
                {
                    model.ResultAnnouncementDate = date.ToUniversalTime();
                }
            }
            else
            {
                model.ResultAnnouncementDate = null;
            }

            if (model.ConferenceDate.HasValue && model.ConferenceDate.Value != default(DateTime))
            {
                var date = model.ConferenceDate.Value;
                if (date.Kind == DateTimeKind.Unspecified)
                {
                    model.ConferenceDate = TimeZoneInfo.ConvertTimeToUtc(date, localTimeZone);
                }
                else if (date.Kind == DateTimeKind.Local)
                {
                    model.ConferenceDate = date.ToUniversalTime();
                }
            }
            else
            {
                model.ConferenceDate = null;
            }

            var result = await _adminService.UpdateConferencePlanAsync(model);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã cập nhật lịch trình hội thảo thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật lịch trình hội thảo.";
            }
            return RedirectToAction(nameof(Conference));
        }

        public async Task<IActionResult> Users(UserFilterViewModel? filter)
        {
            filter ??= new UserFilterViewModel();
            var users = await _adminService.GetUsersAsync(filter);
            
            ViewBag.Filter = filter;
            ViewBag.RoleOptions = Enum.GetNames(typeof(Models.Enums.UserRole));
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.RoleOptions = Enum.GetNames(typeof(Models.Enums.UserRole));
            return View(new Models.Admin.CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(Models.Admin.CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleOptions = Enum.GetNames(typeof(Models.Enums.UserRole));
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
            
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                ViewBag.RoleOptions = Enum.GetNames(typeof(Models.Enums.UserRole));
                return View(model);
            }

            var result = await _adminService.CreateUserAsync(model);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã tạo người dùng thành công!";
                return RedirectToAction(nameof(Users));
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tạo người dùng. Email có thể đã tồn tại.";
                ViewBag.RoleOptions = Enum.GetNames(typeof(Models.Enums.UserRole));
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, string newRole)
        {
            var result = await _adminService.UpdateUserRoleAsync(userId, newRole);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã cập nhật vai trò người dùng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật vai trò người dùng.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var result = await _adminService.DeactivateUserAsync(userId);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã vô hiệu hóa người dùng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể vô hiệu hóa người dùng.";
            }
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Fields()
        {
            var topics = await _adminService.GetTopicsAsync();
            return View(topics);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests - token is validated manually if needed
        public async Task<IActionResult> CreateTopic([FromBody] TopicViewModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên lĩnh vực." });
            }

            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hội nghị đang hoạt động. Vui lòng tạo hội nghị trước." });
            }

            var topics = await _context.Topics
                .Where(t => t.ConferenceId == activeConference.Id)
                .ToListAsync();
            var exists = topics.Any(t => t.Name.Trim().Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return Json(new { success = false, message = "Lĩnh vực này đã tồn tại trong hội nghị đang hoạt động." });
            }

            var result = await _adminService.CreateTopicAsync(model);
            if (result)
            {
                return Json(new { success = true, message = "Đã thêm lĩnh vực thành công!" });
            }
            return Json(new { success = false, message = "Không thể thêm lĩnh vực. Vui lòng kiểm tra lại." });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests - token is validated manually if needed
        public async Task<IActionResult> UpdateTopic(int id, [FromBody] TopicViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên lĩnh vực." });
            }

            if (id <= 0)
            {
                return Json(new { success = false, message = "ID lĩnh vực không hợp lệ." });
            }

            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hội nghị đang hoạt động." });
            }

            var topics = await _context.Topics
                .Where(t => t.ConferenceId == activeConference.Id && t.Id != id)
                .ToListAsync();
            var exists = topics.Any(t => t.Name.Trim().Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return Json(new { success = false, message = "Tên lĩnh vực này đã tồn tại trong hội nghị đang hoạt động." });
            }

            var result = await _adminService.UpdateTopicAsync(id, model);
            if (result)
            {
                return Json(new { success = true, message = "Đã cập nhật lĩnh vực thành công!" });
            }
            return Json(new { success = false, message = "Không thể cập nhật lĩnh vực. Vui lòng kiểm tra lại." });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests - token is validated manually if needed
        public async Task<IActionResult> DeleteTopic([FromBody] DeleteTopicRequest request)
        {
            if (request == null || request.Id <= 0)
            {
                return Json(new { success = false, message = "ID lĩnh vực không hợp lệ." });
            }

            var result = await _adminService.DeleteTopicAsync(request.Id);
            if (result)
            {
                return Json(new { success = true, message = "Đã xóa lĩnh vực thành công!" });
            }
            return Json(new { success = false, message = "Không thể xóa lĩnh vực. Lĩnh vực này đang được sử dụng." });
        }

        public async Task<IActionResult> Keywords(KeywordFilterViewModel? filter)
        {
            filter ??= new KeywordFilterViewModel();
            var keywords = await _adminService.GetKeywordsAsync(filter);
            
            ViewBag.Filter = filter;
            ViewBag.StatusOptions = Enum.GetNames(typeof(KeywordStatus));
            return View(keywords);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests
        public async Task<IActionResult> CreateKeyword([FromBody] CreateKeywordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên từ khóa." });
            }

            // TODO: Get userId from current user
            var userId = 1; // Placeholder

            var result = await _adminService.CreateKeywordAsync(request.Name, userId);
            if (result)
            {
                return Json(new { success = true, message = "Đã thêm từ khóa thành công!" });
            }
            return Json(new { success = false, message = "Từ khóa đã tồn tại hoặc không thể thêm." });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests
        public async Task<IActionResult> ApproveKeyword([FromBody] KeywordActionRequest request)
        {
            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder

            var result = await _adminService.ApproveKeywordAsync(request.KeywordId, adminId);
            if (result)
            {
                return Json(new { success = true, message = "Đã duyệt từ khóa thành công!" });
            }
            return Json(new { success = false, message = "Không thể duyệt từ khóa." });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests
        public async Task<IActionResult> RejectKeyword([FromBody] KeywordActionRequest request)
        {
            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder

            var result = await _adminService.RejectKeywordAsync(request.KeywordId, adminId);
            if (result)
            {
                return Json(new { success = true, message = "Đã từ chối từ khóa thành công!" });
            }
            return Json(new { success = false, message = "Không thể từ chối từ khóa. Từ khóa này đang được sử dụng." });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests
        public async Task<IActionResult> DeleteKeyword([FromBody] KeywordActionRequest request)
        {
            var result = await _adminService.DeleteKeywordAsync(request.KeywordId);
            if (result)
            {
                return Json(new { success = true, message = "Đã xóa từ khóa thành công!" });
            }
            return Json(new { success = false, message = "Không thể xóa từ khóa. Từ khóa này đang được sử dụng." });
        }

        public async Task<IActionResult> Reports()
        {
            var stats = await _adminService.GetReportStatisticsAsync();
            return View(stats);
        }

        public async Task<IActionResult> Settings()
        {
            var settings = await _adminService.GetSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _adminService.UpdateSettingsAsync(model);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã cập nhật cài đặt thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật cài đặt.";
            }
            return RedirectToAction(nameof(Settings));
        }

        // Interface Settings
        public async Task<IActionResult> InterfaceSettings()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "No active conference found.";
                return View(new Models.Admin.InterfaceSettingsViewModel());
            }

            var settings = await _context.SystemSettings
                .Where(s => s.ConferenceId == activeConference.Id)
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var viewModel = new Models.Admin.InterfaceSettingsViewModel
            {
                HeroTitle = settings.GetValueOrDefault("Interface.HeroTitle", "Welcome to Scientific Conference Management System"),
                HeroSubtitle = settings.GetValueOrDefault("Interface.HeroSubtitle", "A professional platform for managing, submitting, and evaluating scientific research works."),
                ConferenceDate = activeConference.StartDate?.ToString("yyyy-MM-ddTHH:mm"),
                ConferenceLocation = settings.GetValueOrDefault("Interface.ConferenceLocation", activeConference.Location ?? ""),
                HeroBackgroundColor = settings.GetValueOrDefault("Interface.HeroBackgroundColor"),
                HeroBackgroundImage = settings.GetValueOrDefault("Interface.HeroBackgroundImage"),
                EnableAnimations = bool.Parse(settings.GetValueOrDefault("Interface.EnableAnimations", "true") ?? "true"),
                AnimationSpeed = settings.GetValueOrDefault("Interface.AnimationSpeed", "normal"),
                EnableParticles = bool.Parse(settings.GetValueOrDefault("Interface.EnableParticles", "true") ?? "true"),
                ParticleDensity = settings.GetValueOrDefault("Interface.ParticleDensity", "medium"),
                EnableLightStreaks = bool.Parse(settings.GetValueOrDefault("Interface.EnableLightStreaks", "true") ?? "true"),
                LightIntensity = settings.GetValueOrDefault("Interface.LightIntensity", "medium"),
                GradientColor = settings.GetValueOrDefault("Interface.GradientColor", "#3b82f6"),
                ShowStatistics = bool.Parse(settings.GetValueOrDefault("Interface.ShowStatistics", "true") ?? "true"),
                ShowCountdown = bool.Parse(settings.GetValueOrDefault("Interface.ShowCountdown", "true") ?? "true"),
                ShowProgressSteps = bool.Parse(settings.GetValueOrDefault("Interface.ShowProgressSteps", "true") ?? "true"),
                ShowQuickActions = bool.Parse(settings.GetValueOrDefault("Interface.ShowQuickActions", "true") ?? "true"),
                ShowRecentSubmissions = bool.Parse(settings.GetValueOrDefault("Interface.ShowRecentSubmissions", "true") ?? "true")
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInterfaceSettings(Models.Admin.InterfaceSettingsViewModel model)
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "No active conference found.";
                return RedirectToAction(nameof(InterfaceSettings));
            }

            var settingsToUpdate = new Dictionary<string, string?>
            {
                { "Interface.HeroTitle", model.HeroTitle },
                { "Interface.HeroSubtitle", model.HeroSubtitle },
                { "Interface.ConferenceLocation", model.ConferenceLocation },
                { "Interface.HeroBackgroundColor", model.HeroBackgroundColor },
                { "Interface.HeroBackgroundImage", model.HeroBackgroundImage },
                { "Interface.EnableAnimations", model.EnableAnimations.ToString() },
                { "Interface.AnimationSpeed", model.AnimationSpeed },
                { "Interface.EnableParticles", model.EnableParticles.ToString() },
                { "Interface.ParticleDensity", model.ParticleDensity },
                { "Interface.EnableLightStreaks", model.EnableLightStreaks.ToString() },
                { "Interface.LightIntensity", model.LightIntensity },
                { "Interface.GradientColor", model.GradientColor },
                { "Interface.ShowStatistics", model.ShowStatistics.ToString() },
                { "Interface.ShowCountdown", model.ShowCountdown.ToString() },
                { "Interface.ShowProgressSteps", model.ShowProgressSteps.ToString() },
                { "Interface.ShowQuickActions", model.ShowQuickActions.ToString() },
                { "Interface.ShowRecentSubmissions", model.ShowRecentSubmissions.ToString() }
            };

            foreach (var kvp in settingsToUpdate)
            {
                var existingSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.ConferenceId == activeConference.Id && s.Key == kvp.Key);

                if (existingSetting != null)
                {
                    existingSetting.Value = kvp.Value;
                    existingSetting.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newSetting = new Models.Conference.SystemSetting
                    {
                        ConferenceId = activeConference.Id,
                        Key = kvp.Key,
                        Value = kvp.Value,
                        Description = $"Interface setting: {kvp.Key}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SystemSettings.Add(newSetting);
                }
            }

            // Update conference date if provided
            if (!string.IsNullOrEmpty(model.ConferenceDate) && DateTime.TryParse(model.ConferenceDate, out var date))
            {
                activeConference.StartDate = date;
                activeConference.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Interface settings updated successfully!";
            return RedirectToAction(nameof(InterfaceSettings));
        }

        // Timeline Management
        public async Task<IActionResult> Timeline()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "No active conference found.";
                return View(new TimelineViewModel());
            }

            var timelineSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.ConferenceId == activeConference.Id && s.Key == "Dashboard.Timeline");

            var viewModel = new TimelineViewModel();
            
            if (timelineSetting != null && !string.IsNullOrEmpty(timelineSetting.Value))
            {
                try
                {
                    viewModel.Items = JsonSerializer.Deserialize<List<TimelineItem>>(timelineSetting.Value) ?? new List<TimelineItem>();
                }
                catch
                {
                    viewModel.Items = new List<TimelineItem>();
                }
            }

            // Sort by order
            viewModel.Items = viewModel.Items.OrderBy(i => i.Order).ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTimeline(TimelineViewModel model)
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "No active conference found.";
                return RedirectToAction(nameof(Timeline));
            }

            // Assign IDs to new items
            int maxId = 0;
            if (model.Items != null && model.Items.Any())
            {
                maxId = model.Items.Where(i => i.Id > 0).DefaultIfEmpty(new TimelineItem { Id = 0 }).Max(i => i.Id);
                foreach (var item in model.Items.Where(i => i.Id == 0))
                {
                    item.Id = ++maxId;
                }

                // Sort by order
                model.Items = model.Items.OrderBy(i => i.Order).ToList();
            }

            var timelineJson = JsonSerializer.Serialize(model.Items ?? new List<TimelineItem>());

            var timelineSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.ConferenceId == activeConference.Id && s.Key == "Dashboard.Timeline");

            if (timelineSetting != null)
            {
                timelineSetting.Value = timelineJson;
                timelineSetting.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                timelineSetting = new Models.Conference.SystemSetting
                {
                    ConferenceId = activeConference.Id,
                    Key = "Dashboard.Timeline",
                    Value = timelineJson,
                    Description = "Dashboard timeline items",
                    CreatedAt = DateTime.UtcNow
                };
                _context.SystemSettings.Add(timelineSetting);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Timeline updated successfully!";
            return RedirectToAction(nameof(Timeline));
        }

        // Full Papers Management
        public async Task<IActionResult> FullPapers()
        {
            var fullPapers = await _context.FullPaperVersions
                .Include(f => f.Submission)
                    .ThenInclude(s => s.Author)
                .Include(f => f.Submission)
                    .ThenInclude(s => s.ReviewAssignments)
                        .ThenInclude(ra => ra.Reviewer)
                .Where(f => f.IsCurrentVersion)
                .OrderByDescending(f => f.UploadedAt)
                .Select(f => new Models.Admin.FullPaperViewModel
                {
                    Id = f.Id,
                    SubmissionId = f.SubmissionId,
                    Title = f.Submission.Title,
                    AuthorName = f.Submission.Author.FullName,
                    VersionNumber = f.VersionNumber,
                    FileName = f.FileName,
                    FileUrl = f.FileUrl,
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt,
                    IsCurrentVersion = f.IsCurrentVersion,
                    AssignedReviewers = f.Submission.ReviewAssignments
                        .Select(ra => new Models.Admin.ReviewerAssignmentViewModel
                        {
                            ReviewAssignmentId = ra.Id,
                            ReviewerId = ra.ReviewerId,
                            ReviewerName = ra.Reviewer.FullName,
                            ReviewerEmail = ra.Reviewer.Email,
                            Status = ra.Status.ToString(),
                            Deadline = ra.Deadline
                        }).ToList(),
                    CanAssignReviewer = f.Submission.Status == Models.Enums.SubmissionStatus.AbstractApproved || 
                                       f.Submission.Status == Models.Enums.SubmissionStatus.UnderReview
                })
                .ToListAsync();

            // Get available reviewers
            ViewBag.Reviewers = await _context.Users
                .Where(u => u.Role == Models.Enums.UserRole.Reviewer)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            return View(fullPapers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReviewers(int submissionId, List<int> reviewerIds)
        {
            // Filter out empty values (0 or null)
            reviewerIds = reviewerIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            
            if (reviewerIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một reviewer.";
                return RedirectToAction(nameof(FullPapers));
            }

            if (reviewerIds.Count > 3)
            {
                TempData["ErrorMessage"] = "Tối đa chỉ được chọn 3 reviewers cho mỗi bài báo.";
                return RedirectToAction(nameof(FullPapers));
            }
            
            // Check for duplicates
            if (reviewerIds.Count != reviewerIds.Distinct().Count())
            {
                TempData["ErrorMessage"] = "Bạn không thể chọn cùng một reviewer nhiều lần.";
                return RedirectToAction(nameof(FullPapers));
            }

            var submission = await _context.Submissions
                .Include(s => s.ReviewAssignments)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                TempData["ErrorMessage"] = "Submission not found.";
                return RedirectToAction(nameof(FullPapers));
            }

            // Remove existing assignments for this submission
            var existingAssignments = submission.ReviewAssignments.ToList();
            foreach (var assignment in existingAssignments)
            {
                _context.ReviewAssignments.Remove(assignment);
            }

            // Create new assignments
            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder
            var deadline = DateTime.UtcNow.AddDays(14); // Default 14 days deadline

            foreach (var reviewerId in reviewerIds)
            {
                var reviewer = await _context.Users.FindAsync(reviewerId);
                if (reviewer == null || reviewer.Role != Models.Enums.UserRole.Reviewer)
                {
                    continue;
                }

                var assignment = new Models.Review.ReviewAssignment
                {
                    SubmissionId = submissionId,
                    ReviewerId = reviewerId,
                    Status = Models.Enums.ReviewAssignmentStatus.Pending,
                    InvitedAt = DateTime.UtcNow,
                    InvitedBy = adminId,
                    Deadline = deadline,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReviewAssignments.Add(assignment);
            }

            // Update submission status if needed
            if (submission.Status == Models.Enums.SubmissionStatus.AbstractApproved)
            {
                submission.Status = Models.Enums.SubmissionStatus.UnderReview;
                submission.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Successfully assigned {reviewerIds.Count} reviewer(s) to the paper.";
            return RedirectToAction(nameof(FullPapers));
        }

        [HttpPost]
        public async Task<IActionResult> AddTestReviewers()
        {
            try
            {
                // Simple password hashing (same as DbInitializer)
                string HashPassword(string password)
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "SciSubmitSalt"));
                        return Convert.ToBase64String(hashedBytes);
                    }
                }

                // Check if reviewers already exist
                var existingReviewers = await _context.Users
                    .Where(u => u.Role == Models.Enums.UserRole.Reviewer && u.IsActive)
                    .ToListAsync();

                if (existingReviewers.Count >= 5)
                {
                    TempData["InfoMessage"] = "Đã có đủ reviewers trong hệ thống.";
                    return RedirectToAction(nameof(FullPapers));
                }

                // Create test reviewers
                var reviewers = new List<Models.Identity.User>();

                // Reviewer 1: GS. Nguyễn Văn X
                if (!existingReviewers.Any(r => r.Email == "reviewer1@scisubmit.com"))
                {
                    reviewers.Add(new Models.Identity.User
                    {
                        Email = "reviewer1@scisubmit.com",
                        PasswordHash = HashPassword("Reviewer@123"),
                        FullName = "GS. Nguyễn Văn X",
                        Affiliation = "Đại học ABC",
                        Role = Models.Enums.UserRole.Reviewer,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Reviewer 2: PGS. Trần Thị Y
                if (!existingReviewers.Any(r => r.Email == "reviewer2@scisubmit.com"))
                {
                    reviewers.Add(new Models.Identity.User
                    {
                        Email = "reviewer2@scisubmit.com",
                        PasswordHash = HashPassword("Reviewer@123"),
                        FullName = "PGS. Trần Thị Y",
                        Affiliation = "Đại học XYZ",
                        Role = Models.Enums.UserRole.Reviewer,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Reviewer 3: TS. Phạm Văn Z
                if (!existingReviewers.Any(r => r.Email == "reviewer3@scisubmit.com"))
                {
                    reviewers.Add(new Models.Identity.User
                    {
                        Email = "reviewer3@scisubmit.com",
                        PasswordHash = HashPassword("Reviewer@123"),
                        FullName = "TS. Phạm Văn Z",
                        Affiliation = "Đại học DEF",
                        Role = Models.Enums.UserRole.Reviewer,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Reviewer 4: PGS.TS. Lê Thị W
                if (!existingReviewers.Any(r => r.Email == "reviewer4@scisubmit.com"))
                {
                    reviewers.Add(new Models.Identity.User
                    {
                        Email = "reviewer4@scisubmit.com",
                        PasswordHash = HashPassword("Reviewer@123"),
                        FullName = "PGS.TS. Lê Thị W",
                        Affiliation = "Đại học GHI",
                        Role = Models.Enums.UserRole.Reviewer,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Reviewer 5: dương thị thanh thảo
                if (!existingReviewers.Any(r => r.Email == "reviewer5@scisubmit.com"))
                {
                    reviewers.Add(new Models.Identity.User
                    {
                        Email = "reviewer5@scisubmit.com",
                        PasswordHash = HashPassword("Reviewer@123"),
                        FullName = "dương thị thanh thảo",
                        Affiliation = "Đại học JKL",
                        Role = Models.Enums.UserRole.Reviewer,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (reviewers.Any())
                {
                    _context.Users.AddRange(reviewers);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã thêm {reviewers.Count} reviewers vào database thành công!";
                }
                else
                {
                    TempData["InfoMessage"] = "Tất cả reviewers test đã tồn tại trong hệ thống.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding test reviewers");
                TempData["ErrorMessage"] = $"Lỗi khi thêm reviewers: {ex.Message}";
            }

            return RedirectToAction(nameof(FullPapers));
        }

        // Keynote Speakers Management
        public async Task<IActionResult> KeynoteSpeakers()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hội nghị đang hoạt động.";
                return View(new List<Models.Conference.KeynoteSpeaker>());
            }

            var speakers = await _context.KeynoteSpeakers
                .Where(k => k.ConferenceId == activeConference.Id)
                .OrderBy(k => k.OrderIndex)
                .ThenBy(k => k.Id)
                .ToListAsync();

            return View(speakers);
        }

        [HttpGet]
        public IActionResult CreateKeynoteSpeaker()
        {
            return View(new Models.Conference.KeynoteSpeaker());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateKeynoteSpeaker(Models.Conference.KeynoteSpeaker model)
        {
            // Remove ConferenceId and Conference from validation - they will be set automatically
            ModelState.Remove(nameof(model.ConferenceId));
            ModelState.Remove(nameof(model.Conference));
            
            // Clear PhotoUrl validation if empty
            if (string.IsNullOrWhiteSpace(model.PhotoUrl))
            {
                ModelState.Remove(nameof(model.PhotoUrl));
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hội nghị đang hoạt động.";
                return View(model);
            }

            try
            {
                model.ConferenceId = activeConference.Id;
                model.CreatedAt = DateTime.UtcNow;
                model.IsActive = true;

                _context.KeynoteSpeakers.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm diễn giả chính thành công!";
                return RedirectToAction(nameof(KeynoteSpeakers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating keynote speaker");
                TempData["ErrorMessage"] = $"Lỗi khi thêm diễn giả: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditKeynoteSpeaker(int id)
        {
            var speaker = await _context.KeynoteSpeakers.FindAsync(id);
            if (speaker == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy diễn giả.";
                return RedirectToAction(nameof(KeynoteSpeakers));
            }
            return View(speaker);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditKeynoteSpeaker(Models.Conference.KeynoteSpeaker model)
        {
            // Clear PhotoUrl validation if empty
            if (string.IsNullOrWhiteSpace(model.PhotoUrl))
            {
                ModelState.Remove(nameof(model.PhotoUrl));
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            var speaker = await _context.KeynoteSpeakers.FindAsync(model.Id);
            if (speaker == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy diễn giả.";
                return RedirectToAction(nameof(KeynoteSpeakers));
            }

            try
            {
                speaker.Name = model.Name;
                speaker.Title = model.Title;
                speaker.Affiliation = model.Affiliation;
                speaker.Biography = model.Biography;
                speaker.Topic = model.Topic;
                speaker.PhotoUrl = model.PhotoUrl;
                speaker.OrderIndex = model.OrderIndex;
                speaker.IsActive = model.IsActive;
                speaker.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã cập nhật diễn giả chính thành công!";
                return RedirectToAction(nameof(KeynoteSpeakers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating keynote speaker");
                TempData["ErrorMessage"] = $"Lỗi khi cập nhật diễn giả: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKeynoteSpeaker(int id)
        {
            var speaker = await _context.KeynoteSpeakers.FindAsync(id);
            if (speaker == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy diễn giả.";
                return RedirectToAction(nameof(KeynoteSpeakers));
            }

            _context.KeynoteSpeakers.Remove(speaker);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa diễn giả chính thành công!";
            return RedirectToAction(nameof(KeynoteSpeakers));
        }

        // Conference Location Management
        public async Task<IActionResult> ConferenceLocation()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hội nghị đang hoạt động.";
                return View(new Models.Conference.ConferenceLocation());
            }

            var location = await _context.ConferenceLocations
                .Where(l => l.ConferenceId == activeConference.Id)
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return View(new Models.Conference.ConferenceLocation());
            }

            return View(location);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConferenceLocation(Models.Conference.ConferenceLocation model)
        {
            // Remove ConferenceId from validation
            ModelState.Remove(nameof(model.ConferenceId));
            ModelState.Remove(nameof(model.Conference));

            if (!ModelState.IsValid)
            {
                return View("ConferenceLocation", model);
            }

            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hội nghị đang hoạt động.";
                return View("ConferenceLocation", model);
            }

            // Geocode address using OpenStreetMap Nominatim API
            if (!string.IsNullOrWhiteSpace(model.Address))
            {
                var geocodeResult = await GeocodeAddressAsync(model.Address, model.Name);
                if (geocodeResult != null)
                {
                    model.Latitude = geocodeResult.Latitude;
                    model.Longitude = geocodeResult.Longitude;
                    if (!string.IsNullOrWhiteSpace(geocodeResult.City))
                        model.City = geocodeResult.City;
                    if (!string.IsNullOrWhiteSpace(geocodeResult.Country))
                        model.Country = geocodeResult.Country;
                }
            }

            if (model.Id == 0)
            {
                model.ConferenceId = activeConference.Id;
                model.CreatedAt = DateTime.UtcNow;
                model.IsActive = true;
                _context.ConferenceLocations.Add(model);
            }
            else
            {
                var location = await _context.ConferenceLocations.FindAsync(model.Id);
                if (location == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy địa điểm.";
                    return View("ConferenceLocation", model);
                }

                location.Name = model.Name;
                location.Address = model.Address;
                location.City = model.City;
                location.Country = model.Country;
                location.Latitude = model.Latitude;
                location.Longitude = model.Longitude;
                location.Description = model.Description;
                location.IsActive = model.IsActive;
                location.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Check if coordinates were successfully geocoded (check the saved entity)
            var savedLocation = model.Id == 0 
                ? await _context.ConferenceLocations.OrderByDescending(l => l.Id).FirstOrDefaultAsync()
                : await _context.ConferenceLocations.FindAsync(model.Id);
            
            if (savedLocation != null && !string.IsNullOrWhiteSpace(savedLocation.Latitude) && !string.IsNullOrWhiteSpace(savedLocation.Longitude))
            {
                TempData["SuccessMessage"] = "Đã lưu thông tin địa điểm thành công! Tọa độ đã được tự động lấy từ địa chỉ.";
            }
            else
            {
                TempData["WarningMessage"] = "Đã lưu thông tin địa điểm, nhưng không thể lấy tọa độ tự động. Vui lòng kiểm tra lại địa chỉ hoặc thử lại sau.";
            }
            
            return RedirectToAction(nameof(ConferenceLocation));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReGeocodeLocation(int id)
        {
            var location = await _context.ConferenceLocations.FindAsync(id);
            if (location == null || string.IsNullOrWhiteSpace(location.Address))
            {
                TempData["ErrorMessage"] = "Không tìm thấy địa điểm hoặc địa chỉ trống.";
                return RedirectToAction(nameof(ConferenceLocation));
            }

            var geocodeResult = await GeocodeAddressAsync(location.Address, location.Name);
            
            if (geocodeResult != null)
            {
                location.Latitude = geocodeResult.Latitude;
                location.Longitude = geocodeResult.Longitude;
                if (!string.IsNullOrWhiteSpace(geocodeResult.City))
                    location.City = geocodeResult.City;
                if (!string.IsNullOrWhiteSpace(geocodeResult.Country))
                    location.Country = geocodeResult.Country;
                location.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Re-geocoding successful. Coordinates: {Lat}, {Lon}", location.Latitude, location.Longitude);
                TempData["SuccessMessage"] = $"Đã lấy tọa độ thành công! Latitude: {location.Latitude}, Longitude: {location.Longitude}";
            }
            else
            {
                TempData["WarningMessage"] = "Không thể lấy tọa độ tự động. Vui lòng kiểm tra lại địa chỉ hoặc thử lại sau.";
            }
            
            return RedirectToAction(nameof(ConferenceLocation));
        }

        // Helper method for geocoding addresses
        private async Task<GeocodeResult?> GeocodeAddressAsync(string address, string? locationName = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            // Generate multiple address formats to try
            var addressFormats = new List<string>();
            
            // Priority 1: Try with location name + city (most likely to work for famous places)
            if (!string.IsNullOrWhiteSpace(locationName))
            {
                // Extract city from address or use common cities
                string city = "Ho Chi Minh City";
                if (address.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase) || 
                    address.Contains("Hanoi", StringComparison.OrdinalIgnoreCase))
                    city = "Hanoi";
                else if (address.Contains("Đà Nẵng", StringComparison.OrdinalIgnoreCase) || 
                         address.Contains("Da Nang", StringComparison.OrdinalIgnoreCase))
                    city = "Da Nang";
                
                // Try: "Opera House Ho Chi Minh City" format
                addressFormats.Add($"{locationName} {city}");
                addressFormats.Add($"{locationName}, {city}");
            }
            
            // Priority 2: Translate location name to English if it's Vietnamese
            if (!string.IsNullOrWhiteSpace(locationName))
            {
                var translatedName = locationName
                    .Replace("NHÀ HÁT THÀNH PHỐ", "Opera House", StringComparison.OrdinalIgnoreCase)
                    .Replace("NHÀ HÁT", "Opera House", StringComparison.OrdinalIgnoreCase)
                    .Replace("THÀNH PHỐ", "City", StringComparison.OrdinalIgnoreCase);
                
                if (translatedName != locationName)
                {
                    string city = "Ho Chi Minh City";
                    if (address.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase))
                        city = "Hanoi";
                    else if (address.Contains("Đà Nẵng", StringComparison.OrdinalIgnoreCase))
                        city = "Da Nang";
                    
                    addressFormats.Add($"{translatedName} {city}");
                    addressFormats.Add($"{translatedName}, {city}");
                }
            }
            
            // Priority 3: Original address
            addressFormats.Add(address);
            
            // Priority 4: Add Vietnam if not present
            if (!address.Contains("Vietnam", StringComparison.OrdinalIgnoreCase) && 
                !address.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase))
            {
                addressFormats.Add($"{address}, Vietnam");
            }
            
            // Priority 5: Try with location name + address
            if (!string.IsNullOrWhiteSpace(locationName))
            {
                addressFormats.Add($"{locationName}, {address}");
                if (!address.Contains("Vietnam", StringComparison.OrdinalIgnoreCase))
                {
                    addressFormats.Add($"{locationName}, {address}, Vietnam");
                }
            }
            
            // Priority 6: Translate common Vietnamese city names to English
            var translatedAddress = address
                .Replace("Thành phố Hồ Chí Minh", "Ho Chi Minh City", StringComparison.OrdinalIgnoreCase)
                .Replace("TP. Hồ Chí Minh", "Ho Chi Minh City", StringComparison.OrdinalIgnoreCase)
                .Replace("TP HCM", "Ho Chi Minh City", StringComparison.OrdinalIgnoreCase)
                .Replace("Hà Nội", "Hanoi", StringComparison.OrdinalIgnoreCase)
                .Replace("Đà Nẵng", "Da Nang", StringComparison.OrdinalIgnoreCase)
                .Replace("Quận 1", "District 1", StringComparison.OrdinalIgnoreCase)
                .Replace("Quận 2", "District 2", StringComparison.OrdinalIgnoreCase)
                .Replace("Quận 3", "District 3", StringComparison.OrdinalIgnoreCase);
            
            if (translatedAddress != address)
            {
                addressFormats.Add(translatedAddress);
                if (!translatedAddress.Contains("Vietnam", StringComparison.OrdinalIgnoreCase))
                {
                    addressFormats.Add($"{translatedAddress}, Vietnam");
                }
            }
            
            // Priority 7: Try with just main parts of address
            var addressParts = address.Split(',');
            if (addressParts.Length > 2)
            {
                var mainAddress = string.Join(",", addressParts.Take(3));
                addressFormats.Add($"{mainAddress}, Vietnam");
            }
            
            // Remove duplicates
            addressFormats = addressFormats.Distinct().ToList();
            
            int maxRetries = addressFormats.Count;
            int retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    // Add delay for rate limiting (OpenStreetMap requires 1 second between requests)
                    if (retryCount > 0)
                    {
                        await Task.Delay(3000); // Wait 3 seconds before retry (be respectful to OSM)
                    }
                    
                    using (var httpClient = new HttpClient())
                    {
                        // OpenStreetMap requires User-Agent header
                        httpClient.DefaultRequestHeaders.Clear();
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "SciSubmit/1.0 (contact@scisubmit.com)");
                        httpClient.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        
                        string addressToGeocode = addressFormats[retryCount];
                        var encodedAddress = Uri.EscapeDataString(addressToGeocode);
                        var geocodeUrl = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1&addressdetails=1";
                        
                        _logger.LogInformation("Geocoding address (attempt {Attempt}/{Total}): {Address}", 
                            retryCount + 1, maxRetries, addressToGeocode);
                        
                        var response = await httpClient.GetStringAsync(geocodeUrl);
                        
                        _logger.LogInformation("OpenStreetMap response (attempt {Attempt}): {Response}", retryCount + 1, 
                            response?.Length > 300 ? response.Substring(0, 300) + "..." : response);
                        
                        if (string.IsNullOrWhiteSpace(response) || response.Trim() == "[]")
                        {
                            _logger.LogWarning("Empty or no results from OpenStreetMap geocoding (attempt {Attempt})", retryCount + 1);
                            retryCount++;
                            continue;
                        }
                        
                        var results = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(response);
                        
                        if (results != null && results.Length > 0)
                        {
                            var firstResult = results[0];
                            if (firstResult.TryGetProperty("lat", out var latElement) && 
                                firstResult.TryGetProperty("lon", out var lonElement))
                            {
                                var lat = latElement.GetString();
                                var lon = lonElement.GetString();
                                
                                if (!string.IsNullOrWhiteSpace(lat) && !string.IsNullOrWhiteSpace(lon))
                                {
                                    _logger.LogInformation("Geocoding successful! Coordinates: {Lat}, {Lon} (from format: {Format})", 
                                        lat, lon, addressToGeocode);
                                    
                                    var result = new GeocodeResult
                                    {
                                        Latitude = lat,
                                        Longitude = lon
                                    };
                                    
                                    // Extract city and country from address components if available
                                    if (firstResult.TryGetProperty("address", out var addressObj))
                                    {
                                        if (addressObj.TryGetProperty("city", out var cityElement))
                                        {
                                            result.City = cityElement.GetString();
                                        }
                                        else if (addressObj.TryGetProperty("town", out var townElement))
                                        {
                                            result.City = townElement.GetString();
                                        }
                                        else if (addressObj.TryGetProperty("municipality", out var municipalityElement))
                                        {
                                            result.City = municipalityElement.GetString();
                                        }
                                        
                                        if (addressObj.TryGetProperty("country", out var countryElement))
                                        {
                                            result.Country = countryElement.GetString();
                                        }
                                    }
                                    
                                    return result;
                                }
                            }
                        }
                        
                        retryCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to geocode address (attempt {Attempt}): {Address}", 
                        retryCount + 1, addressFormats[retryCount]);
                    retryCount++;
                }
            }
            
            _logger.LogError("All geocoding attempts failed for address: {Address}", address);
            return null;
        }
        
        // Helper class for geocoding result
        private class GeocodeResult
        {
            public string Latitude { get; set; } = string.Empty;
            public string Longitude { get; set; } = string.Empty;
            public string? City { get; set; }
            public string? Country { get; set; }
        }
    }

    // Request models for JSON actions
    public class CreateKeywordRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class KeywordActionRequest
    {
        public int KeywordId { get; set; }
    }

    public class DeleteTopicRequest
    {
        public int Id { get; set; }
    }
}
