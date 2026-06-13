using dotnet_mvc.Extensions;
using dotnet_mvc.Security;
using dotnet_mvc.Services.Academic;
using dotnet_mvc.Services.Student;
using dotnet_mvc.ViewModels.Professor;
using dotnet_mvc.ViewModels.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Professor + "," + UserRoles.Admin)]
public class ProfessorController : AuthenticatedControllerBase
{
    private readonly IAcademicManagementService _academicManagementService;
    private readonly IAccessCodeService _accessCodeService;
    private readonly IProfileService _profileService;

    public ProfessorController(
        IAcademicManagementService academicManagementService,
        IAccessCodeService accessCodeService,
        IProfileService profileService)
    {
        _academicManagementService = academicManagementService;
        _accessCodeService = accessCodeService;
        _profileService = profileService;
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Dashboard", "dashboard", GetManagementRole());

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var model = await _academicManagementService.GetProfessorDashboardAsync(professorId.Value, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Challenges(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Desafios", "challenges", GetManagementRole());

        IReadOnlyList<ProfessorChallengeListItemViewModel> challenges;
        if (User.IsInRole(UserRoles.Admin))
        {
            challenges = await _academicManagementService.GetAllChallengesAsync(cancellationToken);
        }
        else
        {
            var professorId = User.GetUserId();
            if (!professorId.HasValue) return Forbid();
            challenges = await _academicManagementService.GetProfessorChallengesAsync(professorId.Value, cancellationToken);
        }

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

        IReadOnlyList<ClassroomListItemViewModel> classes;
        if (User.IsInRole(UserRoles.Admin))
        {
            classes = await _academicManagementService.GetAllClassroomsAsync(cancellationToken);
        }
        else
        {
            var professorId = User.GetUserId();
            if (!professorId.HasValue) return Forbid();
            classes = await _academicManagementService.GetProfessorClassroomsAsync(professorId.Value, cancellationToken);
        }

        var model = new ClassesIndexViewModel { Classes = classes };

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

    public async Task<IActionResult> Submissions(int? challengeId, int? classroomId, string? status, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Submissões", "submissions", GetManagementRole());

        IReadOnlyList<SubmissionListItemViewModel> allSubmissions;
        IReadOnlyList<ClassroomOptionViewModel> classes;
        IReadOnlyList<ChallengeOptionViewModel> challenges;

        if (User.IsInRole(UserRoles.Admin))
        {
            allSubmissions = await _academicManagementService.GetAllSubmissionsAsync(challengeId, classroomId, cancellationToken);
            classes        = await _academicManagementService.GetAllClassroomOptionsAsync(onlyActive: false, cancellationToken);
            challenges     = await _academicManagementService.GetAllChallengeOptionsAsync(onlyDraft: false, cancellationToken);
        }
        else
        {
            var professorId = User.GetUserId();
            if (!professorId.HasValue) return Forbid();
            allSubmissions = await _academicManagementService.GetProfessorSubmissionsAsync(professorId.Value, challengeId, classroomId, cancellationToken);
            classes        = await _academicManagementService.GetProfessorClassroomOptionsAsync(professorId.Value, onlyActive: false, cancellationToken);
            challenges     = await _academicManagementService.GetProfessorChallengeOptionsAsync(professorId.Value, onlyDraft: false, cancellationToken);
        }

        var filtered = status switch
        {
            "pending" => allSubmissions.Where(x => !x.IsGraded).ToList(),
            "graded"  => allSubmissions.Where(x => x.IsGraded).ToList(),
            _         => (IReadOnlyList<SubmissionListItemViewModel>)allSubmissions
        };

        var model = new SubmissionsIndexViewModel
        {
            Submissions = filtered,
            AvailableClasses = classes,
            AvailableChallenges = challenges,
            SelectedChallengeId = challengeId,
            SelectedClassroomId = classroomId,
            StatusFilter = status,
            PendingCount = allSubmissions.Count(x => !x.IsGraded),
            GradedCount = allSubmissions.Count(x => x.IsGraded)
        };

        return View(model);
    }

    public async Task<IActionResult> ReviewSubmission(int id, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Avaliar submissão", "submissions", GetManagementRole());

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var model = await _academicManagementService.GetSubmissionForReviewAsync(professorId.Value, id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReviewSubmission(int id, ReviewSubmissionViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Avaliar submissão", "submissions", GetManagementRole());

        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var existing = await _academicManagementService.GetSubmissionForReviewAsync(professorId.Value, id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            model.StudentName = existing.StudentName;
            model.ChallengeTitle = existing.ChallengeTitle;
            model.ClassroomName = existing.ClassroomName;
            model.ResponseText = existing.ResponseText;
            model.SubmittedAtUtc = existing.SubmittedAtUtc;
            model.UpdatedAtUtc = existing.UpdatedAtUtc;
            model.IsAlreadyGraded = existing.IsAlreadyGraded;
            return View(model);
        }

        var reviewed = await _academicManagementService.ReviewSubmissionAsync(
            professorId.Value, id, model.Grade!.Value, model.Feedback, cancellationToken);

        if (!reviewed)
        {
            return NotFound();
        }

        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = $"Avaliação de {model.StudentName} guardada com sucesso.";
        return RedirectToAction(nameof(Submissions));
    }

    public async Task<IActionResult> Progress(int? classId, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Alunos", "progress", GetManagementRole());

        IReadOnlyList<ClassroomOptionViewModel> turmas;
        IReadOnlyList<ProfessorStudentListItemViewModel> alunos;
        int? turmaId;

        if (User.IsInRole(UserRoles.Admin))
        {
            turmas  = await _academicManagementService.GetAllClassroomOptionsAsync(onlyActive: false, cancellationToken);
            turmaId = turmas.Any(x => x.Id == classId) ? classId : null;
            alunos  = await _academicManagementService.GetAllStudentsAsync(turmaId, cancellationToken);
        }
        else
        {
            var professorId = User.GetUserId();
            if (!professorId.HasValue) return Forbid();

            turmas  = await _academicManagementService.GetProfessorClassroomOptionsAsync(professorId.Value, onlyActive: false, cancellationToken);
            turmaId = turmas.Any(x => x.Id == classId) ? classId : null;
            alunos  = await _academicManagementService.GetProfessorStudentsAsync(professorId.Value, turmaId, cancellationToken);
        }

        return View(BuildProgressViewModel(turmas, alunos, turmaId));
    }

    private static ProfessorStudentsIndexViewModel BuildProgressViewModel(
        IReadOnlyList<ClassroomOptionViewModel> turmas,
        IReadOnlyList<ProfessorStudentListItemViewModel> alunos,
        int? turmaId)
    {
        var vm = new ProfessorStudentsIndexViewModel
        {
            SelectedClassId          = turmaId,
            AvailableClasses         = turmas,
            Students                 = alunos,
            TotalStudents            = alunos.Select(x => x.StudentId).Distinct().Count(),
            TotalAvailableChallenges = alunos.Sum(x => x.AvailableChallengesCount),
            TotalSubmittedChallenges = alunos.Sum(x => x.SubmittedChallengesCount),
            TotalReviewedChallenges  = alunos.Sum(x => x.ReviewedChallengesCount)
        };

        vm.OverallCompletionRateLabel = vm.TotalAvailableChallenges == 0
            ? "0%"
            : $"{Math.Round((decimal)vm.TotalSubmittedChallenges * 100m / vm.TotalAvailableChallenges, 1, MidpointRounding.AwayFromZero):0.#}%";

        return vm;
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
        IReadOnlyList<ChallengeOptionViewModel> challenges;
        IReadOnlyList<ClassroomOptionViewModel> classes;

        if (User.IsInRole(UserRoles.Admin))
        {
            challenges = await _academicManagementService.GetAllChallengeOptionsAsync(onlyDraft: true, cancellationToken);
            classes    = await _academicManagementService.GetAllClassroomOptionsAsync(cancellationToken: cancellationToken);
        }
        else
        {
            var professorId = User.GetUserId();
            if (!professorId.HasValue)
            {
                model.AvailableChallenges = [];
                model.AvailableClasses = [];
                return;
            }
            challenges = await _academicManagementService.GetProfessorChallengeOptionsAsync(professorId.Value, onlyDraft: true, cancellationToken: cancellationToken);
            classes    = await _academicManagementService.GetProfessorClassroomOptionsAsync(professorId.Value, cancellationToken: cancellationToken);
        }

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

    [HttpPost]
    public async Task<IActionResult> RemoveStudent(int classroomId, int studentId, CancellationToken cancellationToken)
    {
        var professorId = User.GetUserId();
        if (!professorId.HasValue)
        {
            return Forbid();
        }

        var success = await _academicManagementService.RemoveStudentFromClassroomAsync(
            professorId.Value, classroomId, studentId, cancellationToken);

        TempData["StatusType"] = success ? "success" : "error";
        TempData["StatusMessage"] = success
            ? "Aluno removido da turma com sucesso."
            : "Não foi possível remover o aluno da turma.";

        return RedirectToAction(nameof(Classes));
    }

    // ===== Perfil =====

    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Perfil", "profile", GetManagementRole());

        var userId = User.GetUserId();
        if (!userId.HasValue) return Forbid();

        var model = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
        if (model is null) return NotFound();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Perfil", "profile", GetManagementRole());

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

    private string GetManagementRole()
    {
        return User.IsInRole(UserRoles.Admin) ? UserRoles.Admin : UserRoles.Professor;
    }
}
