using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SciSubmit.Services;
using SciSubmit.Models.Review;
using System.Security.Claims;

namespace SciSubmit.Controllers
{
    [Authorize(Roles = "Admin,Reviewer")]
    public class ReviewController : Controller
    {
        private readonly ILogger<ReviewController> _logger;
        private readonly IReviewService _reviewService;

        public ReviewController(ILogger<ReviewController> logger, IReviewService reviewService)
        {
            _logger = logger;
            _reviewService = reviewService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return 0;
        }

        public async Task<IActionResult> Index()
        {
            var reviewerId = GetCurrentUserId();
            if (reviewerId == 0)
            {
                TempData["ErrorMessage"] = "Không xác định được reviewer.";
                return RedirectToAction("Login", "Account");
            }

            var dashboard = await _reviewService.GetReviewerDashboardAsync(reviewerId);
            return View("Dashboard", dashboard);
        }

        public async Task<IActionResult> Details(int id)
        {
            var reviewerId = GetCurrentUserId();
            if (reviewerId == 0)
            {
                TempData["ErrorMessage"] = "Không xác định được reviewer.";
                return RedirectToAction("Login", "Account");
            }

            var reviewDetails = await _reviewService.GetReviewDetailsAsync(id, reviewerId);
            if (reviewDetails == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài phản biện hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(Index));
            }

            return View(reviewDetails);
        }

        public IActionResult Invitation(int id)
        {
            ViewData["AssignmentId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptInvitation(int id)
        {
            // TODO: Implement accept invitation logic
            TempData["SuccessMessage"] = "Đã chấp nhận lời mời phản biện!";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectInvitation(int id, string reason)
        {
            // TODO: Implement reject invitation logic
            TempData["SuccessMessage"] = "Đã từ chối lời mời phản biện.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int id, [FromForm] SubmitReviewViewModel model)
        {
            var reviewerId = GetCurrentUserId();
            if (reviewerId == 0)
            {
                _logger.LogWarning("SubmitReview: Reviewer ID is 0");
                return Json(new { success = false, message = "Không xác định được reviewer." });
            }

            // Manually bind Scores from form data
            if (model.Scores == null || model.Scores.Count == 0)
            {
                model.Scores = new Dictionary<string, int>();
                foreach (var key in Request.Form.Keys)
                {
                    if (key.StartsWith("Scores["))
                    {
                        var criterionName = key.Substring(7, key.Length - 8); // Remove "Scores[" and "]"
                        if (int.TryParse(Request.Form[key], out int score))
                        {
                            model.Scores[criterionName] = score;
                        }
                    }
                }
            }

            model.AssignmentId = id;
            
            _logger.LogInformation("SubmitReview: AssignmentId={AssignmentId}, ReviewerId={ReviewerId}, ScoresCount={ScoresCount}, HasComments={HasComments}, Recommendation={Recommendation}",
                id, reviewerId, model.Scores?.Count ?? 0, !string.IsNullOrEmpty(model.CommentsForAuthor), model.Recommendation);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("SubmitReview: ModelState invalid. Errors: {Errors}", string.Join(", ", errors));
                return Json(new { success = false, message = string.Join("<br>", errors) });
            }

            var result = await _reviewService.SubmitReviewAsync(id, reviewerId, model);
            
            if (result)
            {
                _logger.LogInformation("SubmitReview: Success for AssignmentId={AssignmentId}", id);
                TempData["SuccessMessage"] = "Đã nộp đánh giá thành công!";
                return Json(new { success = true, message = "Đã nộp đánh giá thành công!", redirectUrl = Url.Action(nameof(Index)) });
            }
            else
            {
                _logger.LogWarning("SubmitReview: Failed for AssignmentId={AssignmentId}, ReviewerId={ReviewerId}", id, reviewerId);
                return Json(new { success = false, message = "Không thể nộp đánh giá. Vui lòng kiểm tra lại thông tin." });
            }
        }

        public IActionResult History()
        {
            return View();
        }

        public IActionResult Download(int id)
        {
            // TODO: Implement download logic
            return NotFound();
        }
    }

}
