using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using System.Security.Claims;

namespace SciSubmit.ViewComponents
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public UserAvatarViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdClaim = ViewContext.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return View(new UserAvatarViewModel
                {
                    AvatarUrl = null,
                    UserName = "Account"
                });
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return View(new UserAvatarViewModel
                {
                    AvatarUrl = null,
                    UserName = "Account"
                });
            }

            return View(new UserAvatarViewModel
            {
                AvatarUrl = user.AvatarUrl,
                UserName = user.FullName
            });
        }
    }

    public class UserAvatarViewModel
    {
        public string? AvatarUrl { get; set; }
        public string UserName { get; set; } = "Account";
    }
}

