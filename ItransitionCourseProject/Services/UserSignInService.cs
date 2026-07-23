using System.Security.Claims;
using ItransitionCourseProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ItransitionCourseProject.Services;

public interface IUserSignInService {
    Task SignInAsync(User user);
    Task SignOutAsync();
}

public sealed class UserSignInService : IUserSignInService {
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSignInService(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task SignInAsync(User user) {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await GetHttpContext().SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });
    }

    public Task SignOutAsync() {
        return GetHttpContext().SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private HttpContext GetHttpContext() {
        return _httpContextAccessor.HttpContext ??
               throw new InvalidOperationException("The current HTTP context is not available.");
    }
}
