using Microsoft.AspNetCore.Mvc;

namespace SciSubmit.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(ILogger<ReviewController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            ViewData["ReviewId"] = id;
            return View();
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
        public IActionResult SubmitReview(int id, ReviewFormViewModel model)
        {
            // TODO: Implement review submission logic
            TempData["SuccessMessage"] = "Đã nộp đánh giá thành công!";
            return RedirectToAction(nameof(Index));
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

    public class ReviewFormViewModel
    {
        public int SubmissionId { get; set; }
        public Dictionary<string, int> Scores { get; set; } = new Dictionary<string, int>();
        public string CommentsForAuthor { get; set; } = string.Empty;
        public string CommentsForAdmin { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty; // Accept, Minor, Major, Reject
    }
}
