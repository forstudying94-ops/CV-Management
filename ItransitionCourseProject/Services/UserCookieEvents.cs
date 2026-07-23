using System.Security.Claims;
using ItransitionCourseProject.DataBase;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public class UserCookieEvents(DatabaseContext db) : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        return RedirectOrSetStatus(context, StatusCodes.Status401Unauthorized);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        return RedirectOrSetStatus(context, StatusCodes.Status403Forbidden);
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdText, out var userId))
        {
            context.RejectPrincipal();
            return;
        }

        var user = await db.Users
            .AsNoTracking()
            .Where(savedUser => savedUser.UserId == userId)
            .Select(savedUser => new { savedUser.Role, savedUser.IsBlocked })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (user is not null && !user.IsBlocked && context.Principal!.IsInRole(user.Role.ToString()))
        {
            return;
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static Task RedirectOrSetStatus(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = statusCode;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
