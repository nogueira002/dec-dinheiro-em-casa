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

        return View(model);
    }

    public async Task<IActionResult> CreateChallenge(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Criar desafio", "create-challenge", GetManagementRole());

        var model = await BuildCreateChallengeViewModelAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateChallenge(CreateChallengeViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Criar desafio", "create-challenge", GetManagementRole());

        if (!ModelState.IsValid)
        {
            await PopulateChallengeFormAsync(model, cancellationToken);
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
            TempData["StatusMessage"] = $"Desafio \"{result.Title}\" atribuído com sucesso. Código de adesão: {result.AccessCode}.";
            return RedirectToAction(nameof(Classes));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ClassId), ex.Message);
            await PopulateChallengeFormAsync(model, cancellationToken);
            return View(model);
        }
    }

    private async Task<CreateChallengeViewModel> BuildCreateChallengeViewModelAsync(CancellationToken cancellationToken)
    {
        var model = new CreateChallengeViewModel
        {
            GeneratedCodePreview = _accessCodeService.GeneratePreview()
        };

        await PopulateChallengeFormAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateChallengeFormAsync(CreateChallengeViewModel model, CancellationToken cancellationToken)
    {
        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            model.AvailableClasses = [];
            return;
        }

        var classes = await _academicManagementService.GetProfessorClassroomOptionsAsync(
            professorId.Value,
            cancellationToken: cancellationToken);
        model.AvailableClasses = classes
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        model.GeneratedCodePreview = string.IsNullOrWhiteSpace(model.GeneratedCodePreview)
            ? _accessCodeService.GeneratePreview()
            : model.GeneratedCodePreview;

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
