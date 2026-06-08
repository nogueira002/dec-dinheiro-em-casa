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
    private readonly IFinancialEntryService _entryService;
    private readonly ISavingsGoalService _goalService;
    private readonly ISimulationHistoryService _simulationHistoryService;

    public StudentController(
        IAcademicManagementService academicManagementService,
        IStudentDashboardService studentDashboardService,
        IScenarioService scenarioService,
        IFinanceApiClient financeApiClient,
        IFinancialEntryService entryService,
        ISavingsGoalService goalService,
        ISimulationHistoryService simulationHistoryService)
    {
        _academicManagementService = academicManagementService;
        _studentDashboardService = studentDashboardService;
        _scenarioService = scenarioService;
        _financeApiClient = financeApiClient;
        _entryService = entryService;
        _goalService = goalService;
        _simulationHistoryService = simulationHistoryService;
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

    // ===== Lançamentos =====

    public async Task<IActionResult> Entries(int? scenarioId, string? type, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Lançamentos", "entries", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        var model = await BuildEntriesViewModelAsync(userId.Value, scenarioId, type, cancellationToken);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddEntry(int scenarioId, CreateEntryViewModel Form, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Lançamentos", "entries", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildEntriesViewModelAsync(userId.Value, scenarioId, null, cancellationToken);
            vm.Form = Form;
            return View("Entries", vm);
        }

        await _entryService.AddEntryAsync(userId.Value, scenarioId, Form, cancellationToken);
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = "Lançamento adicionado com sucesso.";
        return RedirectToAction(nameof(Entries), new { scenarioId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEntry(int scenarioId, int entryId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        await _entryService.DeleteEntryAsync(userId.Value, entryId, cancellationToken);
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = "Lançamento eliminado.";
        return RedirectToAction(nameof(Entries), new { scenarioId });
    }

    // ===== Objetivos =====

    public async Task<IActionResult> Objectives(int? scenarioId, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Objetivos", "objectives", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        var model = await BuildObjectivesViewModelAsync(userId.Value, scenarioId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateObjective(int scenarioId, CreateObjectiveViewModel Form, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Objetivos", "objectives", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildObjectivesViewModelAsync(userId.Value, scenarioId, cancellationToken);
            vm.Form = Form;
            return View("Objectives", vm);
        }

        await _goalService.CreateGoalAsync(userId.Value, scenarioId, Form, cancellationToken);
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = "Objetivo criado com sucesso.";
        return RedirectToAction(nameof(Objectives), new { scenarioId });
    }

    [HttpPost]
    public async Task<IActionResult> AddSavings(int scenarioId, int goalId, decimal amount, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        if (amount > 0)
        {
            await _goalService.AddSavingsAsync(userId.Value, goalId, amount, cancellationToken);
            TempData["StatusType"] = "success";
            TempData["StatusMessage"] = "Poupança registada com sucesso.";
        }

        return RedirectToAction(nameof(Objectives), new { scenarioId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteObjective(int scenarioId, int goalId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        await _goalService.DeleteGoalAsync(userId.Value, goalId, cancellationToken);
        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = "Objetivo eliminado.";
        return RedirectToAction(nameof(Objectives), new { scenarioId });
    }

    // ===== Simulações =====

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
            Scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken),
            History = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken)
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

        model.Scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken);
        model.History = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var outcome = await _financeApiClient.SimulateAsync(new SimulationRequest
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

        if (outcome.Success && outcome.Result is not null)
        {
            model.Result = outcome.Result;
            await _simulationHistoryService.SaveAsync(studentId.Value, new SimulationRequest
            {
                Strategy = model.Strategy,
                ScenarioId = model.ScenarioId ?? 0,
                RendimentoMensal = model.RendimentoMensal,
                DespesasMensais = model.DespesasMensais,
                EncargosFixos = model.EncargosFixos,
                CapitalPoupanca = model.CapitalPoupanca,
                Emprestimo = model.Emprestimo,
                Meses = model.Meses
            }, outcome.Result, cancellationToken);

            model.History = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken);
        }
        else
        {
            model.ErrorMessage = outcome.ErrorMessage;
        }

        return View(model);
    }

    // ===== Desafios =====

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

    // ===== Helpers =====

    private async Task<EntriesIndexViewModel> BuildEntriesViewModelAsync(
        int userId, int? scenarioId, string? typeFilter, CancellationToken ct)
    {
        var scenarios = await _scenarioService.GetUserScenariosAsync(userId, ct);
        var entries = scenarioId.HasValue
            ? await _entryService.GetScenarioEntriesAsync(userId, scenarioId.Value, typeFilter, ct)
            : [];

        var allEntries = scenarioId.HasValue
            ? await _entryService.GetScenarioEntriesAsync(userId, scenarioId.Value, null, ct)
            : [];

        return new EntriesIndexViewModel
        {
            SelectedScenarioId = scenarioId,
            AvailableScenarios = scenarios,
            Entries = entries,
            TypeFilter = typeFilter,
            TotalIncome = allEntries.Where(e => e.IsIncome).Sum(e => e.Amount),
            TotalExpense = allEntries.Where(e => !e.IsIncome).Sum(e => e.Amount)
        };
    }

    private async Task<ObjectivesIndexViewModel> BuildObjectivesViewModelAsync(
        int userId, int? scenarioId, CancellationToken ct)
    {
        var scenarios = await _scenarioService.GetUserScenariosAsync(userId, ct);
        var goals = scenarioId.HasValue
            ? await _goalService.GetScenarioGoalsAsync(userId, scenarioId.Value, ct)
            : [];

        return new ObjectivesIndexViewModel
        {
            SelectedScenarioId = scenarioId,
            AvailableScenarios = scenarios,
            Goals = goals
        };
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
        int studentId, int challengeId, string responseText, CancellationToken cancellationToken)
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
