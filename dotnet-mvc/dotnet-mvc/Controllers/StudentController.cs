using dotnet_mvc.Extensions;
using dotnet_mvc.Security;
using dotnet_mvc.Services.Academic;
using dotnet_mvc.ViewModels.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class StudentController : AuthenticatedControllerBase
{
    private readonly IAcademicManagementService _academicManagementService;

    public StudentController(IAcademicManagementService academicManagementService)
    {
        _academicManagementService = academicManagementService;
    }

    public IActionResult Dashboard()
    {
        ApplyDashboardContext("Dashboard", "dashboard", UserRoles.Student);
        return View();
    }

    public IActionResult JoinCode()
    {
        ApplyDashboardContext("Aderir por código", "join-code", UserRoles.Student);
        return View(new JoinByCodeViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> JoinCode(JoinByCodeViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Aderir por código", "join-code", UserRoles.Student);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var studentId = User.GetUserId();
        if (!studentId.HasValue)
        {
            return Forbid();
        }

        try
        {
            var result = await _academicManagementService.JoinByAccessCodeAsync(studentId.Value, model.AccessCode, cancellationToken);
            TempData["StatusType"] = result.CreatedAssociation ? "success" : "info";
            TempData["StatusMessage"] = BuildJoinStatusMessage(result);
            return RedirectToAction(nameof(Challenges));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.AccessCode), ex.Message);
            return View(model);
        }
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

    public async Task<IActionResult> Challenges(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Desafios", "challenges", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var model = new StudentChallengesIndexViewModel
        {
            Challenges = await _academicManagementService.GetStudentChallengesAsync(studentId.Value, cancellationToken)
        };

        return View(model);
    }

    private static string BuildJoinStatusMessage(JoinByAccessCodeResult result)
    {
        return result.TargetType switch
        {
            AccessCodeTargetType.Classroom when result.CreatedAssociation
                => $"Entraste na turma \"{result.TargetName}\" com sucesso. Os desafios atribuídos a esta turma já estão disponíveis.",
            AccessCodeTargetType.Classroom
                => $"Já estavas inscrito na turma \"{result.TargetName}\".",
            AccessCodeTargetType.Challenge when result.CreatedAssociation
                => $"Entraste no desafio \"{result.TargetName}\" com sucesso.",
            _ => $"Já tens acesso ao desafio \"{result.TargetName}\"."
        };
    }
}
