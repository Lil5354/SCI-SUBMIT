using Hangfire.Dashboard;

namespace SciSubmit
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // TODO: Implement proper authorization
            // For now, allow access in development mode only
            var env = context.GetHttpContext().RequestServices.GetRequiredService<IHostEnvironment>();
            return env.IsDevelopment();
        }
    }
}













