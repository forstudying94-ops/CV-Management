using System.Security.Claims;

namespace ItransitionCourseProject.Controllers;

public static class ControllerExtensions {
    public static Guid GetCurrentUserId(this ClaimsPrincipal principal) {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : throw new UnauthorizedAccessException("Authenticated user id was not found.");
    }
}
