using System.Data;
using System.Globalization;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Professor;
using dotnet_mvc.ViewModels.Student;
using MySqlConnector;

namespace dotnet_mvc.Services.Academic;

public enum AccessCodeTargetType { Classroom, Challenge }

public sealed record JoinByAccessCodeResult(
    AccessCodeTargetType TargetType,
    string TargetName,
    bool CreatedAssociation,
    string? ClassroomName = null);

public interface IAcademicManagementService
{
    Task<IReadOnlyList<ClassroomListItemViewModel>> GetProfessorClassroomsAsync(int professorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassroomListItemViewModel>> GetAllClassroomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassroomOptionViewModel>> GetProfessorClassroomOptionsAsync(int professorId, bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassroomOptionViewModel>> GetAllClassroomOptionsAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessorChallengeListItemViewModel>> GetProfessorChallengesAsync(int professorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessorChallengeListItemViewModel>> GetAllChallengesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChallengeOptionViewModel>> GetProfessorChallengeOptionsAsync(int professorId, bool onlyDraft = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChallengeOptionViewModel>> GetAllChallengeOptionsAsync(bool onlyDraft = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessorStudentListItemViewModel>> GetAllStudentsAsync(int? classroomId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubmissionListItemViewModel>> GetAllSubmissionsAsync(int? challengeId = null, int? classroomId = null, CancellationToken cancellationToken = default);
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
    Task<IReadOnlyList<SubmissionListItemViewModel>> GetProfessorSubmissionsAsync(int professorId, int? challengeId = null, int? classroomId = null, CancellationToken cancellationToken = default);
    Task<ReviewSubmissionViewModel?> GetSubmissionForReviewAsync(int professorId, int submissionId, CancellationToken cancellationToken = default);
    Task<bool> ReviewSubmissionAsync(int professorId, int submissionId, decimal grade, string? feedback, CancellationToken cancellationToken = default);
    Task<bool> RemoveStudentFromClassroomAsync(int professorId, int classroomId, int studentId, CancellationToken cancellationToken = default);
    Task<ProfessorDashboardViewModel> GetProfessorDashboardAsync(int professorId, CancellationToken cancellationToken = default);
}

public sealed class AcademicManagementService : DriverDB, IAcademicManagementService
{
    private readonly IAccessCodeService _accessCodeService;

    public AcademicManagementService(IAccessCodeService accessCodeService)
    {
        _accessCodeService = accessCodeService;
    }

    // ── TURMAS ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ClassroomListItemViewModel>> GetProfessorClassroomsAsync(
        int professorId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetProfessorClassrooms";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        return await BuildClassroomListAsync(await GetSelectAsync(cmd, cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<ClassroomListItemViewModel>> GetAllClassroomsAsync(
        CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetAllClassrooms";
        return await BuildClassroomListAsync(await GetSelectAsync(cmd, cancellationToken), cancellationToken);
    }

    private async Task<IReadOnlyList<ClassroomListItemViewModel>> BuildClassroomListAsync(
        DataTable dt, CancellationToken ct)
    {
        var result = new List<ClassroomListItemViewModel>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var classroomId    = Convert.ToInt32(row["Id"]);
            var studentCount   = Convert.ToInt32(row["StudentCount"]);
            var totalChallenge = Convert.ToInt32(row["TotalChallenges"]);
            var activeChall    = Convert.ToInt32(row["ActiveChallenges"]);
            var totalSubs      = Convert.ToInt32(row["TotalSubmissions"]);
            var reviewedSubs   = Convert.ToInt32(row["ReviewedSubmissions"]);
            var expectedSubs   = studentCount * totalChallenge;

            var students   = await GetClassroomStudentsAsync(classroomId, ct);
            var challenges = await GetClassroomChallengesAsync(classroomId, ct);

            result.Add(new ClassroomListItemViewModel
            {
                Id                              = classroomId,
                Name                            = row["Name"].ToString()!,
                Description                     = row["Description"] is DBNull ? null : row["Description"].ToString(),
                AccessCode                      = row["AccessCode"].ToString()!,
                IsActive                        = Convert.ToBoolean(row["IsActive"]),
                StudentCount                    = studentCount,
                ClassroomStudentCount           = studentCount,
                DirectChallengeParticipantCount = 0,
                ActiveChallengesCount           = activeChall,
                TotalChallengesCount            = totalChallenge,
                ExpectedSubmissionsCount        = expectedSubs,
                SubmittedChallengesCount        = totalSubs,
                ReviewedChallengesCount         = reviewedSubs,
                CompletionRatePercent           = CalcRate(totalSubs, expectedSubs),
                CompletionRateLabel             = FormatRate(totalSubs, expectedSubs),
                AverageGrade                    = CalcAvgGrade(students),
                Students                        = students,
                Challenges                      = challenges
            });
        }
        return result;
    }

    private async Task<IReadOnlyList<ClassroomStudentListItemViewModel>> GetClassroomStudentsAsync(
        int classroomId, CancellationToken ct)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetClassroomMembers";
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = classroomId;
        DataTable dt = await GetSelectAsync(cmd, ct);

        var result = new List<ClassroomStudentListItemViewModel>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var total     = Convert.ToInt32(row["TotalChallenges"]);
            var submitted = Convert.ToInt32(row["Submitted"]);
            var reviewed  = Convert.ToInt32(row["Reviewed"]);
            var avg       = row["AverageGrade"] is DBNull ? 0m : Convert.ToDecimal(row["AverageGrade"]);

            result.Add(new ClassroomStudentListItemViewModel
            {
                StudentId                = Convert.ToInt32(row["StudentId"]),
                Name                     = row["Name"].ToString()!,
                Email                    = row["Email"].ToString()!,
                JoinedAtUtc              = Convert.ToDateTime(row["JoinedAtUtc"]),
                AssociationLabel         = "Turma",
                AvailableChallengesCount = total,
                SubmittedChallengesCount = submitted,
                ReviewedChallengesCount  = reviewed,
                MissingChallengesCount   = Math.Max(total - submitted, 0),
                CompletionRatePercent    = CalcRate(submitted, total),
                CompletionRateLabel      = FormatRate(submitted, total),
                AverageGrade             = reviewed > 0 ? FormatGrade(avg) : null,
                ChallengeSummary         = null
            });
        }
        return result;
    }

    private async Task<IReadOnlyList<ClassroomChallengeListItemViewModel>> GetClassroomChallengesAsync(
        int classroomId, CancellationToken ct)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetClassroomChallenges";
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = classroomId;
        DataTable dt = await GetSelectAsync(cmd, ct);

        var result = new List<ClassroomChallengeListItemViewModel>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var isExpired = Convert.ToBoolean(row["IsExpired"]);
            var members   = Convert.ToInt32(row["ClassroomMemberCount"]);
            var direct    = Convert.ToInt32(row["DirectEnrollCount"]);
            result.Add(new ClassroomChallengeListItemViewModel
            {
                Id                   = Convert.ToInt32(row["Id"]),
                Title                = row["Title"].ToString()!,
                AccessCode           = row["AccessCode"].ToString()!,
                Deadline             = row["Deadline"] is DBNull ? null : Convert.ToDateTime(row["Deadline"]),
                IsExpired            = isExpired,
                StatusLabel          = isExpired ? "Expirado" : "Ativo",
                AudienceCount        = members + direct,
                DirectParticipantCount = direct
            });
        }
        return result;
    }

    public async Task<IReadOnlyList<ClassroomOptionViewModel>> GetProfessorClassroomOptionsAsync(
        int professorId, bool onlyActive = true, CancellationToken cancellationToken = default)
        => await GetClassroomOptionsAsync(professorId, onlyActive, cancellationToken);

    public async Task<IReadOnlyList<ClassroomOptionViewModel>> GetAllClassroomOptionsAsync(
        bool onlyActive = true, CancellationToken cancellationToken = default)
        => await GetClassroomOptionsAsync(0, onlyActive, cancellationToken);

    private async Task<IReadOnlyList<ClassroomOptionViewModel>> GetClassroomOptionsAsync(
        int professorId, bool onlyActive, CancellationToken ct)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetClassroomOptions";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value   = professorId;
        cmd.Parameters.Add("p_only_active",  MySqlDbType.Byte).Value    = onlyActive ? 1 : 0;
        DataTable dt = await GetSelectAsync(cmd, ct);

        return dt.Rows.Cast<DataRow>()
            .Select(r => new ClassroomOptionViewModel { Id = Convert.ToInt32(r["Id"]), Name = r["Name"].ToString()! })
            .ToList();
    }

    // ── DESAFIOS ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ProfessorChallengeListItemViewModel>> GetProfessorChallengesAsync(
        int professorId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetProfessorChallenges";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        return MapChallengeList(await GetSelectAsync(cmd, cancellationToken));
    }

    public async Task<IReadOnlyList<ProfessorChallengeListItemViewModel>> GetAllChallengesAsync(
        CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetAllChallenges";
        return MapChallengeList(await GetSelectAsync(cmd, cancellationToken));
    }

    private static IReadOnlyList<ProfessorChallengeListItemViewModel> MapChallengeList(DataTable dt)
    {
        var today  = DateTime.UtcNow.Date;
        var result = new List<ProfessorChallengeListItemViewModel>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var classroomName  = row["ClassroomName"].ToString();
            var publishedAt    = row["PublishedAtUtc"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["PublishedAtUtc"]);
            var deadline       = row["Deadline"]       is DBNull ? (DateTime?)null : Convert.ToDateTime(row["Deadline"]);
            var isDraft        = !publishedAt.HasValue || !deadline.HasValue || string.IsNullOrWhiteSpace(classroomName);
            var isExpired      = deadline.HasValue && deadline.Value < today;

            result.Add(new ProfessorChallengeListItemViewModel
            {
                Id            = Convert.ToInt32(row["Id"]),
                Title         = row["Title"].ToString()!,
                Description   = row["Description"] is DBNull ? string.Empty : row["Description"].ToString()!,
                AccessCode    = row["AccessCode"].ToString()!,
                CreatedAtUtc  = Convert.ToDateTime(row["CreatedAtUtc"]),
                PublishedAtUtc = publishedAt,
                Deadline      = deadline,
                ClassroomName = string.IsNullOrWhiteSpace(classroomName) ? null : classroomName,
                AudienceCount = Convert.ToInt32(row["AudienceCount"]),
                CanAssign     = isDraft,
                StatusKey     = isDraft ? "draft" : isExpired ? "expired" : "published",
                StatusLabel   = isDraft ? "Rascunho" : isExpired ? "Expirado" : "Atribuído"
            });
        }
        return result;
    }

    public async Task<IReadOnlyList<ChallengeOptionViewModel>> GetProfessorChallengeOptionsAsync(
        int professorId, bool onlyDraft = true, CancellationToken cancellationToken = default)
        => await GetChallengeOptionsAsync(professorId, onlyDraft, cancellationToken);

    public async Task<IReadOnlyList<ChallengeOptionViewModel>> GetAllChallengeOptionsAsync(
        bool onlyDraft = true, CancellationToken cancellationToken = default)
        => await GetChallengeOptionsAsync(0, onlyDraft, cancellationToken);

    private async Task<IReadOnlyList<ChallengeOptionViewModel>> GetChallengeOptionsAsync(
        int professorId, bool onlyDraft, CancellationToken ct)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetChallengeOptions";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        cmd.Parameters.Add("p_only_draft",   MySqlDbType.Byte).Value  = onlyDraft ? 1 : 0;
        DataTable dt = await GetSelectAsync(cmd, ct);

        return dt.Rows.Cast<DataRow>()
            .Select(r => new ChallengeOptionViewModel
            {
                Id = Convert.ToInt32(r["Id"]),
                Title = r["Title"].ToString()!,
                AccessCode = r["AccessCode"].ToString()!
            })
            .ToList();
    }

    public async Task<CreateChallengeViewModel?> GetChallengeForEditAsync(
        int professorId, int challengeId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetChallengeForEdit";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        cmd.Parameters.Add("p_challenge_id", MySqlDbType.Int32).Value = challengeId;
        DataTable dt = await GetSelectAsync(cmd, cancellationToken);
        if (dt.Rows.Count == 0) return null;

        DataRow row = dt.Rows[0];
        return new CreateChallengeViewModel
        {
            Id                   = Convert.ToInt32(row["Id"]),
            Title                = row["Title"].ToString()!,
            Description          = row["Description"] is DBNull ? "" : row["Description"].ToString()!,
            GeneratedCodePreview = row["AccessCode"].ToString()!
        };
    }

    // ── CRIAR / EDITAR ───────────────────────────────────────────────────────

    public async Task<(int Id, string Name, string AccessCode)> CreateClassroomAsync(
        int professorId, CreateClassViewModel model, CancellationToken cancellationToken = default)
    {
        var code = await _accessCodeService.GenerateUniqueCodeAsync(cancellationToken);

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_CreateClassroom";
        cmd.Parameters.Add("p_owner_id",    MySqlDbType.Int32).Value   = professorId;
        cmd.Parameters.Add("p_name",        MySqlDbType.VarChar, 120).Value = model.ClassName.Trim();
        cmd.Parameters.Add("p_description", MySqlDbType.Text).Value    = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim();
        cmd.Parameters.Add("p_access_code", MySqlDbType.VarChar, 16).Value  = code;

        MySqlParameter pId     = new MySqlParameter("p_id",       MySqlDbType.Int32)         { Direction = ParameterDirection.Output };
        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)          { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500)  { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pId); cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        return (Convert.ToInt32(pId.Value), model.ClassName.Trim(), code);
    }

    public async Task<(int Id, string Title, string AccessCode)> CreateChallengeAsync(
        int professorId, CreateChallengeViewModel model, CancellationToken cancellationToken = default)
    {
        var code = await _accessCodeService.GenerateUniqueCodeAsync(cancellationToken);

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_CreateChallenge";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value          = professorId;
        cmd.Parameters.Add("p_title",        MySqlDbType.VarChar, 160).Value   = model.Title.Trim();
        cmd.Parameters.Add("p_description",  MySqlDbType.Text).Value           = model.Description.Trim();
        cmd.Parameters.Add("p_access_code",  MySqlDbType.VarChar, 16).Value    = code;

        MySqlParameter pId     = new MySqlParameter("p_id",       MySqlDbType.Int32)         { Direction = ParameterDirection.Output };
        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)          { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500)  { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pId); cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        return (Convert.ToInt32(pId.Value), model.Title.Trim(), code);
    }

    public async Task<bool> UpdateChallengeAsync(
        int professorId, int challengeId, CreateChallengeViewModel model, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_UpdateChallenge";
        cmd.Parameters.Add("p_challenge_id", MySqlDbType.Int32).Value         = challengeId;
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value         = professorId;
        cmd.Parameters.Add("p_title",        MySqlDbType.VarChar, 160).Value  = model.Title.Trim();
        cmd.Parameters.Add("p_description",  MySqlDbType.Text).Value          = model.Description.Trim();

        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        return Convert.ToInt32(pHaErro.Value) == 0;
    }

    public async Task<(int Id, string Title, string ClassroomName, string AccessCode)> AssignChallengeAsync(
        int professorId, AssignChallengeViewModel model, CancellationToken cancellationToken = default)
    {
        var newCode = await _accessCodeService.GenerateUniqueCodeAsync(cancellationToken);

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_AssignChallenge";
        cmd.Parameters.Add("p_challenge_id", MySqlDbType.Int32).Value    = model.ChallengeId;
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value    = professorId;
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value    = model.ClassId;
        cmd.Parameters.Add("p_deadline",     MySqlDbType.DateTime).Value = model.Deadline!.Value.Date;
        cmd.Parameters.Add("p_access_code",  MySqlDbType.VarChar, 16).Value = newCode;

        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);

        if (Convert.ToInt32(pHaErro.Value) != 0)
            throw new InvalidOperationException(pMsg.Value?.ToString() ?? "Erro ao atribuir desafio.");

        // Busca nome do desafio e turma para o feedback
        var (challengeTitle, classroomName) = await GetAssignmentNamesAsync(model.ChallengeId!.Value, model.ClassId!.Value, cancellationToken);
        return (model.ChallengeId.Value, challengeTitle, classroomName, newCode);
    }

    private async Task<(string Title, string ClassroomName)> GetAssignmentNamesAsync(
        int challengeId, int classroomId, CancellationToken ct)
    {
        await using var conn = new MySqlConnection(DriverDB.ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT c.Title, cl.Name AS ClassroomName
            FROM Challenges c
            LEFT JOIN Classrooms cl ON cl.Id = @cid
            WHERE c.Id = @id LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@id",  challengeId);
        cmd.Parameters.AddWithValue("@cid", classroomId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync()) return ("", "");
        return (reader.GetString(0), reader.IsDBNull(1) ? "" : reader.GetString(1));
    }

    // ── JOIN POR CÓDIGO ──────────────────────────────────────────────────────

    public async Task<JoinByAccessCodeResult> JoinByAccessCodeAsync(
        int studentId, string accessCode, CancellationToken cancellationToken = default)
    {
        var code = NormalizeAccessCode(accessCode);
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Indica um código de adesão válido.");

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_FindByAccessCode";
        cmd.Parameters.Add("p_code", MySqlDbType.VarChar, 16).Value = code;
        DataTable dt = await GetSelectAsync(cmd, cancellationToken);

        if (dt.Rows.Count == 0)
            throw new InvalidOperationException("O código de adesão não existe ou já não está disponível.");

        DataRow row        = dt.Rows[0];
        var targetType     = row["TargetType"].ToString();
        var targetName     = row["TargetName"].ToString()!;
        var classroomName  = row["ClassroomName"] is DBNull ? null : row["ClassroomName"].ToString();
        var targetId       = Convert.ToInt32(row["Id"]);

        if (targetType == "Classroom")
        {
            MySqlCommand joinCmd = new MySqlCommand();
            joinCmd.CommandType = CommandType.StoredProcedure;
            joinCmd.CommandText = "sp_JoinClassroom";
            joinCmd.Parameters.Add("p_student_id",   MySqlDbType.Int32).Value = studentId;
            joinCmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = targetId;
            MySqlParameter pCreated = new MySqlParameter("p_created",  MySqlDbType.Byte) { Direction = ParameterDirection.Output };
            MySqlParameter pHaErro  = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte) { Direction = ParameterDirection.Output };
            MySqlParameter pMsg     = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            joinCmd.Parameters.Add(pCreated); joinCmd.Parameters.Add(pHaErro); joinCmd.Parameters.Add(pMsg);
            await ExecuteNonQueryAsync(joinCmd, cancellationToken);
            bool created = Convert.ToInt32(pCreated.Value) == 1;
            return new JoinByAccessCodeResult(AccessCodeTargetType.Classroom, targetName, created, targetName);
        }
        else
        {
            MySqlParameter pCreated = new MySqlParameter("p_created",  MySqlDbType.Byte) { Direction = ParameterDirection.Output };
            MySqlParameter pHaErro  = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte) { Direction = ParameterDirection.Output };
            MySqlParameter pMsg     = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            MySqlCommand joinCmd = new MySqlCommand();
            joinCmd.CommandType = CommandType.StoredProcedure;
            joinCmd.CommandText = "sp_JoinChallenge";
            joinCmd.Parameters.Add("p_student_id",   MySqlDbType.Int32).Value = studentId;
            joinCmd.Parameters.Add("p_challenge_id", MySqlDbType.Int32).Value = targetId;
            joinCmd.Parameters.Add(pCreated); joinCmd.Parameters.Add(pHaErro); joinCmd.Parameters.Add(pMsg);
            await ExecuteNonQueryAsync(joinCmd, cancellationToken);
            bool created = Convert.ToInt32(pCreated.Value) == 1;
            return new JoinByAccessCodeResult(AccessCodeTargetType.Challenge, targetName, created, classroomName);
        }
    }

    // ── DESAFIOS DO ALUNO ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<StudentChallengeListItemViewModel>> GetStudentChallengesAsync(
        int studentId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetStudentChallenges";
        cmd.Parameters.Add("p_student_id", MySqlDbType.Int32).Value = studentId;
        DataTable dt  = await GetSelectAsync(cmd, cancellationToken);
        var today     = DateTime.UtcNow.Date;
        var result    = new List<StudentChallengeListItemViewModel>(dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            var deadline    = row["Deadline"] is DBNull ? DateTime.MaxValue : Convert.ToDateTime(row["Deadline"]);
            var hasSubmit   = Convert.ToInt32(row["HasSubmitted"]) > 0;
            var grade       = row["Grade"] is DBNull ? (decimal?)null : Convert.ToDecimal(row["Grade"]);
            var isGraded    = grade.HasValue;
            var submittedAt = row["SubmittedAtUtc"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["SubmittedAtUtc"]);
            var status      = ResolveStudentChallengeStatus(deadline, hasSubmit, isGraded, today);

            result.Add(new StudentChallengeListItemViewModel
            {
                Id               = Convert.ToInt32(row["Id"]),
                Title            = row["Title"].ToString()!,
                Description      = row["Description"] is DBNull ? "" : row["Description"].ToString()!,
                Professor        = row["ProfessorName"].ToString()!,
                ClassroomName    = row["ClassroomName"].ToString()!,
                Deadline         = deadline == DateTime.MaxValue ? DateTime.MinValue : deadline,
                StatusKey        = status.Key,
                StatusLabel      = status.Label,
                Grade            = isGraded ? FormatGrade(grade!.Value) : null,
                HasSubmission    = hasSubmit,
                CanEditSubmission = !isGraded && deadline >= today,
                SubmittedAtUtc   = submittedAt
            });
        }
        return result;
    }

    public async Task<StudentChallengeDetailsViewModel?> GetStudentChallengeDetailsAsync(
        int studentId, int challengeId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetStudentChallengeDetails";
        cmd.Parameters.Add("p_student_id",   MySqlDbType.Int32).Value = studentId;
        cmd.Parameters.Add("p_challenge_id", MySqlDbType.Int32).Value = challengeId;
        DataTable dt = await GetSelectAsync(cmd, cancellationToken);
        if (dt.Rows.Count == 0) return null;

        DataRow row     = dt.Rows[0];
        var today       = DateTime.UtcNow.Date;
        var deadline    = row["Deadline"] is DBNull ? DateTime.MaxValue : Convert.ToDateTime(row["Deadline"]);
        var hasSubmit   = row["SubmissionId"] is not DBNull;
        var grade       = row["Grade"] is DBNull ? (decimal?)null : Convert.ToDecimal(row["Grade"]);
        var isGraded    = grade.HasValue;
        var status      = ResolveStudentChallengeStatus(deadline, hasSubmit, isGraded, today);

        return new StudentChallengeDetailsViewModel
        {
            Id               = Convert.ToInt32(row["Id"]),
            Title            = row["Title"].ToString()!,
            Description      = row["Description"] is DBNull ? "" : row["Description"].ToString()!,
            Professor        = row["ProfessorName"].ToString()!,
            ClassroomName    = row["ClassroomName"].ToString()!,
            Deadline         = deadline == DateTime.MaxValue ? DateTime.MinValue : deadline,
            StatusKey        = status.Key,
            StatusLabel      = status.Label,
            GradeDisplay     = isGraded ? FormatGrade(grade!.Value) : null,
            Feedback         = row["Feedback"] is DBNull ? null : row["Feedback"].ToString(),
            SubmittedAtUtc   = row["SubmittedAtUtc"]  is DBNull ? null : Convert.ToDateTime(row["SubmittedAtUtc"]),
            UpdatedAtUtc     = row["UpdatedAtUtc"]    is DBNull ? null : Convert.ToDateTime(row["UpdatedAtUtc"]),
            HasSubmission    = hasSubmit,
            CanEditSubmission = !isGraded && deadline >= today,
            ResponseText     = row["ResponseText"] is DBNull ? string.Empty : row["ResponseText"].ToString()!
        };
    }

    public async Task<bool> SaveStudentChallengeSubmissionAsync(
        int studentId, int challengeId, string responseText, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_SaveStudentSubmission";
        cmd.Parameters.Add("p_student_id",    MySqlDbType.Int32).Value  = studentId;
        cmd.Parameters.Add("p_challenge_id",  MySqlDbType.Int32).Value  = challengeId;
        cmd.Parameters.Add("p_response_text", MySqlDbType.MediumText).Value = responseText.Trim();

        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        var errorCode = Convert.ToInt32(pHaErro.Value);
        if (errorCode == 0) return true;
        if (errorCode == 1) return false;  // não inscrito
        throw new InvalidOperationException(pMsg.Value?.ToString() ?? "Erro ao guardar submissão.");
    }

    // ── SUBMISSÕES ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SubmissionListItemViewModel>> GetProfessorSubmissionsAsync(
        int professorId, int? challengeId = null, int? classroomId = null,
        CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetProfessorSubmissions";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        cmd.Parameters.Add("p_challenge_id", MySqlDbType.Int32).Value = challengeId ?? 0;
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = classroomId ?? 0;
        return MapSubmissionList(await GetSelectAsync(cmd, cancellationToken));
    }

    public async Task<IReadOnlyList<SubmissionListItemViewModel>> GetAllSubmissionsAsync(
        int? challengeId = null, int? classroomId = null,
        CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetAllSubmissions";
        cmd.Parameters.Add("p_challenge_id", MySqlDbType.Int32).Value = challengeId ?? 0;
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = classroomId ?? 0;
        return MapSubmissionList(await GetSelectAsync(cmd, cancellationToken));
    }

    private static IReadOnlyList<SubmissionListItemViewModel> MapSubmissionList(DataTable dt)
    {
        var result = new List<SubmissionListItemViewModel>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var submittedAt = Convert.ToDateTime(row["SubmittedAtUtc"]);
            var updatedAt   = row["UpdatedAtUtc"] is DBNull ? submittedAt : Convert.ToDateTime(row["UpdatedAtUtc"]);
            var grade       = row["Grade"] is DBNull ? (decimal?)null : Convert.ToDecimal(row["Grade"]);
            result.Add(new SubmissionListItemViewModel
            {
                Id             = Convert.ToInt32(row["Id"]),
                ChallengeId    = Convert.ToInt32(row["ChallengeId"]),
                ChallengeTitle = row["ChallengeTitle"].ToString()!,
                ClassroomName  = row["ClassroomName"].ToString()!,
                StudentId      = Convert.ToInt32(row["StudentId"]),
                StudentName    = row["StudentName"].ToString()!,
                SubmittedAtUtc = submittedAt,
                UpdatedAtUtc   = updatedAt != submittedAt ? updatedAt : null,
                IsGraded       = grade.HasValue,
                GradeDisplay   = grade.HasValue ? FormatGrade(grade.Value) : null
            });
        }
        return result;
    }

    public async Task<ReviewSubmissionViewModel?> GetSubmissionForReviewAsync(
        int professorId, int submissionId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetSubmissionForReview";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        cmd.Parameters.Add("p_submission_id",MySqlDbType.Int32).Value = submissionId;
        DataTable dt = await GetSelectAsync(cmd, cancellationToken);
        if (dt.Rows.Count == 0) return null;

        DataRow row     = dt.Rows[0];
        var submittedAt = Convert.ToDateTime(row["SubmittedAtUtc"]);
        var updatedAt   = row["UpdatedAtUtc"] is DBNull ? submittedAt : Convert.ToDateTime(row["UpdatedAtUtc"]);
        var grade       = row["Grade"] is DBNull ? (decimal?)null : Convert.ToDecimal(row["Grade"]);

        return new ReviewSubmissionViewModel
        {
            SubmissionId     = Convert.ToInt32(row["Id"]),
            StudentName      = row["StudentName"].ToString()!,
            ChallengeTitle   = row["ChallengeTitle"].ToString()!,
            ClassroomName    = row["ClassroomName"].ToString()!,
            ResponseText     = row["ResponseText"] is DBNull ? "" : row["ResponseText"].ToString()!,
            SubmittedAtUtc   = submittedAt,
            UpdatedAtUtc     = updatedAt != submittedAt ? updatedAt : null,
            IsAlreadyGraded  = grade.HasValue,
            Grade            = grade,
            Feedback         = row["Feedback"] is DBNull ? null : row["Feedback"].ToString()
        };
    }

    public async Task<bool> ReviewSubmissionAsync(
        int professorId, int submissionId, decimal grade, string? feedback,
        CancellationToken cancellationToken = default)
    {
        var roundedGrade = Math.Clamp(Math.Round(grade * 2, MidpointRounding.AwayFromZero) / 2, 0, 20);

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_ReviewSubmission";
        cmd.Parameters.Add("p_professor_id",  MySqlDbType.Int32).Value      = professorId;
        cmd.Parameters.Add("p_submission_id", MySqlDbType.Int32).Value      = submissionId;
        cmd.Parameters.Add("p_grade",         MySqlDbType.Decimal).Value    = roundedGrade;
        cmd.Parameters.Add("p_feedback",      MySqlDbType.Text).Value       = feedback?.Trim() ?? "";

        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        return Convert.ToInt32(pHaErro.Value) == 0;
    }

    public async Task<bool> RemoveStudentFromClassroomAsync(
        int professorId, int classroomId, int studentId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_RemoveStudentFromClassroom";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = classroomId;
        cmd.Parameters.Add("p_student_id",   MySqlDbType.Int32).Value = studentId;

        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        return Convert.ToInt32(pHaErro.Value) == 0;
    }

    // ── ALUNOS / PROGRESSO ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<ProfessorStudentListItemViewModel>> GetProfessorStudentsAsync(
        int professorId, int? classroomId = null, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetProfessorStudents";
        cmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = classroomId ?? 0;
        return MapStudentList(await GetSelectAsync(cmd, cancellationToken));
    }

    public async Task<IReadOnlyList<ProfessorStudentListItemViewModel>> GetAllStudentsAsync(
        int? classroomId = null, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetAllStudents";
        cmd.Parameters.Add("p_classroom_id", MySqlDbType.Int32).Value = classroomId ?? 0;
        return MapStudentList(await GetSelectAsync(cmd, cancellationToken));
    }

    private static IReadOnlyList<ProfessorStudentListItemViewModel> MapStudentList(DataTable dt)
    {
        var result = new List<ProfessorStudentListItemViewModel>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var total     = Convert.ToInt32(row["TotalChallenges"]);
            var submitted = Convert.ToInt32(row["Submitted"]);
            var reviewed  = Convert.ToInt32(row["Reviewed"]);
            var avg       = row["AverageGrade"] is DBNull ? 0m : Convert.ToDecimal(row["AverageGrade"]);
            var missing   = Math.Max(total - submitted, 0);

            result.Add(new ProfessorStudentListItemViewModel
            {
                StudentId                = Convert.ToInt32(row["StudentId"]),
                Name                     = row["Name"].ToString()!,
                Email                    = row["Email"].ToString()!,
                ClassroomName            = row["ClassroomName"].ToString()!,
                JoinedAtUtc              = Convert.ToDateTime(row["JoinedAtUtc"]),
                AssociationLabel         = "Turma",
                AvailableChallengesCount = total,
                ActiveChallengesCount    = 0,
                SubmittedChallengesCount = submitted,
                ReviewedChallengesCount  = reviewed,
                MissingChallengesCount   = missing,
                CompletionRatePercent    = CalcRate(submitted, total),
                CompletionRateLabel      = FormatRate(submitted, total),
                AverageGrade             = reviewed > 0 ? FormatGrade(avg) : null,
                ChallengeSummary         = $"Acesso aos {total} desafios da turma."
            });
        }
        return result;
    }

    // ── DASHBOARD DO PROFESSOR ───────────────────────────────────────────────

    public async Task<ProfessorDashboardViewModel> GetProfessorDashboardAsync(
        int professorId, CancellationToken cancellationToken = default)
    {
        // Counts
        MySqlCommand countCmd = new MySqlCommand();
        countCmd.CommandType = CommandType.StoredProcedure;
        countCmd.CommandText = "sp_GetProfessorDashboard";
        countCmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        DataTable countDt = await GetSelectAsync(countCmd, cancellationToken);

        int totalClasses = 0, totalStudents = 0, activeChallenges = 0, pendingSubs = 0;
        if (countDt.Rows.Count > 0)
        {
            DataRow r  = countDt.Rows[0];
            totalClasses    = Convert.ToInt32(r["ActiveClassrooms"]);
            activeChallenges = Convert.ToInt32(r["ActiveChallenges"]);
            pendingSubs     = Convert.ToInt32(r["PendingReviews"]);
            totalStudents   = Convert.ToInt32(r["TotalStudents"]);
        }

        // Top 5 classrooms
        MySqlCommand classCmd = new MySqlCommand();
        classCmd.CommandType = CommandType.StoredProcedure;
        classCmd.CommandText = "sp_GetProfessorClassrooms";
        classCmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        DataTable classDt = await GetSelectAsync(classCmd, cancellationToken);

        var classes = classDt.Rows.Cast<DataRow>().Take(5).Select(r => new DashboardClassViewModel
        {
            Id                  = Convert.ToInt32(r["Id"]),
            Name                = r["Name"].ToString()!,
            AccessCode          = r["AccessCode"].ToString()!,
            StudentCount        = Convert.ToInt32(r["StudentCount"]),
            ActiveChallengesCount = Convert.ToInt32(r["ActiveChallenges"])
        }).ToList();

        // Recent submissions
        MySqlCommand subCmd = new MySqlCommand();
        subCmd.CommandType = CommandType.StoredProcedure;
        subCmd.CommandText = "sp_GetRecentProfessorSubmissions";
        subCmd.Parameters.Add("p_professor_id", MySqlDbType.Int32).Value = professorId;
        subCmd.Parameters.Add("p_limit",        MySqlDbType.Int32).Value = 5;
        DataTable subDt = await GetSelectAsync(subCmd, cancellationToken);

        var recentSubs = subDt.Rows.Cast<DataRow>().Select(r => new DashboardSubmissionViewModel
        {
            StudentName    = r["StudentName"].ToString()!,
            ChallengeTitle = r["ChallengeTitle"].ToString()!,
            ClassroomName  = r["ClassroomName"] is DBNull ? null : r["ClassroomName"].ToString(),
            SubmittedAtUtc = Convert.ToDateTime(r["SubmittedAtUtc"]),
            IsGraded       = r["Grade"] is not DBNull
        }).ToList();

        return new ProfessorDashboardViewModel
        {
            TotalClasses            = totalClasses,
            TotalStudents           = totalStudents,
            ActiveChallengesCount   = activeChallenges,
            PendingSubmissionsCount = pendingSubs,
            Classes                 = classes,
            RecentSubmissions       = recentSubs
        };
    }

    // ── Utilitários ──────────────────────────────────────────────────────────

    private static string NormalizeAccessCode(string accessCode) =>
        new string((accessCode ?? string.Empty)
            .Where(c => !char.IsWhiteSpace(c)).ToArray())
            .Trim().ToUpperInvariant();

    private static (string Key, string Label) ResolveStudentChallengeStatus(
        DateTime deadline, bool hasSubmission, bool isGraded, DateTime today)
    {
        if (isGraded)     return ("graded",    "Avaliado");
        if (hasSubmission) return ("submitted", "Submetido");
        return deadline < today ? ("expired", "Expirado") : ("pending", "Pendente");
    }

    private static string FormatGrade(decimal grade)
    {
        return $"{grade.ToString("0.#", CultureInfo.GetCultureInfo("pt-PT"))}/20";
    }

    private static decimal CalcRate(int completed, int total)
    {
        if (total <= 0) return 0m;
        return Math.Round((decimal)completed * 100m / total, 1, MidpointRounding.AwayFromZero);
    }

    private static string FormatRate(int completed, int total)
    {
        return $"{CalcRate(completed, total).ToString("0.#", CultureInfo.GetCultureInfo("pt-PT"))}%";
    }

    private static string? CalcAvgGrade(IReadOnlyList<ClassroomStudentListItemViewModel> students)
    {
        var reviewed = students.Where(s => s.AverageGrade != null).ToList();
        if (reviewed.Count == 0) return null;
        var gradeValues = reviewed
            .Select(s => decimal.TryParse(s.AverageGrade!.Split('/')[0],
                System.Globalization.NumberStyles.Any,
                CultureInfo.GetCultureInfo("pt-PT"), out var v) ? v : 0m)
            .ToList();
        return FormatGrade(gradeValues.Average());
    }
}
