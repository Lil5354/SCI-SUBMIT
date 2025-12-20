using Microsoft.AspNetCore.Mvc;

namespace SciSubmit.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Submissions()
        {
            return View();
        }

        public IActionResult SubmissionDetails(int id)
        {
            ViewData["SubmissionId"] = id;
            return View();
        }

        public IActionResult ReviewSubmission(int id)
        {
            ViewData["SubmissionId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveAbstract(int id)
        {
            // TODO: Implement approve logic
            TempData["SuccessMessage"] = "Đã chấp nhận tóm tắt thành công!";
            return RedirectToAction(nameof(Submissions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectAbstract(int id, string reason)
        {
            // TODO: Implement reject logic
            TempData["SuccessMessage"] = "Đã từ chối tóm tắt và gửi email thông báo cho tác giả.";
            return RedirectToAction(nameof(Submissions));
        }

        public IActionResult Assignments()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignReviewer(int submissionId, int reviewerId, DateTime deadline)
        {
            // TODO: Implement assign reviewer logic
            TempData["SuccessMessage"] = "Đã phân công phản biện thành công!";
            return RedirectToAction(nameof(Assignments));
        }

        public IActionResult FinalDecision(int id)
        {
            ViewData["SubmissionId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MakeFinalDecision(int id, string decision, string comments)
        {
            // TODO: Implement final decision logic
            TempData["SuccessMessage"] = "Đã ra quyết định cuối cùng và gửi email thông báo!";
            return RedirectToAction(nameof(Submissions));
        }

        public IActionResult Conference()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditConference(object model)
        {
            // TODO: Implement conference edit logic
            TempData["SuccessMessage"] = "Đã cập nhật thông tin hội thảo thành công!";
            return RedirectToAction(nameof(Conference));
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Fields()
        {
            return View();
        }

        public IActionResult Keywords()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}
