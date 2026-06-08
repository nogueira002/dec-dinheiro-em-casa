using dotnet_mvc.Extensions;
using dotnet_mvc.Security;
using dotnet_mvc.Services.Admin;
using dotnet_mvc.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_mvc.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class AdminController : AuthenticatedControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly IMonitoringService _monitoringService;

    public AdminController(IAdminUserService adminUserService, IMonitoringService monitoringService)
    {
        _adminUserService = adminUserService;
        _monitoringService = monitoringService;
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Dashboard de administração", "dashboard", UserRoles.Admin);
        var model = await _adminUserService.GetAdminDashboardAsync(cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Users(string? search, string? role, string? status, CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Utilizadores", "users", UserRoles.Admin);

        var allUsers = await _adminUserService.GetUsersAsync(search, role, status, cancellationToken);

        var model = new UsersIndexViewModel
        {
            Users = allUsers,
            Search = search,
            RoleFilter = role,
            StatusFilter = status,
            TotalCount = allUsers.Count,
            ActiveCount = allUsers.Count(u => u.IsActive),
            StudentCount = allUsers.Count(u => u.Role == UserRoles.Student),
            ProfessorCount = allUsers.Count(u => u.Role == UserRoles.Professor)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUserStatus(int id, CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var (success, isNowActive, userName) = await _adminUserService.ToggleUserStatusAsync(id, adminId.Value, cancellationToken);

        if (!success)
        {
            TempData["StatusType"] = "error";
            TempData["StatusMessage"] = id == adminId.Value
                ? "Não podes desativar a tua própria conta."
                : "Utilizador não encontrado.";
        }
        else
        {
            TempData["StatusType"] = "success";
            TempData["StatusMessage"] = isNowActive
                ? $"{userName} foi ativado com sucesso."
                : $"{userName} foi desativado. A sessão foi terminada.";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> EditUserRole(int id, string role, CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var (success, userName, newRole) = await _adminUserService.ChangeUserRoleAsync(id, role, adminId.Value, cancellationToken);

        if (!success)
        {
            TempData["StatusType"] = "error";
            TempData["StatusMessage"] = id == adminId.Value
                ? "Não podes alterar o teu próprio perfil."
                : "Não foi possível alterar o perfil do utilizador.";
        }
        else
        {
            var roleLabel = newRole switch
            {
                UserRoles.Professor => "Professor",
                UserRoles.Admin => "Administrador",
                _ => "Aluno"
            };
            TempData["StatusType"] = "success";
            TempData["StatusMessage"] = $"O perfil de {userName} foi alterado para {roleLabel}. A sessão do utilizador foi terminada.";
        }

        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Monitoring(CancellationToken cancellationToken)
    {
        ApplyDashboardContext("Monitorização", "monitoring", UserRoles.Admin);
        var model = await _monitoringService.GetStatusAsync(cancellationToken);
        return View(model);
    }
}
