using dotnet_mvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class AdminController : AuthenticatedControllerBase
{
    public IActionResult Dashboard()
    {
        ApplyDashboardContext("Dashboard de administração", "dashboard", UserRoles.Admin);
        return View();
    }

    public IActionResult Users()
    {
        ApplyDashboardContext("Utilizadores", "users", UserRoles.Admin);
        return View();
    }

    public IActionResult Monitoring()
    {
        ApplyDashboardContext("Monitorização", "monitoring", UserRoles.Admin);
        return View();
    }
}
