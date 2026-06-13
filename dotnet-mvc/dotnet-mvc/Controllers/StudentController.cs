using dotnet_mvc.Extensions;
using dotnet_mvc.Security;
using dotnet_mvc.Services.Academic;
using dotnet_mvc.Services.Dashboard;
using dotnet_mvc.Services.Finance;
using dotnet_mvc.Services.Scenarios;
using dotnet_mvc.Services.Student;
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
    private readonly IProfileService _profileService;

    public StudentController(
        IAcademicManagementService academicManagementService,
        IStudentDashboardService studentDashboardService,
        IScenarioService scenarioService,
        IFinanceApiClient financeApiClient,
        IFinancialEntryService entryService,
        IProfileService profileService,
        ISavingsGoalService goalService,
        ISimulationHistoryService simulationHistoryService)
    {
        _academicManagementService = academicManagementService;
        _studentDashboardService = studentDashboardService;
        _scenarioService = scenarioService;
        _financeApiClient = financeApiClient;
        _entryService = entryService;
        _profileService = profileService;
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
    public async Task<IActionResult> UpdateEntry(int scenarioId, int entryId, CreateEntryViewModel Form, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue) return Forbid();

        if (ModelState.IsValid)
        {
            await _entryService.UpdateEntryAsync(userId.Value, entryId, Form, cancellationToken);
            TempData["StatusType"]    = "success";
            TempData["StatusMessage"] = "Lançamento atualizado.";
        }

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

    // ===== Calculadora =====

    public IActionResult Calculator()
    {
        ApplyDashboardContext("Calculadora", "calculator", UserRoles.Student);
        return View(new CalculatorViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CalculatorEsforco(EsforcoCalcVM Esforco, CancellationToken ct)
    {
        ApplyDashboardContext("Calculadora", "calculator", UserRoles.Student);
        var vm = new CalculatorViewModel { ActiveTab = "esforco", Esforco = Esforco };
        if (ModelState.IsValid)
        {
            var (result, error) = await _financeApiClient.CalcTaxaEsforcoAsync(
                new CalcEsforcoRequest { EncargosFixos = Esforco.EncargosFixos, RendimentoLiquido = Esforco.RendimentoLiquido }, ct);
            Esforco.Result = result;
            Esforco.Error  = error;
        }
        return View("Calculator", vm);
    }

    [HttpPost]
    public async Task<IActionResult> CalculatorCapitalizacao(CapitalizacaoCalcVM Capitalizacao, CancellationToken ct)
    {
        ApplyDashboardContext("Calculadora", "calculator", UserRoles.Student);
        var vm = new CalculatorViewModel { ActiveTab = "capitalizacao", Capitalizacao = Capitalizacao };
        if (ModelState.IsValid)
        {
            var (result, error) = await _financeApiClient.CalcCapitalizacaoAsync(
                new CalcCapitalizacaoRequest { Capital = Capitalizacao.Capital, Tan = Capitalizacao.TanPercent / 100m, Meses = Capitalizacao.Meses }, ct);
            Capitalizacao.Result = result;
            Capitalizacao.Error  = error;
        }
        return View("Calculator", vm);
    }

    [HttpPost]
    public async Task<IActionResult> CalculatorCredito(CreditoCalcVM Credito, CancellationToken ct)
    {
        ApplyDashboardContext("Calculadora", "calculator", UserRoles.Student);
        var vm = new CalculatorViewModel { ActiveTab = "credito", Credito = Credito };
        if (ModelState.IsValid)
        {
            var (result, error) = await _financeApiClient.CalcCreditoAsync(
                new CalcCreditoRequest { Emprestimo = Credito.Emprestimo, Tan = Credito.TanPercent / 100m, Meses = Credito.Meses }, ct);
            Credito.Result = result;
            Credito.Error  = error;
        }
        return View("Calculator", vm);
    }

    [HttpPost]
    public async Task<IActionResult> CalculatorProjecao(ProjecaoCalcVM Projecao, CancellationToken ct)
    {
        ApplyDashboardContext("Calculadora", "calculator", UserRoles.Student);
        var vm = new CalculatorViewModel { ActiveTab = "projecao", Projecao = Projecao };
        if (ModelState.IsValid)
        {
            var (result, error) = await _financeApiClient.CalcProjecaoAsync(
                new CalcProjecaoRequest
                {
                    RendimentoMensal = Projecao.RendimentoMensal,
                    DespesasMensais  = Projecao.DespesasMensais,
                    TaxaInflacao     = Projecao.TaxaInflacaoPercent / 100m,
                    Meses            = Projecao.Meses
                }, ct);
            Projecao.Result = result;
            Projecao.Error  = error;
        }
        return View("Calculator", vm);
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

    // ===== Workspace de desafio =====

    public async Task<IActionResult> ChallengeWorkspace(int id, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Workspace", "challenges", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue) return Forbid();

        var challenge = await _academicManagementService.GetStudentChallengeDetailsAsync(studentId.Value, id, cancellationToken);
        if (challenge is null) return NotFound();

        var model = new ChallengeWorkspaceViewModel
        {
            Challenge = challenge,
            Simulation = new SimulationViewModel
            {
                Scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken),
                History   = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken)
            }
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ScenarioQuickData(int id, CancellationToken cancellationToken)
    {
        var studentId = User.GetUserId();
        if (!studentId.HasValue) return Forbid();

        var data = await _scenarioService.GetScenarioQuickDataAsync(studentId.Value, id, cancellationToken);
        if (data is null) return NotFound();

        return Json(new
        {
            rendimentoMensal = data.RendimentoMensal,
            despesasMensais  = data.DespesasMensais,
            encargosFixos    = data.EncargosFixos,
            capitalPoupanca  = data.CapitalPoupanca
        });
    }

    [HttpPost]
    public async Task<IActionResult> ChallengeWorkspaceSimulate(int id, SimulationViewModel sim, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Workspace", "challenges", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue) return Forbid();

        var challenge = await _academicManagementService.GetStudentChallengeDetailsAsync(studentId.Value, id, cancellationToken);
        if (challenge is null) return NotFound();

        sim.Scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken);
        sim.History   = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken);

        var model = new ChallengeWorkspaceViewModel
        {
            Challenge  = challenge,
            Simulation = sim,
            ActiveTab  = "simulate"
        };

        if (!ModelState.IsValid)
            return View("ChallengeWorkspace", model);

        var outcome = await _financeApiClient.SimulateAsync(new SimulationRequest
        {
            Strategy         = sim.Strategy,
            ScenarioId       = sim.ScenarioId ?? 0,
            RendimentoMensal = sim.RendimentoMensal,
            DespesasMensais  = sim.DespesasMensais,
            EncargosFixos    = sim.EncargosFixos,
            CapitalPoupanca  = sim.CapitalPoupanca,
            Emprestimo       = sim.Emprestimo,
            Meses            = sim.Meses
        }, cancellationToken);

        if (outcome.Success && outcome.Result is not null)
        {
            model.Simulation.Result = outcome.Result;
            await _simulationHistoryService.SaveAsync(studentId.Value, new SimulationRequest
            {
                Strategy         = sim.Strategy,
                ScenarioId       = sim.ScenarioId ?? 0,
                RendimentoMensal = sim.RendimentoMensal,
                DespesasMensais  = sim.DespesasMensais,
                EncargosFixos    = sim.EncargosFixos,
                CapitalPoupanca  = sim.CapitalPoupanca,
                Emprestimo       = sim.Emprestimo,
                Meses            = sim.Meses
            }, outcome.Result, cancellationToken);
            model.Simulation.History = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken);
        }
        else
        {
            model.Simulation.ErrorMessage = outcome.ErrorMessage;
        }

        return View("ChallengeWorkspace", model);
    }

    [HttpPost]
    public async Task<IActionResult> ChallengeWorkspaceCompare(int id, StrategyComparisonViewModel cmp, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Workspace", "challenges", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue) return Forbid();

        var challenge = await _academicManagementService.GetStudentChallengeDetailsAsync(studentId.Value, id, cancellationToken);
        if (challenge is null) return NotFound();

        var scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken);
        cmp.Scenarios = scenarios;

        SimulationRequest BuildRequest(string strategy) => new()
        {
            Strategy         = strategy,
            ScenarioId       = cmp.ScenarioId ?? 0,
            RendimentoMensal = cmp.RendimentoMensal,
            DespesasMensais  = cmp.DespesasMensais,
            EncargosFixos    = cmp.EncargosFixos,
            CapitalPoupanca  = cmp.CapitalPoupanca,
            Emprestimo       = cmp.Emprestimo,
            Meses            = cmp.Meses
        };

        var results = await Task.WhenAll(
            _financeApiClient.SimulateAsync(BuildRequest("conservative"), cancellationToken),
            _financeApiClient.SimulateAsync(BuildRequest("moderate"),     cancellationToken),
            _financeApiClient.SimulateAsync(BuildRequest("aggressive"),   cancellationToken)
        );

        if (results.All(r => !r.Success))
        {
            cmp.ErrorMessage = results[0].ErrorMessage ?? "Erro ao calcular a comparação.";
        }
        else
        {
            cmp.Conservative = results[0].Result;
            cmp.Moderate     = results[1].Result;
            cmp.Aggressive   = results[2].Result;
        }

        var model = new ChallengeWorkspaceViewModel
        {
            Challenge  = challenge,
            Simulation = new SimulationViewModel
            {
                Scenarios = scenarios,
                History   = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken)
            },
            Comparison = cmp,
            ActiveTab  = "compare"
        };
        return View("ChallengeWorkspace", model);
    }

    [HttpPost]
    public async Task<IActionResult> ChallengeWorkspaceSubmit(int id, StudentChallengeDetailsViewModel challengeForm, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Workspace", "challenges", UserRoles.Student);

        var studentId = User.GetUserId();
        if (!studentId.HasValue) return Forbid();

        async Task<IActionResult> ReturnWorkspace(string? responseText = null)
        {
            var ch = await _academicManagementService.GetStudentChallengeDetailsAsync(studentId.Value, id, cancellationToken);
            if (ch is null) return NotFound();
            if (responseText is not null) ch.ResponseText = responseText;
            return View("ChallengeWorkspace", new ChallengeWorkspaceViewModel
            {
                Challenge  = ch,
                Simulation = new SimulationViewModel
                {
                    Scenarios = await _scenarioService.GetUserScenariosAsync(studentId.Value, cancellationToken),
                    History   = await _simulationHistoryService.GetUserHistoryAsync(studentId.Value, 8, cancellationToken)
                },
                ActiveTab = "respond"
            });
        }

        if (!ModelState.IsValid)
            return await ReturnWorkspace(challengeForm.ResponseText);

        try
        {
            var saved = await _academicManagementService.SaveStudentChallengeSubmissionAsync(studentId.Value, id, challengeForm.ResponseText, cancellationToken);
            if (!saved) return NotFound();

            TempData["StatusType"]    = "success";
            TempData["StatusMessage"] = "A tua resposta foi guardada com sucesso.";
            return RedirectToAction(nameof(ChallengeWorkspace), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReturnWorkspace(challengeForm.ResponseText);
        }
    }

    // ===== Perfil =====

    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Perfil", "profile", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue) return Forbid();

        var model = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
        if (model is null) return NotFound();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Perfil", "profile", UserRoles.Student);

        var userId = User.GetUserId();
        if (!userId.HasValue) return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var (sucesso, mensagem) = await _profileService.UpdateProfileAsync(userId.Value, model, cancellationToken);
        if (!sucesso)
        {
            ModelState.AddModelError(nameof(model.Email), mensagem ?? "Não foi possível guardar o perfil.");
            return View(model);
        }

        TempData["StatusType"]    = "success";
        TempData["StatusMessage"] = "Perfil atualizado com sucesso.";
        return RedirectToAction(nameof(Profile));
    }

    // ===== Helpers =====

    private async Task<EntriesIndexViewModel> BuildEntriesViewModelAsync(
        int userId, int? scenarioId, string? typeFilter, CancellationToken ct)
    {
        var cenarios      = await _scenarioService.GetUserScenariosAsync(userId, ct);
        var todosLancamentos = scenarioId.HasValue
            ? await _entryService.GetScenarioEntriesAsync(userId, scenarioId.Value, null, ct)
            : [];

        var lancamentosFiltrados = typeFilter switch
        {
            "Income"  => todosLancamentos.Where(e => e.IsIncome).ToList(),
            "Expense" => todosLancamentos.Where(e => !e.IsIncome).ToList(),
            _         => (IReadOnlyList<EntryListItemViewModel>)todosLancamentos
        };

        return new EntriesIndexViewModel
        {
            SelectedScenarioId = scenarioId,
            AvailableScenarios = cenarios,
            Entries            = lancamentosFiltrados,
            TypeFilter         = typeFilter,
            TotalIncome        = todosLancamentos.Where(e => e.IsIncome).Sum(e => e.Amount),
            TotalExpense       = todosLancamentos.Where(e => !e.IsIncome).Sum(e => e.Amount)
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
