using dotnet_mvc.Data;
using dotnet_mvc.Security;
using dotnet_mvc.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Services.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken = default);

    Task<(bool Success, bool IsNowActive, string UserName)> ToggleUserStatusAsync(
        int userId,
        int adminId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string UserName, string NewRole)> ChangeUserRoleAsync(
        int userId,
        string newRole,
        int adminId,
        CancellationToken cancellationToken = default);

    Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}

public sealed class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _dbContext;

    public AdminUserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync(
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => EF.Functions.Like(u.Name, $"%{term}%")
                                  || EF.Functions.Like(u.Email, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(role) && UserRoles.IsSupported(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (status == "active")
        {
            query = query.Where(u => u.UserStatus == "Active");
        }
        else if (status == "inactive")
        {
            query = query.Where(u => u.UserStatus != "Active");
        }

        return await query
            .OrderBy(u => u.Name)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                RoleLabel = u.Role == UserRoles.Professor ? "Professor"
                           : u.Role == UserRoles.Admin    ? "Administrador"
                           :                               "Aluno",
                IsActive = u.UserStatus == "Active",
                LastLoginAtUtc = u.LastLoginAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, bool IsNowActive, string UserName)> ToggleUserStatusAsync(
        int userId,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        if (userId == adminId)
        {
            return (false, true, string.Empty);
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return (false, false, string.Empty);
        }

        var isNowActive = user.UserStatus != "Active";
        user.UserStatus = isNowActive ? "Active" : "Inactive";

        if (!isNowActive)
        {
            // invalidate session immediately on deactivation
            user.SessionTokenHash = null;
            user.SessionExpiresAtUtc = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, isNowActive, user.Name);
    }

    public async Task<(bool Success, string UserName, string NewRole)> ChangeUserRoleAsync(
        int userId,
        string newRole,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        if (!UserRoles.IsSupported(newRole) || userId == adminId)
        {
            return (false, string.Empty, string.Empty);
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return (false, string.Empty, string.Empty);
        }

        user.Role = UserRoles.Normalize(newRole);
        // invalidate session so the user re-autentica com o novo papel
        user.SessionTokenHash = null;
        user.SessionExpiresAtUtc = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, user.Name, user.Role);
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var userCounts = await _dbContext.Users
            .AsNoTracking()
            .GroupBy(u => new { u.Role, IsActive = u.UserStatus == "Active" })
            .Select(g => new { g.Key.Role, g.Key.IsActive, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalClassrooms = await _dbContext.Classrooms
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalChallenges = await _dbContext.Challenges
            .AsNoTracking()
            .Where(ch => ch.PublishedAtUtc != null)
            .CountAsync(cancellationToken);

        var totalSubmissions = await _dbContext.ChallengeSubmissions
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var pendingSubmissions = await _dbContext.ChallengeSubmissions
            .AsNoTracking()
            .Where(s => s.Grade == null)
            .CountAsync(cancellationToken);

        return new AdminDashboardViewModel
        {
            TotalUsers = userCounts.Sum(g => g.Count),
            StudentCount = userCounts.Where(g => g.Role == UserRoles.Student).Sum(g => g.Count),
            ProfessorCount = userCounts.Where(g => g.Role == UserRoles.Professor).Sum(g => g.Count),
            AdminCount = userCounts.Where(g => g.Role == UserRoles.Admin).Sum(g => g.Count),
            ActiveCount = userCounts.Where(g => g.IsActive).Sum(g => g.Count),
            TotalClassrooms = totalClassrooms,
            TotalChallenges = totalChallenges,
            TotalSubmissions = totalSubmissions,
            PendingSubmissions = pendingSubmissions
        };
    }
}
