using dotnet_mvc.Extensions;
using dotnet_mvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_mvc.Controllers;

[Authorize]
public abstract class AuthenticatedControllerBase : Controller
{
    protected void ApplyDashboardContext(string pageTitle, string activeMenu, string role)
    {
        ViewData["PageTitle"] = pageTitle;
        ViewData["ActiveMenu"] = activeMenu;
        ViewData["Role"] = role;
        ViewData["UserName"] = User.GetDisplayName();
        ViewData["UserRoleLabel"] = User.GetRoleLabel();
    }

    protected RedirectToActionResult RedirectToRoleDashboard()
    {
        var route = UserRoles.GetDefaultRoute(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
        return RedirectToAction(route.Action, route.Controller);
    }
}
