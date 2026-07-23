using System.Security.Claims;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase {
    private readonly IAuthServices _authServices;
    private readonly IProfileService _profileService;
    private readonly IUserSignInService _userSignInService;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IAntiforgery _antiforgery;

    public AuthController(IAuthServices authServices, IProfileService profileService, IUserSignInService userSignInService, IAuthenticationSchemeProvider schemes, IAntiforgery antiforgery) {
        _authServices = authServices;
        _profileService = profileService;
        _userSignInService = userSignInService;
        _schemes = schemes;
        _antiforgery = antiforgery;
    }

    [HttpGet("antiforgery")]
    public IActionResult Antiforgery() {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken token) {
        var user = await _authServices.AuthenticateAsync(request.Email, request.Password, token);

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        await _userSignInService.SignInAsync(user);
        return Ok(await _profileService.GetCurrentUserInfoAsync(user.UserId, token));
    }

    [HttpPost("register/candidate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterCandidate(RegisterCandidateRequest request, CancellationToken token) {
        var user = await _authServices.RegisterCandidateAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            token);

        await _userSignInService.SignInAsync(user);
        return Ok(await _profileService.GetCurrentUserInfoAsync(user.UserId, token));
    }

    [HttpPost("register/recruiter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterRecruiter(RegisterRecruiterRequest request, CancellationToken token) {
        var user = await _authServices.RegisterRecruiterAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.Company,
            token);

        await _userSignInService.SignInAsync(user);
        return Ok(await _profileService.GetCurrentUserInfoAsync(user.UserId, token));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken token) {
        var user = await _profileService.GetCurrentUserInfoAsync(
            User.GetCurrentUserId(),
            token);

        return user is null ? Unauthorized() : Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() {
        await _userSignInService.SignOutAsync();
        return NoContent();
    }

    [HttpGet("external/{provider}")]
    public async Task<IActionResult> ExternalLogin(string provider) {
        if (provider is not ("Google" or "Facebook") ||
            await _schemes.GetSchemeAsync(provider) is null)
        {
            return BadRequest(new { message = $"{provider} authentication is not configured." });
        }

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(ExternalCallback), new { provider })
        }, provider);
    }

    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalCallback(string provider, CancellationToken token) {
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded || result.Principal is null)
        {
            return Unauthorized(new { message = "External authentication failed." });
        }

        var subject = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var displayName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "The external provider did not return an email." });
        }

        var user = await _authServices.FindOrCreateExternalAsync(
            provider,
            subject,
            email,
            displayName ?? "Candidate",
            token);

        await HttpContext.SignOutAsync("External");
        await _userSignInService.SignInAsync(user);
        return Redirect("/");
    }
}
