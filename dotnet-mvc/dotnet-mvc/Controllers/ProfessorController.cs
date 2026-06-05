using dotnet_mvc.Extensions;
using dotnet_mvc.Security;
using dotnet_mvc.Services.Academic;
using dotnet_mvc.ViewModels.Professor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Professor + "," + UserRoles.Admin)]
public class ProfessorController : AuthenticatedControllerBase
{
    private readonly IAcademicManagementService _academicManagementService;
    private readonly IAccessCodeService _accessCodeService;

    public ProfessorController(
        IAcademicManagementService academicManagementService,
        IAccessCodeService accessCodeService)
    {
        _academicManagementService = academicManagementService;
        _accessCodeService = accessCodeService;
    }

    public IActionResult Dashboard()
    {
        ApplyDashboardContext("Dashboard", "dashboard", GetManagementRole());
        return View();
    }

    public async Task<IActionResult> Challenges(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Desafios", "challenges", GetManagementRole());

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var challenges = await _academicManagementService.GetProfessorChallengesAsync(professorId.Value, cancellationToken);
        var model = new ProfessorChallengesIndexViewModel
        {
            Challenges = challenges,
            DraftCount = challenges.Count(x => x.StatusKey == "draft"),
            PublishedCount = challenges.Count(x => x.StatusKey != "draft")
        };

        return View(model);
    }

    public async Task<IActionResult> Classes(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Turmas", "classes", GetManagementRole());

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var model = new ClassesIndexViewModel
        {
            Classes = await _academicManagementService.GetProfessorClassroomsAsync(professorId.Value, cancellationToken)
        };

        model.TotalStudents = model.Classes
            .SelectMany(x => x.Students)
            .Select(x => x.StudentId)
            .Distinct()
            .Count();

        model.TotalChallenges = model.Classes.Sum(x => x.TotalChallengesCount);
        model.TotalSubmittedChallenges = model.Classes.Sum(x => x.SubmittedChallengesCount);
        model.TotalReviewedChallenges = model.Classes.Sum(x => x.ReviewedChallengesCount);

        var totalExpectedSubmissions = model.Classes.Sum(x => x.ExpectedSubmissionsCount);
        model.OverallCompletionRateLabel = totalExpectedSubmissions == 0
            ? "0%"
            : $"{Math.Round((decimal)model.TotalSubmittedChallenges * 100m / totalExpectedSubmissions, 1, MidpointRounding.AwayFromZero):0.#}%";

        return View(model);
    }

    public IActionResult CreateClass()
    {
        ApplyDashboardContext("Criar turma", "create-class", GetManagementRole());

        return View(new CreateClassViewModel
        {
            GeneratedCodePreview = _accessCodeService.GeneratePreview()
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateClass(CreateClassViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Criar turma", "create-class", GetManagementRole());

        if (!ModelState.IsValid)
        {
            model.GeneratedCodePreview = string.IsNullOrWhiteSpace(model.GeneratedCodePreview)
                ? _accessCodeService.GeneratePreview()
                : model.GeneratedCodePreview;
            return View(model);
        }

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var result = await _academicManagementService.CreateClassroomAsync(professorId.Value, model, cancellationToken);

        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Turma \"{result.Name}\" criada com sucesso. Código de adesão: {result.AccessCode}.";
        return RedirectToAction(nameof(Classes));
    }

    public IActionResult Submissions()
    {
        ApplyDashboardContext("Submissões", "submissions", GetManagementRole());
        return View();
    }

    public async Task<IActionResult> Progress(int? classId, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Alunos", "progress", GetManagementRole());

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var availableClasses = await _academicManagementService.GetProfessorClassroomOptionsAsync(
            professorId.Value,
            onlyActive: false,
            cancellationToken);

        var selectedClassId = availableClasses.Any(x => x.Id == classId) ? classId : null;
        var model = new ProfessorStudentsIndexViewModel
        {
            SelectedClassId = selectedClassId,
            AvailableClasses = availableClasses,
            Students = await _academicManagementService.GetProfessorStudentsAsync(
                professorId.Value,
                selectedClassId,
                cancellationToken)
        };

        model.TotalStudents = model.Students
            .Select(x => x.StudentId)
            .Distinct()
            .Count();
        model.TotalAvailableChallenges = model.Students.Sum(x => x.AvailableChallengesCount);
        model.TotalSubmittedChallenges = model.Students.Sum(x => x.SubmittedChallengesCount);
        model.TotalReviewedChallenges = model.Students.Sum(x => x.ReviewedChallengesCount);
        model.OverallCompletionRateLabel = model.TotalAvailableChallenges == 0
            ? "0%"
            : $"{Math.Round((decimal)model.TotalSubmittedChallenges * 100m / model.TotalAvailableChallenges, 1, MidpointRounding.AwayFromZero):0.#}%";

        return View(model);
    }

    public IActionResult CreateChallenge(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Criar desafio", "create-challenge", GetManagementRole());

        return View(new CreateChallengeViewModel
        {
            GeneratedCodePreview = _accessCodeService.GeneratePreview()
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateChallenge(CreateChallengeViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Criar desafio", "create-challenge", GetManagementRole());

        if (!ModelState.IsValid)
        {
            model.GeneratedCodePreview = string.IsNullOrWhiteSpace(model.GeneratedCodePreview)
                ? _accessCodeService.GeneratePreview()
                : model.GeneratedCodePreview;
            return View(model);
        }

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        try
        {
            var result = await _academicManagementService.CreateChallengeAsync(professorId.Value, model, cancellationToken);
            TempData["StatusType"] = "success";
            TempData["StatusMessage"] = $"Desafio \"{result.Title}\" criado com sucesso. Código reservado: {result.AccessCode}. Já o podes atribuir a uma turma.";
            return RedirectToAction(nameof(Challenges));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.GeneratedCodePreview = string.IsNullOrWhiteSpace(model.GeneratedCodePreview)
                ? _accessCodeService.GeneratePreview()
                : model.GeneratedCodePreview;
            return View(model);
        }
    }

    public async Task<IActionResult> EditChallenge(int id, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Editar desafio", "challenges", GetManagementRole());

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var model = await _academicManagementService.GetChallengeForEditAsync(professorId.Value, id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View("CreateChallenge", model);
    }

    [HttpPost]
    public async Task<IActionResult> EditChallenge(int id, CreateChallengeViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Editar desafio", "challenges", GetManagementRole());
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View("CreateChallenge", model);
        }

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var updated = await _academicManagementService.UpdateChallengeAsync(professorId.Value, id, model, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Desafio \"{model.Title.Trim()}\" atualizado com sucesso.";
        return RedirectToAction(nameof(Challenges));
    }

    public async Task<IActionResult> AssignChallenge(int? challengeId, int? classId, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Atribuir desafio", "challenges", GetManagementRole());

        var model = new AssignChallengeViewModel
        {
            ChallengeId = challengeId,
            ClassId = classId
        };

        await PopulateAssignChallengeFormAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AssignChallenge(AssignChallengeViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Atribuir desafio", "challenges", GetManagementRole());

        if (!ModelState.IsValid)
        {
            await PopulateAssignChallengeFormAsync(model, cancellationToken);
            return View(model);
        }

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        try
        {
            var result = await _academicManagementService.AssignChallengeAsync(professorId.Value, model, cancellationToken);
            TempData["StatusType"] = "success";
            TempData["StatusMessage"] = $"Desafio \"{result.Title}\" atribuído à turma \"{result.ClassroomName}\" com sucesso. Código de adesão: {result.AccessCode}.";
            return RedirectToAction(nameof(Classes));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateAssignChallengeFormAsync(model, cancellationToken);
            return View(model);
        }
    }

    private async Task PopulateAssignChallengeFormAsync(AssignChallengeViewModel model, CancellationToken cancellationToken)
    {
        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            model.AvailableChallenges = [];
            model.AvailableClasses = [];
            return;
        }

        var challenges = await _academicManagementService.GetProfessorChallengeOptionsAsync(
            professorId.Value,
            onlyDraft: true,
            cancellationToken: cancellationToken);

        var classes = await _academicManagementService.GetProfessorClassroomOptionsAsync(
            professorId.Value,
            cancellationToken: cancellationToken);

        model.AvailableChallenges = challenges
            .Select(x => new SelectListItem($"{x.Title} ({x.AccessCode})", x.Id.ToString()))
            .ToList();

        model.AvailableClasses = classes
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var selectedChallenge = challenges.FirstOrDefault(x => x.Id == model.ChallengeId);
        model.ChallengeTitle = selectedChallenge?.Title;
        model.ChallengeCode = selectedChallenge?.AccessCode;

        if (model.AvailableChallenges.Count == 0)
        {
            ModelState.AddModelError(nameof(model.ChallengeId), "Cria primeiro um desafio antes de o atribuíres.");
        }

        if (model.AvailableClasses.Count == 0)
        {
            ModelState.AddModelError(nameof(model.ClassId), "Cria primeiro uma turma antes de atribuíres desafios.");
        }
    }

    private string GetManagementRole()
    {
        return User.IsInRole(UserRoles.Admin) ? UserRoles.Admin : UserRoles.Professor;
    }
}
