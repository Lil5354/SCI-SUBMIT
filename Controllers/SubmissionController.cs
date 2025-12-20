using Microsoft.AspNetCore.Mvc;

namespace SciSubmit.Controllers
{
    public class SubmissionController : Controller
    {
        private readonly ILogger<SubmissionController> _logger;

        public SubmissionController(ILogger<SubmissionController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(object model)
        {
            // TODO: Implement submission logic
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(int id)
        {
            ViewData["SubmissionId"] = id;
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
        public IActionResult Withdraw(int id)
        {
            // TODO: Implement withdraw logic
            TempData["SuccessMessage"] = "Đã rút bài nộp thành công!";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Download(int id)
        {
            // TODO: Implement download logic
            return NotFound();
        }
    }
}

