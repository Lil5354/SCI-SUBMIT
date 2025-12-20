using Microsoft.AspNetCore.Mvc;

namespace SciSubmit.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index(int id)
        {
            ViewData["SubmissionId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(int submissionId, string gateway)
        {
            // TODO: Implement payment processing
            // Redirect to payment gateway (Momo, VNPAY, etc.)
            return RedirectToAction(nameof(Callback), new { gateway, submissionId });
        }

        public IActionResult Callback(string gateway, int submissionId)
        {
            // TODO: Handle payment callback
            TempData["SuccessMessage"] = "Thanh toán thành công!";
            return RedirectToAction(nameof(Invoice), new { id = submissionId });
        }

        public IActionResult Invoice(int id)
        {
            ViewData["PaymentId"] = id;
            return View();
        }
    }
}
