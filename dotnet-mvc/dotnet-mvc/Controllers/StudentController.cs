using dotnet_mvc.Extensions;
using dotnet_mvc.Security;
using dotnet_mvc.Services.Academic;
using dotnet_mvc.Services.Dashboard;
using dotnet_mvc.Services.Finance;
using dotnet_mvc.Services.Scenarios;
using dotnet_mvc.ViewModels.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Student)]
public class StudentController : AuthenticatedControllerBase
{
    private readonly IAcademicManagementService _academicManagementService;
    private readonly IStudentDashboardService _studentDashboardService;
    private readonly IScenarioService _scenarioService;
    private readonly IFinanceApiClient _financeApiClient;

    public StudentController(
        IAcademicManagementService academicManagementService,
        IStudentDashboardService studentDashboardService,
        IScenarioService scenarioService,
        IFinanceApiClient financeApiClient)
    {
        _academicManagementService = academicManagementService;
        _studentDashboardService = studentDashboardService;
        _scenarioService = scenarioService;
        _financeApiClient = financeApiClient;
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Dashboard", "dashboard", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var model = await _studentDashboardService.GetDashboardAsync(studentId.Value, cancellationToken);
        return View(model);
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

    public async Task<IActionResult> Scenarios(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Cenários familiares", "scenarios", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        var model = new StudentScenariosIndexViewModel
        {
            Scenarios = await _scenarioService.GetUserScenariosAsync(userId.Value, cancellationToken)
        };

        return View(model);
    }

    public IActionResult CreateScenario()
    {
        ApplyDashboardContext("Criar cenário", "scenarios", UserRoles.Student);
        return View(new ScenarioEditorViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateScenario(ScenarioEditorViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Criar cenário", "scenarios", UserRoles.Student);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        await _scenarioService.CreateScenarioAsync(userId.Value, model, cancellationToken);
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Cenário \"{model.Name.Trim()}\" criado com sucesso.";
        return RedirectToAction(nameof(Scenarios));
    }

    public async Task<IActionResult> EditScenario(int id, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Editar cenário", "scenarios", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        var model = await _scenarioService.GetScenarioForEditAsync(userId.Value, id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View("CreateScenario", model);
    }

    [HttpPost]
    public async Task<IActionResult> EditScenario(int id, ScenarioEditorViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Editar cenário", "scenarios", UserRoles.Student);
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View("CreateScenario", model);
        }

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        var updated = await _scenarioService.UpdateScenarioAsync(userId.Value, id, model, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Cenário \"{model.Name.Trim()}\" atualizado com sucesso.";
        return RedirectToAction(nameof(Scenarios));
    }

    public IActionResult Entries()
    {
        ApplyDashboardContext("Lançamentos", "entries", UserRoles.Student);
        return View();
    }

    public async Task<IActionResult> Simulations(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Simulações", "simulations", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var model = new SimulationViewModel
        {
            Scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Simulations(SimulationViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Simulações", "simulations", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue)
        {
            return Forbid();
        }

        // Recarrega o dropdown de cenários para reapresentar o formulário.
        model.Scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Delega o cálculo na API Django (que invoca o motor Python). O portal não calcula.
        var result = await _financeApiClient.SimulateAsync(new SimulationRequest
        {
            Strategy = model.Strategy,
            ScenarioId = model.ScenarioId ?? 0,
            RendimentoMensal = model.RendimentoMensal,
            DespesasMensais = model.DespesasMensais,
            EncargosFixos = model.EncargosFixos,
            CapitalPoupanca = model.CapitalPoupanca,
            Emprestimo = model.Emprestimo,
            Meses = model.Meses
        }, cancellationToken);

        if (result is null)
        {
            model.ErrorMessage = "Não foi possível obter a simulação. Tenta novamente dentro de momentos.";
        }

        model.Result = result;
        return View(model);
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

    public async Task<IActionResult> ChallengeDetails(int id, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Detalhe do desafio", "challenges", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var model = await _academicManagementService.GetStudentChallengeDetailsAsync(studentId.Value, id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChallengeDetails(int id, StudentChallengeDetailsViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Detalhe do desafio", "challenges", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await LoadChallengeDetailsViewModelAsync(studentId.Value, id, model.ResponseText, cancellationToken);
            return invalidModel is null ? NotFound() : View(invalidModel);
        }

        try
        {
            var saved = await _academicManagementService.SaveStudentChallengeSubmissionAsync(studentId.Value, id, model.ResponseText, cancellationToken);
            if (!saved)
            {
                return NotFound();
            }

            TempData["StatusType"] = "success";
            TempData["StatusMessage"] = "A tua resposta foi guardada com sucesso.";
            return RedirectToAction(nameof(ChallengeDetails), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var invalidStateModel = await LoadChallengeDetailsViewModelAsync(studentId.Value, id, model.ResponseText, cancellationToken);
            return invalidStateModel is null ? NotFound() : View(invalidStateModel);
        }
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

    private async Task<StudentChallengeDetailsViewModel?> LoadChallengeDetailsViewModelAsync(
        int studentId,
        int challengeId,
        string responseText,
        CancellationToken cancellationToken)
    {
        var model = await _academicManagementService.GetStudentChallengeDetailsAsync(studentId, challengeId, cancellationToken);
        if (model is null)
        {
            return null;
        }

        model.ResponseText = responseText;
        return model;
    }
}
