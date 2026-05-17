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
    Task<IReadOnlyList<ProfessorStudentListItemViewModel>> GetProfessorStudentsAsync(int professorId, int? classroomId = null, CancellationToken cancellationToken = default);
    Task<(int Id, string Name, string AccessCode)> CreateClassroomAsync(int professorId, CreateClassViewModel model, CancellationToken cancellationToken = default);
    Task<(int Id, string Title, string AccessCode)> CreateChallengeAsync(int professorId, CreateChallengeViewModel model, CancellationToken cancellationToken = default);
    Task<JoinByAccessCodeResult> JoinByAccessCodeAsync(int studentId, string accessCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentChallengeListItemViewModel>> GetStudentChallengesAsync(int studentId, CancellationToken cancellationToken = default);
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
                ActiveChallengesCount = x.Challenges.Count(c => c.Deadline >= today),
                TotalChallengesCount = x.Challenges.Count,
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
            .Where(x => classroomIds.Contains(x.ClassroomId))
            .OrderBy(x => x.Deadline)
            .ThenBy(x => x.Title)
            .Select(x => new ChallengeOverviewProjection
            {
                Id = x.Id,
                ClassroomId = x.ClassroomId,
                Title = x.Title,
                AccessCode = x.AccessCode,
                Deadline = x.Deadline,
                IsExpired = x.Deadline < today,
                DirectParticipantCount = x.Enrollments.Count
            })
            .ToListAsync(cancellationToken);

        var participantsByClassroom = participants
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ClassroomStudentListItemViewModel>)group
                    .OrderBy(x => x.StudentName)
                    .Select(x => new ClassroomStudentListItemViewModel
                    {
                        StudentId = x.StudentId,
                        Name = x.StudentName,
                        Email = x.StudentEmail,
                        JoinedAtUtc = x.JoinedAtUtc,
                        AssociationLabel = x.AssociationKind == ProfessorStudentAssociationKind.Classroom ? "Turma" : "Desafio direto",
                        ChallengeSummary = x.AssociationKind == ProfessorStudentAssociationKind.Challenge
                            ? string.Join(", ", x.DirectChallengeTitles.OrderBy(title => title))
                            : null
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
                TotalChallengesCount = x.Challenges.Count,
                ActiveChallengesCount = x.Challenges.Count(c => c.Deadline >= today)
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

        return participants
            .OrderBy(x => x.StudentName)
            .ThenBy(x => classroomLookup[x.ClassroomId].Name)
            .Select(x =>
            {
                var classroom = classroomLookup[x.ClassroomId];
                var availableChallengesCount = x.AssociationKind == ProfessorStudentAssociationKind.Classroom
                    ? classroom.TotalChallengesCount
                    : x.DirectChallengeCount;
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
        var classroom = await _dbContext.Classrooms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == model.ClassId && x.OwnerId == professorId && x.IsActive, cancellationToken);

        if (classroom is null)
        {
            throw new InvalidOperationException("A turma selecionada não existe ou já não está disponível.");
        }

        var challenge = new Challenge
        {
            ClassroomId = classroom.Id,
            CreatedByUserId = professorId,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            AccessCode = await _accessCodeService.GenerateUniqueCodeAsync(cancellationToken),
            Deadline = model.Deadline!.Value.Date,
            NotifyStudents = model.NotifyStudents,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Challenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (challenge.Id, challenge.Title, challenge.AccessCode);
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
            .SingleOrDefaultAsync(x => x.AccessCode == normalizedCode && x.Classroom.IsActive, cancellationToken);

        if (challenge is not null)
        {
            return await JoinChallengeAsync(studentId, challenge, cancellationToken);
        }

        throw new InvalidOperationException("O código de adesão não existe ou já não está disponível.");
    }

    public async Task<IReadOnlyList<StudentChallengeListItemViewModel>> GetStudentChallengesAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.Classroom.IsActive
                && (x.Classroom.Memberships.Any(membership => membership.StudentId == studentId)
                    || x.Enrollments.Any(enrollment => enrollment.StudentId == studentId)))
            .OrderBy(x => x.Deadline)
            .Select(challenge => new StudentChallengeListItemViewModel
            {
                Title = challenge.Title,
                Description = challenge.Description,
                Professor = challenge.CreatedByUser.Name,
                ClassroomName = challenge.Classroom.Name,
                Deadline = challenge.Deadline,
                StatusKey = challenge.Deadline < today ? "expired" : "pending",
                StatusLabel = challenge.Deadline < today ? "Expirado" : "Pendente"
            })
            .ToListAsync(cancellationToken);
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
        var alreadyHasClassroomAccess = await _dbContext.ClassroomMemberships
            .AnyAsync(x => x.ClassroomId == challenge.ClassroomId && x.StudentId == studentId, cancellationToken);

        if (alreadyHasClassroomAccess)
        {
            return new JoinByAccessCodeResult(AccessCodeTargetType.Challenge, challenge.Title, false, challenge.Classroom.Name);
        }

        var alreadyEnrolled = await _dbContext.ChallengeEnrollments
            .AnyAsync(x => x.ChallengeId == challenge.Id && x.StudentId == studentId, cancellationToken);

        if (alreadyEnrolled)
        {
            return new JoinByAccessCodeResult(AccessCodeTargetType.Challenge, challenge.Title, false, challenge.Classroom.Name);
        }

        _dbContext.ChallengeEnrollments.Add(new ChallengeEnrollment
        {
            ChallengeId = challenge.Id,
            StudentId = studentId,
            JoinedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new JoinByAccessCodeResult(AccessCodeTargetType.Challenge, challenge.Title, true, challenge.Classroom.Name);
    }

    private static string NormalizeAccessCode(string accessCode)
    {
        return new string((accessCode ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character))
            .ToArray())
            .Trim()
            .ToUpperInvariant();
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
            .Where(x => classroomIds.Contains(x.Challenge.ClassroomId))
            .Select(x => new
            {
                ClassroomId = x.Challenge.ClassroomId,
                x.StudentId,
                StudentName = x.Student.Name,
                StudentEmail = x.Student.Email,
                x.JoinedAtUtc,
                ChallengeTitle = x.Challenge.Title,
                ChallengeIsActive = x.Challenge.Deadline >= today
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

            accumulator.AddDirectChallenge(enrollment.JoinedAtUtc, enrollment.ChallengeTitle, enrollment.ChallengeIsActive);
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
        public DateTime Deadline { get; init; }
        public bool IsExpired { get; init; }
        public int DirectParticipantCount { get; init; }
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
        public List<string> DirectChallengeTitles { get; } = [];

        public void MarkClassroomMembership(DateTime joinedAtUtc)
        {
            AssociationKind = ProfessorStudentAssociationKind.Classroom;
            JoinedAtUtc = Earliest(JoinedAtUtc, joinedAtUtc);
        }

        public void AddDirectChallenge(DateTime joinedAtUtc, string challengeTitle, bool isActive)
        {
            JoinedAtUtc = Earliest(JoinedAtUtc, joinedAtUtc);
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
                DirectChallengeTitles = DirectChallengeTitles
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
        public IReadOnlyList<string> DirectChallengeTitles { get; init; } = [];
    }
}
