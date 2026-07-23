using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase {
    private readonly IProfileService _profileService;
    private readonly IAttributeProfileService _attributeProfileService;

    public ProfileController(IProfileService profileService, IAttributeProfileService attributeProfileService) {
        _profileService = profileService;
        _attributeProfileService = attributeProfileService;
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken token) {
        var profile = await _profileService.GetMeAsync(User.GetCurrentUserId(), token);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPut("me")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMe(UpdateProfileMeRequest request, CancellationToken token) {
        return Ok(await _profileService.SaveMeAsync(
            User.GetCurrentUserId(),
            request,
            token));
    }

    [HttpPut("preferences")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(UpdatePreferencesRequest request, CancellationToken token) {
        var version = await _profileService.SavePreferencesAsync(
            User.GetCurrentUserId(),
            request,
            token);

        return Ok(new VersionResponse { Version = version });
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPost("avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar, CancellationToken token) {
        var url = await _profileService.UploadProfilePictureAsync(User.GetCurrentUserId(), avatar, token);

        return Ok(new AvatarResponse { Url = url });
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("attributes")]
    public async Task<IActionResult> Attributes(CancellationToken token) {
        return Ok(await _attributeProfileService.GetProfileAttributesAsync(
            User.GetCurrentUserId(),
            token));
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("attributes/available")]
    public async Task<IActionResult> AvailableAttributes(int page = 1, string? prefix = null, string? category = null, bool recentFirst = false, CancellationToken token = default) {
        return Ok(await _attributeProfileService.GetAvailableAttributesAsync(
            User.GetCurrentUserId(),
            page,
            prefix,
            category,
            recentFirst,
            token));
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPost("attributes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttribute(AddProfileAttributeRequest request, CancellationToken token) {
        var added = await _attributeProfileService.AddAttributeAsync(
            User.GetCurrentUserId(),
            request.AttributeId,
            token);

        return added ? Ok() : Conflict(new { message = "Attribute is already in the profile." });
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPut("attributes/{attributeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttributeValue(Guid attributeId, SaveProfileAttributeValueRequest request, CancellationToken token) {
        var version = await _attributeProfileService.SaveAttributeValueAsync(
            User.GetCurrentUserId(),
            attributeId,
            request.Value,
            request.Version,
            token);

        return Ok(new VersionResponse { Version = version });
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpDelete("attributes/{attributeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttribute(Guid attributeId, CancellationToken token) {
        var deleted = await _attributeProfileService.DeleteAttributeAsync(
            User.GetCurrentUserId(),
            attributeId,
            token);

        return deleted ? NoContent() : NotFound();
    }
}
