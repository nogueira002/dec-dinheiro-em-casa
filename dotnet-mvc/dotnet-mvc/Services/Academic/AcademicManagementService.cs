using System.Globalization;
using dotnet_mvc.Data;
using dotnet_mvc.Models;
using dotnet_mvc.ViewModels.Professor;
using dotnet_mvc.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Services.Academic;

public enum AccessCodeTargetType
{
    Classroom,
    Challenge
}

public sealed record JoinByAccessCodeResult(
    AccessCodeTargetType TargetType,
    string TargetName,
    bool CreatedAssociation,
    string? ClassroomName = null);

public interface IAcademicManagementService
{
    Task<IReadOnlyList<ClassroomListItemViewModel>> GetProfessorClassroomsAsync(int professorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassroomOptionViewModel>> GetProfessorClassroomOptionsAsync(int professorId, bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessorChallengeListItemViewModel>> GetProfessorChallengesAsync(int professorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChallengeOptionViewModel>> GetProfessorChallengeOptionsAsync(int professorId, bool onlyDraft = true, CancellationToken cancellationToken = default);
    Task<CreateChallengeViewModel?> GetChallengeForEditAsync(int professorId, int challengeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessorStudentListItemViewModel>> GetProfessorStudentsAsync(int professorId, int? classroomId = null, CancellationToken cancellationToken = default);
    Task<(int Id, string Name, string AccessCode)> CreateClassroomAsync(int professorId, CreateClassViewModel model, CancellationToken cancellationToken = default);
    Task<(int Id, string Title, string AccessCode)> CreateChallengeAsync(int professorId, CreateChallengeViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateChallengeAsync(int professorId, int challengeId, CreateChallengeViewModel model, CancellationToken cancellationToken = default);
    Task<(int Id, string Title, string ClassroomName, string AccessCode)> AssignChallengeAsync(int professorId, AssignChallengeViewModel model, CancellationToken cancellationToken = default);
    Task<JoinByAccessCodeResult> JoinByAccessCodeAsync(int studentId, string accessCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentChallengeListItemViewModel>> GetStudentChallengesAsync(int studentId, CancellationToken cancellationToken = default);
    Task<StudentChallengeDetailsViewModel?> GetStudentChallengeDetailsAsync(int studentId, int challengeId, CancellationToken cancellationToken = default);
    Task<bool> SaveStudentChallengeSubmissionAsync(int studentId, int challengeId, string responseText, CancellationToken cancellationToken = default);
}

public sealed class AcademicManagementService : IAcademicManagementService
{
    private readonly AppDbContext _dbContext;
    private readonly IAccessCodeService _accessCodeService;

    public AcademicManagementService(AppDbContext dbContext, IAccessCodeService accessCodeService)
    {
        _dbContext = dbContext;
        _accessCodeService = accessCodeService;
    }

    public async Task<IReadOnlyList<ClassroomListItemViewModel>> GetProfessorClassroomsAsync(int professorId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var classrooms = await _dbContext.Classrooms
            .AsNoTracking()
            .Where(x => x.OwnerId == professorId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ClassroomOverviewProjection
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                AccessCode = x.AccessCode,
                ClassroomStudentCount = x.Memberships.Count,
                ActiveChallengesCount = x.Challenges.Count(c => c.PublishedAtUtc != null && c.Deadline.HasValue && c.Deadline.Value >= today),
                TotalChallengesCount = x.Challenges.Count(c => c.PublishedAtUtc != null),
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        if (classrooms.Count == 0)
        {
            return [];
        }

        var classroomIds = classrooms
            .Select(x => x.Id)
            .ToArray();

        var participants = await GetProfessorParticipantAssociationsAsync(classroomIds, cancellationToken);
        var classroomLookup = classrooms.ToDictionary(x => x.Id);
        var challenges = await _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.ClassroomId.HasValue
                && x.PublishedAtUtc != null
                && classroomIds.Contains(x.ClassroomId.Value))
            .OrderBy(x => x.Deadline)
            .ThenBy(x => x.Title)
            .Select(x => new ChallengeOverviewProjection
            {
                Id = x.Id,
                ClassroomId = x.ClassroomId!.Value,
                Title = x.Title,
                AccessCode = x.AccessCode,
                Deadline = x.Deadline,
                IsExpired = x.Deadline.HasValue && x.Deadline.Value < today,
                DirectParticipantCount = x.Enrollments.Count
            })
            .ToListAsync(cancellationToken);

        var progressLookup = await GetParticipantProgressLookupAsync(participants, challenges, cancellationToken);
        var participantMetricsByClassroom = participants
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ParticipantProgressMetrics>)group
                    .Select(participant => progressLookup.GetValueOrDefault(
                        (participant.ClassroomId, participant.StudentId),
                        ParticipantProgressMetrics.Empty))
                    .ToList());

        var participantsByClassroom = participants
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ClassroomStudentListItemViewModel>)group
                    .OrderBy(x => x.StudentName)
                    .Select(x =>
                    {
                        var progress = progressLookup.GetValueOrDefault(
                            (x.ClassroomId, x.StudentId),
                            ParticipantProgressMetrics.Empty);

                        return new ClassroomStudentListItemViewModel
                        {
                            StudentId = x.StudentId,
                            Name = x.StudentName,
                            Email = x.StudentEmail,
                            JoinedAtUtc = x.JoinedAtUtc,
                            AssociationLabel = x.AssociationKind == ProfessorStudentAssociationKind.Classroom ? "Turma" : "Desafio direto",
                            AvailableChallengesCount = progress.ExpectedSubmissionsCount,
                            SubmittedChallengesCount = progress.SubmittedChallengesCount,
                            ReviewedChallengesCount = progress.ReviewedChallengesCount,
                            MissingChallengesCount = progress.MissingChallengesCount,
                            CompletionRatePercent = progress.CompletionRatePercent,
                            CompletionRateLabel = progress.CompletionRateLabel,
                            AverageGrade = progress.AverageGrade,
                            ChallengeSummary = x.AssociationKind == ProfessorStudentAssociationKind.Challenge
                                ? string.Join(", ", x.DirectChallengeTitles.OrderBy(title => title))
                                : null
                        };
                    })
                    .ToList());

        var challengesByClassroom = challenges
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ClassroomChallengeListItemViewModel>)group
                    .Select(x =>
                    {
                        var classroom = classroomLookup[x.ClassroomId];
                        return new ClassroomChallengeListItemViewModel
                        {
                            Id = x.Id,
                            Title = x.Title,
                            AccessCode = x.AccessCode,
                            Deadline = x.Deadline,
                            IsExpired = x.IsExpired,
                            StatusLabel = x.IsExpired ? "Expirado" : "Ativo",
                            AudienceCount = classroom.ClassroomStudentCount + x.DirectParticipantCount,
                            DirectParticipantCount = x.DirectParticipantCount
                        };
                    })
                    .ToList());

        return classrooms
            .Select(x =>
            {
                var classroomParticipants = participantsByClassroom.GetValueOrDefault(x.Id) ?? [];
                var challengeItems = challengesByClassroom.GetValueOrDefault(x.Id) ?? [];
                var participantMetrics = participantMetricsByClassroom.GetValueOrDefault(x.Id) ?? [];
                var expectedSubmissionsCount = participantMetrics.Sum(metric => metric.ExpectedSubmissionsCount);
                var submittedChallengesCount = participantMetrics.Sum(metric => metric.SubmittedChallengesCount);
                var reviewedChallengesCount = participantMetrics.Sum(metric => metric.ReviewedChallengesCount);
                var gradedSubmissionCount = participantMetrics.Sum(metric => metric.GradedSubmissionsCount);
                var gradeTotal = participantMetrics.Sum(metric => metric.GradeTotal);

                return new ClassroomListItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    AccessCode = x.AccessCode,
                    StudentCount = classroomParticipants.Count,
                    ClassroomStudentCount = x.ClassroomStudentCount,
                    DirectChallengeParticipantCount = classroomParticipants.Count(student => student.AssociationLabel == "Desafio direto"),
                    ActiveChallengesCount = x.ActiveChallengesCount,
                    TotalChallengesCount = x.TotalChallengesCount,
                    ExpectedSubmissionsCount = expectedSubmissionsCount,
                    SubmittedChallengesCount = submittedChallengesCount,
                    ReviewedChallengesCount = reviewedChallengesCount,
                    CompletionRatePercent = CalculateCompletionRatePercent(submittedChallengesCount, expectedSubmissionsCount),
                    CompletionRateLabel = FormatPercentage(submittedChallengesCount, expectedSubmissionsCount),
                    AverageGrade = FormatAverageGrade(gradeTotal, gradedSubmissionCount),
                    IsActive = x.IsActive,
                    Challenges = challengeItems,
                    Students = classroomParticipants
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ClassroomOptionViewModel>> GetProfessorClassroomOptionsAsync(int professorId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Classrooms
            .AsNoTracking()
            .Where(x => x.OwnerId == professorId);

        if (onlyActive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new ClassroomOptionViewModel
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfessorChallengeListItemViewModel>> GetProfessorChallengesAsync(int professorId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var challenges = await _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.CreatedByUserId == professorId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.AccessCode,
                x.CreatedAtUtc,
                x.PublishedAtUtc,
                x.Deadline,
                ClassroomName = x.Classroom != null ? x.Classroom.Name : null,
                AudienceCount = x.ClassroomId.HasValue
                    ? x.Classroom!.Memberships.Count + x.Enrollments.Count
                    : 0
            })
            .ToListAsync(cancellationToken);

        return challenges
            .Select(x =>
            {
                var isDraft = !x.PublishedAtUtc.HasValue || !x.Deadline.HasValue || string.IsNullOrWhiteSpace(x.ClassroomName);
                var isExpired = x.Deadline.HasValue && x.Deadline.Value < today;

                return new ProfessorChallengeListItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    AccessCode = x.AccessCode,
                    CreatedAtUtc = x.CreatedAtUtc,
                    PublishedAtUtc = x.PublishedAtUtc,
                    Deadline = x.Deadline,
                    ClassroomName = x.ClassroomName,
                    AudienceCount = x.AudienceCount,
                    CanAssign = isDraft,
                    StatusKey = isDraft ? "draft" : isExpired ? "expired" : "published",
                    StatusLabel = isDraft ? "Rascunho" : isExpired ? "Expirado" : "Atribuído"
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ChallengeOptionViewModel>> GetProfessorChallengeOptionsAsync(int professorId, bool onlyDraft = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.CreatedByUserId == professorId);

        if (onlyDraft)
        {
            query = query.Where(x => x.PublishedAtUtc == null || !x.ClassroomId.HasValue || !x.Deadline.HasValue);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ChallengeOptionViewModel
            {
                Id = x.Id,
                Title = x.Title,
                AccessCode = x.AccessCode
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CreateChallengeViewModel?> GetChallengeForEditAsync(int professorId, int challengeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.CreatedByUserId == professorId && x.Id == challengeId)
            .Select(x => new CreateChallengeViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                GeneratedCodePreview = x.AccessCode
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfessorStudentListItemViewModel>> GetProfessorStudentsAsync(int professorId, int? classroomId = null, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var classroomQuery = _dbContext.Classrooms
            .AsNoTracking()
            .Where(x => x.OwnerId == professorId);

        if (classroomId.HasValue)
        {
            classroomQuery = classroomQuery.Where(x => x.Id == classroomId.Value);
        }

        var classrooms = await classroomQuery
            .OrderBy(x => x.Name)
            .Select(x => new ClassroomProgressProjection
            {
                Id = x.Id,
                Name = x.Name,
                TotalChallengesCount = x.Challenges.Count(c => c.PublishedAtUtc != null),
                ActiveChallengesCount = x.Challenges.Count(c => c.PublishedAtUtc != null && c.Deadline.HasValue && c.Deadline.Value >= today)
            })
            .ToListAsync(cancellationToken);

        if (classrooms.Count == 0)
        {
            return [];
        }

        var classroomIds = classrooms
            .Select(x => x.Id)
            .ToArray();

        var participants = await GetProfessorParticipantAssociationsAsync(classroomIds, cancellationToken);
        var classroomLookup = classrooms.ToDictionary(x => x.Id);
        var publishedChallenges = await _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.ClassroomId.HasValue
                && x.PublishedAtUtc != null
                && classroomIds.Contains(x.ClassroomId.Value))
            .Select(x => new ChallengeOverviewProjection
            {
                Id = x.Id,
                ClassroomId = x.ClassroomId!.Value,
                Title = x.Title,
                AccessCode = x.AccessCode,
                Deadline = x.Deadline,
                IsExpired = x.Deadline.HasValue && x.Deadline.Value < today,
                DirectParticipantCount = x.Enrollments.Count
            })
            .ToListAsync(cancellationToken);
        var progressLookup = await GetParticipantProgressLookupAsync(participants, publishedChallenges, cancellationToken);

        return participants
            .OrderBy(x => x.StudentName)
            .ThenBy(x => classroomLookup[x.ClassroomId].Name)
            .Select(x =>
            {
                var classroom = classroomLookup[x.ClassroomId];
                var progress = progressLookup.GetValueOrDefault(
                    (x.ClassroomId, x.StudentId),
                    ParticipantProgressMetrics.Empty);
                var availableChallengesCount = progress.ExpectedSubmissionsCount;
                var activeChallengesCount = x.AssociationKind == ProfessorStudentAssociationKind.Classroom
                    ? classroom.ActiveChallengesCount
                    : x.DirectActiveChallengeCount;

                return new ProfessorStudentListItemViewModel
                {
                    StudentId = x.StudentId,
                    Name = x.StudentName,
                    Email = x.StudentEmail,
                    ClassroomName = classroom.Name,
                    JoinedAtUtc = x.JoinedAtUtc,
                    AssociationLabel = x.AssociationKind == ProfessorStudentAssociationKind.Classroom ? "Turma" : "Desafio direto",
                    AvailableChallengesCount = availableChallengesCount,
                    ActiveChallengesCount = activeChallengesCount,
                    SubmittedChallengesCount = progress.SubmittedChallengesCount,
                    ReviewedChallengesCount = progress.ReviewedChallengesCount,
                    MissingChallengesCount = progress.MissingChallengesCount,
                    CompletionRatePercent = progress.CompletionRatePercent,
                    CompletionRateLabel = progress.CompletionRateLabel,
                    AverageGrade = progress.AverageGrade,
                    ChallengeSummary = x.AssociationKind == ProfessorStudentAssociationKind.Classroom
                        ? $"Acesso aos {classroom.TotalChallengesCount} desafios da turma."
                        : string.Join(", ", x.DirectChallengeTitles.OrderBy(title => title))
                };
            })
            .ToList();
    }

    public async Task<(int Id, string Name, string AccessCode)> CreateClassroomAsync(int professorId, CreateClassViewModel model, CancellationToken cancellationToken = default)
    {
        var classroom = new Classroom
        {
            OwnerId = professorId,
            Name = model.ClassName.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            AccessCode = await _accessCodeService.GenerateUniqueCodeAsync(cancellationToken),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.Classrooms.Add(classroom);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (classroom.Id, classroom.Name, classroom.AccessCode);
    }

    public async Task<(int Id, string Title, string AccessCode)> CreateChallengeAsync(int professorId, CreateChallengeViewModel model, CancellationToken cancellationToken = default)
    {
        var challenge = new Challenge
        {
            CreatedByUserId = professorId,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            AccessCode = await _accessCodeService.GenerateUniqueCodeAsync(cancellationToken),
            NotifyStudents = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Challenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (challenge.Id, challenge.Title, challenge.AccessCode);
    }

    public async Task<bool> UpdateChallengeAsync(int professorId, int challengeId, CreateChallengeViewModel model, CancellationToken cancellationToken = default)
    {
        var challenge = await _dbContext.Challenges
            .SingleOrDefaultAsync(x => x.CreatedByUserId == professorId && x.Id == challengeId, cancellationToken);

        if (challenge is null)
        {
            return false;
        }

        challenge.Title = model.Title.Trim();
        challenge.Description = model.Description.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(int Id, string Title, string ClassroomName, string AccessCode)> AssignChallengeAsync(int professorId, AssignChallengeViewModel model, CancellationToken cancellationToken = default)
    {
        var challenge = await _dbContext.Challenges
            .SingleOrDefaultAsync(x => x.Id == model.ChallengeId && x.CreatedByUserId == professorId, cancellationToken);

        if (challenge is null)
        {
            throw new InvalidOperationException("O desafio selecionado não existe ou já não está disponível.");
        }

        if (challenge.PublishedAtUtc.HasValue && challenge.ClassroomId.HasValue)
        {
            throw new InvalidOperationException("Este desafio já foi atribuído a uma turma.");
        }

        var classroom = await _dbContext.Classrooms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == model.ClassId && x.OwnerId == professorId && x.IsActive, cancellationToken);

        if (classroom is null)
        {
            throw new InvalidOperationException("A turma selecionada não existe ou já não está disponível.");
        }

        challenge.ClassroomId = classroom.Id;
        challenge.Deadline = model.Deadline!.Value.Date;
        challenge.NotifyStudents = model.NotifyStudents;
        challenge.PublishedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (challenge.Id, challenge.Title, classroom.Name, challenge.AccessCode);
    }

    public async Task<JoinByAccessCodeResult> JoinByAccessCodeAsync(int studentId, string accessCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeAccessCode(accessCode);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Indica um código de adesão válido.");
        }

        var classroom = await _dbContext.Classrooms
            .SingleOrDefaultAsync(x => x.AccessCode == normalizedCode && x.IsActive, cancellationToken);

        if (classroom is not null)
        {
            return await JoinClassroomAsync(studentId, classroom, cancellationToken);
        }

        var challenge = await _dbContext.Challenges
            .Include(x => x.Classroom)
            .SingleOrDefaultAsync(x =>
                x.AccessCode == normalizedCode
                && x.PublishedAtUtc != null
                && x.ClassroomId.HasValue
                && x.Classroom != null
                && x.Classroom.IsActive,
                cancellationToken);

        if (challenge is not null)
        {
            return await JoinChallengeAsync(studentId, challenge, cancellationToken);
        }

        throw new InvalidOperationException("O código de adesão não existe ou já não está disponível.");
    }

    public async Task<IReadOnlyList<StudentChallengeListItemViewModel>> GetStudentChallengesAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var challenges = await GetAccessibleStudentChallengesQuery(studentId)
            .OrderBy(x => x.Deadline)
            .ThenBy(x => x.Title)
            .Select(challenge => new
            {
                challenge.Id,
                challenge.Title,
                challenge.Description,
                Professor = challenge.CreatedByUser.Name,
                ClassroomName = challenge.Classroom!.Name,
                Deadline = challenge.Deadline!.Value,
                Submission = challenge.Submissions
                    .Where(submission => submission.StudentId == studentId)
                    .Select(submission => new
                    {
                        submission.Grade,
                        submission.SubmittedAtUtc
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return challenges
            .Select(challenge =>
            {
                var hasSubmission = challenge.Submission is not null;
                var isGraded = challenge.Submission?.Grade.HasValue == true;
                var status = ResolveStudentChallengeStatus(challenge.Deadline, hasSubmission, isGraded, today);

                return new StudentChallengeListItemViewModel
                {
                    Id = challenge.Id,
                    Title = challenge.Title,
                    Description = challenge.Description,
                    Professor = challenge.Professor,
                    ClassroomName = challenge.ClassroomName,
                    Deadline = challenge.Deadline,
                    StatusKey = status.Key,
                    StatusLabel = status.Label,
                    Grade = challenge.Submission?.Grade.HasValue == true
                        ? FormatGrade(challenge.Submission.Grade.Value)
                        : null,
                    HasSubmission = hasSubmission,
                    CanEditSubmission = CanEditStudentSubmission(challenge.Deadline, isGraded, today),
                    SubmittedAtUtc = challenge.Submission?.SubmittedAtUtc
                };
            })
            .ToList();
    }

    public async Task<StudentChallengeDetailsViewModel?> GetStudentChallengeDetailsAsync(int studentId, int challengeId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var challenge = await GetAccessibleStudentChallengesQuery(studentId)
            .Where(x => x.Id == challengeId)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                Professor = x.CreatedByUser.Name,
                ClassroomName = x.Classroom!.Name,
                Deadline = x.Deadline!.Value,
                Submission = x.Submissions
                    .Where(submission => submission.StudentId == studentId)
                    .Select(submission => new
                    {
                        submission.ResponseText,
                        submission.SubmittedAtUtc,
                        submission.UpdatedAtUtc,
                        submission.Grade,
                        submission.Feedback,
                        submission.ReviewedAtUtc
                    })
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (challenge is null)
        {
            return null;
        }

        var hasSubmission = challenge.Submission is not null;
        var isGraded = challenge.Submission?.Grade.HasValue == true;
        var status = ResolveStudentChallengeStatus(challenge.Deadline, hasSubmission, isGraded, today);

        return new StudentChallengeDetailsViewModel
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            Professor = challenge.Professor,
            ClassroomName = challenge.ClassroomName,
            Deadline = challenge.Deadline,
            StatusKey = status.Key,
            StatusLabel = status.Label,
            GradeDisplay = challenge.Submission?.Grade.HasValue == true
                ? FormatGrade(challenge.Submission.Grade.Value)
                : null,
            Feedback = string.IsNullOrWhiteSpace(challenge.Submission?.Feedback)
                ? null
                : challenge.Submission.Feedback,
            SubmittedAtUtc = challenge.Submission?.SubmittedAtUtc,
            UpdatedAtUtc = challenge.Submission?.UpdatedAtUtc,
            HasSubmission = hasSubmission,
            CanEditSubmission = CanEditStudentSubmission(challenge.Deadline, isGraded, today),
            ResponseText = challenge.Submission?.ResponseText ?? string.Empty
        };
    }

    public async Task<bool> SaveStudentChallengeSubmissionAsync(int studentId, int challengeId, string responseText, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var challenge = await _dbContext.Challenges
            .SingleOrDefaultAsync(x => x.Id == challengeId
                && x.PublishedAtUtc != null
                && x.ClassroomId.HasValue
                && x.Deadline.HasValue
                && x.Classroom != null
                && x.Classroom.IsActive
                && (x.Classroom.Memberships.Any(membership => membership.StudentId == studentId)
                    || x.Enrollments.Any(enrollment => enrollment.StudentId == studentId)),
                cancellationToken);

        if (challenge is null)
        {
            return false;
        }

        if (challenge.Deadline!.Value < today)
        {
            throw new InvalidOperationException("O prazo deste desafio já terminou. Já não é possível submeter ou editar a resposta.");
        }

        var submission = await _dbContext.ChallengeSubmissions
            .SingleOrDefaultAsync(x => x.ChallengeId == challengeId && x.StudentId == studentId, cancellationToken);

        if (submission is not null && submission.Grade.HasValue)
        {
            throw new InvalidOperationException("Esta submissão já foi avaliada e deixou de poder ser alterada.");
        }

        var normalizedResponse = responseText.Trim();
        var now = DateTime.UtcNow;

        if (submission is null)
        {
            _dbContext.ChallengeSubmissions.Add(new ChallengeSubmission
            {
                ChallengeId = challengeId,
                StudentId = studentId,
                ResponseText = normalizedResponse,
                SubmittedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            submission.ResponseText = normalizedResponse;
            submission.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<JoinByAccessCodeResult> JoinClassroomAsync(int studentId, Classroom classroom, CancellationToken cancellationToken)
    {
        var alreadyMember = await _dbContext.ClassroomMemberships
            .AnyAsync(x => x.ClassroomId == classroom.Id && x.StudentId == studentId, cancellationToken);

        if (alreadyMember)
        {
            return new JoinByAccessCodeResult(AccessCodeTargetType.Classroom, classroom.Name, false, classroom.Name);
        }

        _dbContext.ClassroomMemberships.Add(new ClassroomMembership
        {
            ClassroomId = classroom.Id,
            StudentId = studentId,
            JoinedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new JoinByAccessCodeResult(AccessCodeTargetType.Classroom, classroom.Name, true, classroom.Name);
    }

    private async Task<JoinByAccessCodeResult> JoinChallengeAsync(int studentId, Challenge challenge, CancellationToken cancellationToken)
    {
        var classroomName = challenge.Classroom?.Name ?? "turma";

        var alreadyHasClassroomAccess = await _dbContext.ClassroomMemberships
            .AnyAsync(x => x.ClassroomId == challenge.ClassroomId!.Value && x.StudentId == studentId, cancellationToken);

        if (alreadyHasClassroomAccess)
        {
            return new JoinByAccessCodeResult(AccessCodeTargetType.Challenge, challenge.Title, false, classroomName);
        }

        var alreadyEnrolled = await _dbContext.ChallengeEnrollments
            .AnyAsync(x => x.ChallengeId == challenge.Id && x.StudentId == studentId, cancellationToken);

        if (alreadyEnrolled)
        {
            return new JoinByAccessCodeResult(AccessCodeTargetType.Challenge, challenge.Title, false, classroomName);
        }

        _dbContext.ChallengeEnrollments.Add(new ChallengeEnrollment
        {
            ChallengeId = challenge.Id,
            StudentId = studentId,
            JoinedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new JoinByAccessCodeResult(AccessCodeTargetType.Challenge, challenge.Title, true, classroomName);
    }

    private static string NormalizeAccessCode(string accessCode)
    {
        return new string((accessCode ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character))
            .ToArray())
            .Trim()
            .ToUpperInvariant();
    }

    private IQueryable<Challenge> GetAccessibleStudentChallengesQuery(int studentId)
    {
        return _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.PublishedAtUtc != null
                && x.ClassroomId.HasValue
                && x.Deadline.HasValue
                && x.Classroom != null
                && x.Classroom.IsActive
                && (x.Classroom.Memberships.Any(membership => membership.StudentId == studentId)
                    || x.Enrollments.Any(enrollment => enrollment.StudentId == studentId)));
    }

    private static (string Key, string Label) ResolveStudentChallengeStatus(
        DateTime deadline,
        bool hasSubmission,
        bool isGraded,
        DateTime today)
    {
        if (isGraded)
        {
            return ("graded", "Avaliado");
        }

        if (hasSubmission)
        {
            return ("submitted", "Submetido");
        }

        return deadline < today
            ? ("expired", "Expirado")
            : ("pending", "Pendente");
    }

    private static bool CanEditStudentSubmission(DateTime deadline, bool isGraded, DateTime today)
    {
        return !isGraded && deadline >= today;
    }

    private static string FormatGrade(decimal grade)
    {
        return $"{grade.ToString("0.#", CultureInfo.GetCultureInfo("pt-PT"))}/20";
    }

    private static decimal CalculateCompletionRatePercent(int completedCount, int totalCount)
    {
        if (totalCount <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)completedCount * 100m / totalCount, 1, MidpointRounding.AwayFromZero);
    }

    private static string FormatPercentage(int completedCount, int totalCount)
    {
        var percentage = CalculateCompletionRatePercent(completedCount, totalCount);
        return $"{percentage.ToString("0.#", CultureInfo.GetCultureInfo("pt-PT"))}%";
    }

    private static string? FormatAverageGrade(decimal totalGrade, int gradeCount)
    {
        if (gradeCount <= 0)
        {
            return null;
        }

        return FormatGrade(totalGrade / gradeCount);
    }

    private async Task<Dictionary<(int ClassroomId, int StudentId), ParticipantProgressMetrics>> GetParticipantProgressLookupAsync(
        IReadOnlyList<ProfessorParticipantAssociation> participants,
        IReadOnlyList<ChallengeOverviewProjection> challenges,
        CancellationToken cancellationToken)
    {
        if (participants.Count == 0 || challenges.Count == 0)
        {
            return [];
        }

        var challengeIds = challenges
            .Select(x => x.Id)
            .Distinct()
            .ToArray();
        var studentIds = participants
            .Select(x => x.StudentId)
            .Distinct()
            .ToArray();
        var challengeIdsByClassroom = challenges
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group
                    .Select(x => x.Id)
                    .ToArray());

        var submissions = await _dbContext.ChallengeSubmissions
            .AsNoTracking()
            .Where(x => challengeIds.Contains(x.ChallengeId) && studentIds.Contains(x.StudentId))
            .Select(x => new ChallengeSubmissionProgressProjection
            {
                ChallengeId = x.ChallengeId,
                StudentId = x.StudentId,
                Grade = x.Grade
            })
            .ToListAsync(cancellationToken);

        var submissionsByStudent = submissions
            .GroupBy(x => x.StudentId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(x => x.ChallengeId));

        var result = new Dictionary<(int ClassroomId, int StudentId), ParticipantProgressMetrics>();

        foreach (var participant in participants)
        {
            var availableChallengeIds = participant.AssociationKind == ProfessorStudentAssociationKind.Classroom
                ? challengeIdsByClassroom.GetValueOrDefault(participant.ClassroomId) ?? []
                : participant.DirectChallengeIds;

            var distinctChallengeIds = availableChallengeIds
                .Distinct()
                .ToArray();

            submissionsByStudent.TryGetValue(participant.StudentId, out var studentSubmissions);

            var submittedChallengesCount = 0;
            var reviewedChallengesCount = 0;
            decimal gradeTotal = 0m;

            foreach (var challengeId in distinctChallengeIds)
            {
                if (studentSubmissions is null || !studentSubmissions.TryGetValue(challengeId, out var submission))
                {
                    continue;
                }

                submittedChallengesCount++;

                if (submission.Grade.HasValue)
                {
                    reviewedChallengesCount++;
                    gradeTotal += submission.Grade.Value;
                }
            }

            var expectedSubmissionsCount = distinctChallengeIds.Length;
            result[(participant.ClassroomId, participant.StudentId)] = new ParticipantProgressMetrics
            {
                ExpectedSubmissionsCount = expectedSubmissionsCount,
                SubmittedChallengesCount = submittedChallengesCount,
                ReviewedChallengesCount = reviewedChallengesCount,
                MissingChallengesCount = Math.Max(expectedSubmissionsCount - submittedChallengesCount, 0),
                CompletionRatePercent = CalculateCompletionRatePercent(submittedChallengesCount, expectedSubmissionsCount),
                CompletionRateLabel = FormatPercentage(submittedChallengesCount, expectedSubmissionsCount),
                AverageGrade = FormatAverageGrade(gradeTotal, reviewedChallengesCount),
                GradeTotal = gradeTotal,
                GradedSubmissionsCount = reviewedChallengesCount
            };
        }

        return result;
    }

    private async Task<IReadOnlyList<ProfessorParticipantAssociation>> GetProfessorParticipantAssociationsAsync(
        IReadOnlyCollection<int> classroomIds,
        CancellationToken cancellationToken)
    {
        if (classroomIds.Count == 0)
        {
            return [];
        }

        var today = DateTime.UtcNow.Date;
        var participantLookup = new Dictionary<(int ClassroomId, int StudentId), ParticipantAccumulator>();

        var classroomMemberships = await _dbContext.ClassroomMemberships
            .AsNoTracking()
            .Where(x => classroomIds.Contains(x.ClassroomId))
            .Select(x => new
            {
                x.ClassroomId,
                x.StudentId,
                StudentName = x.Student.Name,
                StudentEmail = x.Student.Email,
                x.JoinedAtUtc
            })
            .ToListAsync(cancellationToken);

        foreach (var membership in classroomMemberships)
        {
            var key = (membership.ClassroomId, membership.StudentId);
            if (!participantLookup.TryGetValue(key, out var accumulator))
            {
                accumulator = new ParticipantAccumulator(
                    membership.ClassroomId,
                    membership.StudentId,
                    membership.StudentName,
                    membership.StudentEmail,
                    membership.JoinedAtUtc);
                participantLookup[key] = accumulator;
            }

            accumulator.MarkClassroomMembership(membership.JoinedAtUtc);
        }

        var challengeEnrollments = await _dbContext.ChallengeEnrollments
            .AsNoTracking()
            .Where(x => x.Challenge.ClassroomId.HasValue
                && x.Challenge.PublishedAtUtc != null
                && classroomIds.Contains(x.Challenge.ClassroomId.Value))
            .Select(x => new
            {
                ChallengeId = x.ChallengeId,
                ClassroomId = x.Challenge.ClassroomId!.Value,
                x.StudentId,
                StudentName = x.Student.Name,
                StudentEmail = x.Student.Email,
                x.JoinedAtUtc,
                ChallengeTitle = x.Challenge.Title,
                ChallengeIsActive = x.Challenge.Deadline.HasValue && x.Challenge.Deadline.Value >= today
            })
            .ToListAsync(cancellationToken);

        foreach (var enrollment in challengeEnrollments)
        {
            var key = (enrollment.ClassroomId, enrollment.StudentId);
            if (!participantLookup.TryGetValue(key, out var accumulator))
            {
                accumulator = new ParticipantAccumulator(
                    enrollment.ClassroomId,
                    enrollment.StudentId,
                    enrollment.StudentName,
                    enrollment.StudentEmail,
                    enrollment.JoinedAtUtc);
                participantLookup[key] = accumulator;
            }

            accumulator.AddDirectChallenge(enrollment.ChallengeId, enrollment.JoinedAtUtc, enrollment.ChallengeTitle, enrollment.ChallengeIsActive);
        }

        return participantLookup.Values
            .Select(x => x.ToAssociation())
            .ToList();
    }

    private sealed class ClassroomOverviewProjection
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string AccessCode { get; init; } = string.Empty;
        public int ClassroomStudentCount { get; init; }
        public int ActiveChallengesCount { get; init; }
        public int TotalChallengesCount { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class ClassroomProgressProjection
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int TotalChallengesCount { get; init; }
        public int ActiveChallengesCount { get; init; }
    }

    private sealed class ChallengeOverviewProjection
    {
        public int Id { get; init; }
        public int ClassroomId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string AccessCode { get; init; } = string.Empty;
        public DateTime? Deadline { get; init; }
        public bool IsExpired { get; init; }
        public int DirectParticipantCount { get; init; }
    }

    private sealed class ChallengeSubmissionProgressProjection
    {
        public int ChallengeId { get; init; }
        public int StudentId { get; init; }
        public decimal? Grade { get; init; }
    }

    private sealed class ParticipantProgressMetrics
    {
        public static ParticipantProgressMetrics Empty { get; } = new();

        public int ExpectedSubmissionsCount { get; init; }
        public int SubmittedChallengesCount { get; init; }
        public int ReviewedChallengesCount { get; init; }
        public int MissingChallengesCount { get; init; }
        public decimal CompletionRatePercent { get; init; }
        public string CompletionRateLabel { get; init; } = "0%";
        public string? AverageGrade { get; init; }
        public decimal GradeTotal { get; init; }
        public int GradedSubmissionsCount { get; init; }
    }

    private enum ProfessorStudentAssociationKind
    {
        Classroom,
        Challenge
    }

    private sealed class ParticipantAccumulator
    {
        public ParticipantAccumulator(int classroomId, int studentId, string studentName, string studentEmail, DateTime joinedAtUtc)
        {
            ClassroomId = classroomId;
            StudentId = studentId;
            StudentName = studentName;
            StudentEmail = studentEmail;
            JoinedAtUtc = joinedAtUtc;
        }

        public int ClassroomId { get; }
        public int StudentId { get; }
        public string StudentName { get; }
        public string StudentEmail { get; }
        public DateTime JoinedAtUtc { get; private set; }
        public ProfessorStudentAssociationKind AssociationKind { get; private set; } = ProfessorStudentAssociationKind.Challenge;
        public int DirectChallengeCount { get; private set; }
        public int DirectActiveChallengeCount { get; private set; }
        public List<int> DirectChallengeIds { get; } = [];
        public List<string> DirectChallengeTitles { get; } = [];

        public void MarkClassroomMembership(DateTime joinedAtUtc)
        {
            AssociationKind = ProfessorStudentAssociationKind.Classroom;
            JoinedAtUtc = Earliest(JoinedAtUtc, joinedAtUtc);
        }

        public void AddDirectChallenge(int challengeId, DateTime joinedAtUtc, string challengeTitle, bool isActive)
        {
            JoinedAtUtc = Earliest(JoinedAtUtc, joinedAtUtc);
            DirectChallengeIds.Add(challengeId);
            DirectChallengeTitles.Add(challengeTitle);
            DirectChallengeCount++;

            if (isActive)
            {
                DirectActiveChallengeCount++;
            }
        }

        public ProfessorParticipantAssociation ToAssociation()
        {
            return new ProfessorParticipantAssociation
            {
                ClassroomId = ClassroomId,
                StudentId = StudentId,
                StudentName = StudentName,
                StudentEmail = StudentEmail,
                JoinedAtUtc = JoinedAtUtc,
                AssociationKind = AssociationKind,
                DirectChallengeCount = DirectChallengeCount,
                DirectActiveChallengeCount = DirectActiveChallengeCount,
                DirectChallengeIds = DirectChallengeIds
                    .Distinct()
                    .ToArray(),
                DirectChallengeTitles = DirectChallengeTitles
                    .Distinct()
                    .ToArray()
            };
        }

        private static DateTime Earliest(DateTime left, DateTime right)
        {
            return left <= right ? left : right;
        }
    }

    private sealed class ProfessorParticipantAssociation
    {
        public int ClassroomId { get; init; }
        public int StudentId { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public string StudentEmail { get; init; } = string.Empty;
        public DateTime JoinedAtUtc { get; init; }
        public ProfessorStudentAssociationKind AssociationKind { get; init; }
        public int DirectChallengeCount { get; init; }
        public int DirectActiveChallengeCount { get; init; }
        public IReadOnlyList<int> DirectChallengeIds { get; init; } = [];
        public IReadOnlyList<string> DirectChallengeTitles { get; init; } = [];
    }
}
