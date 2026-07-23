using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/users")]
public class AdminController : ControllerBase {
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService) {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(int page = 1, CancellationToken token = default) {
        return Ok(await _adminService.GetUsersAsync(page, token));
    }

    [HttpPut("{userId:guid}/blocked")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeBlocked(Guid userId, ChangeBlockedRequest request, CancellationToken token) {
        var version = await _adminService.ChangeBlockedAsync(
            User.GetCurrentUserId(),
            userId,
            request,
            token);

        return Ok(new VersionResponse { Version = version });
    }

    [HttpPut("{userId:guid}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(Guid userId, ChangeRoleRequest request, CancellationToken token) {
        var currentAdminId = User.GetCurrentUserId();
        var version = await _adminService.ChangeRoleAsync(userId, request, token);

        if (currentAdminId == userId && request.Role != UserRole.Admin)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return Ok(new VersionResponse { Version = version });
    }

    [HttpDelete("{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken token) {
        await _adminService.DeleteUserAsync(
            User.GetCurrentUserId(),
            userId,
            token);

        return NoContent();
    }
}
