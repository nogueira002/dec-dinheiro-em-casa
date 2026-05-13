using dotnet_mvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Professor)]
public class ProfessorController : AuthenticatedControllerBase
{
    public IActionResult Dashboard()
    {
        ApplyDashboardContext("Dashboard", "dashboard", UserRoles.Professor);
        return View();
    }

    public IActionResult Classes()
    {
        ApplyDashboardContext("Turmas", "classes", UserRoles.Professor);
        return View();
    }

    public IActionResult CreateClass()
    {
        ApplyDashboardContext("Criar turma", "create-class", UserRoles.Professor);
        return View();
    }

    [HttpPost]
    public IActionResult CreateClass(string className)
    {
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Turma \"{className}\" criada com sucesso.";
        return RedirectToAction(nameof(Classes));
    }

    public IActionResult Submissions()
    {
        ApplyDashboardContext("Submissões", "submissions", UserRoles.Professor);
        return View();
    }

    public IActionResult Progress()
    {
        ApplyDashboardContext("Progresso", "progress", UserRoles.Professor);
        return View();
    }

    public IActionResult CreateChallenge()
    {
        ApplyDashboardContext("Criar desafio", "create-challenge", UserRoles.Professor);
        return View();
    }

    [HttpPost]
    public IActionResult CreateChallenge(string title)
    {
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Desafio \"{title}\" atribuído com sucesso.";
        return RedirectToAction(nameof(Dashboard));
    }
}
