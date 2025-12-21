using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Services;
using SciSubmit.Models.Admin;
using SciSubmit.Models.Enums;
using SciSubmit.Data;

namespace SciSubmit.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;

        public AdminController(IAdminService adminService, ApplicationDbContext context)
        {
            _adminService = adminService;
            _context = context;
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
            return RedirectToAction(nameof(Submissions));
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
            return RedirectToAction(nameof(Submissions));
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
            if (!submissionId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn bài báo.";
                return RedirectToAction(nameof(Assignments));
            }

            var availableReviewers = await _adminService.GetAvailableReviewersAsync(submissionId.Value);
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == submissionId.Value);

            ViewBag.AvailableReviewers = availableReviewers;
            ViewBag.Submission = submission;
            ViewBag.SubmissionId = submissionId.Value;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReviewer(int submissionId, int reviewerId, DateTime deadline)
        {
            // TODO: Get adminId from current user
            var adminId = 1; // Placeholder
            
            var result = await _adminService.AssignReviewerAsync(submissionId, reviewerId, deadline, adminId);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã phân công phản biện thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể phân công phản biện. Vui lòng kiểm tra lại.";
            }
            return RedirectToAction(nameof(Assignments));
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
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Conference));
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTopic([FromBody] TopicViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var result = await _adminService.CreateTopicAsync(model);
            if (result)
            {
                return Json(new { success = true, message = "Đã thêm lĩnh vực thành công!" });
            }
            return Json(new { success = false, message = "Không thể thêm lĩnh vực." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTopic(int id, [FromBody] TopicViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var result = await _adminService.UpdateTopicAsync(id, model);
            if (result)
            {
                return Json(new { success = true, message = "Đã cập nhật lĩnh vực thành công!" });
            }
            return Json(new { success = false, message = "Không thể cập nhật lĩnh vực." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic([FromBody] DeleteTopicRequest request)
        {
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
