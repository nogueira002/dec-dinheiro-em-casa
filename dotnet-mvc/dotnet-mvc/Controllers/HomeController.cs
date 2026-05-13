using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dotnet_mvc.Models;
using dotnet_mvc.Security;

namespace dotnet_mvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var route = UserRoles.GetDefaultRoute(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
            return RedirectToAction(route.Action, route.Controller);
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
