using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Enums;
using SciSubmit.Services;

namespace SciSubmit.Controllers
{
    public class FilePreviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FilePreviewController> _logger;
        private readonly IUserService _userService;

        public FilePreviewController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<FilePreviewController> logger,
            IUserService userService)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _userService = userService;
        }

        /// <summary>
        /// Preview file từ submission
        /// </summary>
        public async Task<IActionResult> Submission(int submissionId, string? type = "fullpaper")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var submission = await _context.Submissions
                    .Include(s => s.Author)
                    .Include(s => s.FullPaperVersions)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                {
                    return NotFound();
                }

                // Kiểm tra quyền
                var currentUser = _userService.GetCurrentUser(HttpContext);
                var isAdmin = currentUser != null && currentUser.Role == UserRole.Admin;
                
                if (submission.AuthorId != userId.Value && !isAdmin)
                {
                    return Forbid();
                }

                string? fileUrl = null;
                string? fileName = null;

                if (type == "fullpaper")
                {
                    var fullPaper = submission.FullPaperVersions
                        .Where(v => v.IsCurrentVersion)
                        .FirstOrDefault();

                    if (fullPaper != null)
                    {
                        fileUrl = fullPaper.FileUrl;
                        fileName = fullPaper.FileName;
                    }
                }
                else if (type == "abstract")
                {
                    if (!string.IsNullOrEmpty(submission.AbstractFileUrl))
                    {
                        fileUrl = submission.AbstractFileUrl;
                        fileName = Path.GetFileName(submission.AbstractFileUrl);
                    }
                }

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return NotFound();
                }

                var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                ViewBag.FileName = fileName;
                ViewBag.FileUrl = fileUrl;
                ViewBag.FileType = extension;

                // Return view với file URL để preview
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing file for submission {SubmissionId}", submissionId);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Preview file từ review assignment
        /// </summary>
        public async Task<IActionResult> Review(int reviewAssignmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var assignment = await _context.ReviewAssignments
                    .Include(ra => ra.Submission)
                        .ThenInclude(s => s.FullPaperVersions)
                    .FirstOrDefaultAsync(ra => ra.Id == reviewAssignmentId && ra.ReviewerId == userId.Value);

                if (assignment == null)
                {
                    return NotFound();
                }

                var fullPaper = assignment.Submission.FullPaperVersions
                    .Where(v => v.IsCurrentVersion)
                    .FirstOrDefault();

                if (fullPaper == null && !string.IsNullOrEmpty(assignment.Submission.AbstractFileUrl))
                {
                    // Fallback to abstract
                    ViewBag.FileName = Path.GetFileName(assignment.Submission.AbstractFileUrl);
                    ViewBag.FileUrl = assignment.Submission.AbstractFileUrl;
                    ViewBag.FileType = Path.GetExtension(assignment.Submission.AbstractFileUrl).ToLowerInvariant();
                }
                else if (fullPaper != null)
                {
                    ViewBag.FileName = fullPaper.FileName;
                    ViewBag.FileUrl = fullPaper.FileUrl;
                    ViewBag.FileType = Path.GetExtension(fullPaper.FileName).ToLowerInvariant();
                }
                else
                {
                    return NotFound();
                }

                return View("Submission");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing file for review assignment {ReviewAssignmentId}", reviewAssignmentId);
                return StatusCode(500);
            }
        }
    }
}













