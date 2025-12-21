using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;

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
    }
}

