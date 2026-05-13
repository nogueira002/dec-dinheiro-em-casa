using dotnet_mvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class StudentController : AuthenticatedControllerBase
{
    public IActionResult Dashboard()
    {
        ApplyDashboardContext("Dashboard", "dashboard", UserRoles.Student);
        return View();
    }

    public IActionResult Scenarios()
    {
        ApplyDashboardContext("Cenários familiares", "scenarios", UserRoles.Student);
        return View();
    }

    public IActionResult CreateScenario()
    {
        ApplyDashboardContext("Criar cenário", "scenarios", UserRoles.Student);
        return View();
    }

    [HttpPost]
    public IActionResult CreateScenario(string name)
    {
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Cenário \"{name}\" criado com sucesso.";
        return RedirectToAction(nameof(Scenarios));
    }

    public IActionResult Entries()
    {
        ApplyDashboardContext("Lançamentos", "entries", UserRoles.Student);
        return View();
    }

    public IActionResult Simulations()
    {
        ApplyDashboardContext("Simulações", "simulations", UserRoles.Student);
        return View();
    }

    public IActionResult Objectives()
    {
        ApplyDashboardContext("Objetivos", "objectives", UserRoles.Student);
        return View();
    }

    public IActionResult Challenges()
    {
        ApplyDashboardContext("Desafios", "challenges", UserRoles.Student);
        return View();
    }
}
