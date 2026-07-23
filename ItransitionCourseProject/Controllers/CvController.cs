using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[ApiController]
[Authorize]
[Route("api/cvs")]
public class CvController : ControllerBase {
    private readonly ICvService _cvService;

    public CvController(ICvService cvService)
    {
        _cvService = cvService;
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken token)
    {
        return Ok(await _cvService.GetCandidateCvsAsync(User.GetCurrentUserId(), token));
    }

    [Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
    [HttpGet("published")]
    public async Task<IActionResult> Published(string? search, Guid? positionId, string? technologyTag, CancellationToken token) {
        return Ok(await _cvService.GetPublishedCvsAsync(search, positionId, technologyTag, token));
    }

    [HttpGet("{cvId:guid}")]
    public async Task<IActionResult> GetCv(Guid cvId, CancellationToken token) {
        var currentUserId = User.GetCurrentUserId();
        var recruiterId = User.IsInRole(nameof(UserRole.Recruiter)) || User.IsInRole(nameof(UserRole.Admin)) ? currentUserId : (Guid?)null;

        var cv = await _cvService.GetCvAsync(cvId, recruiterId, token);
        if (cv is null)
        {
            return NotFound();
        }

        if (User.IsInRole(nameof(UserRole.Candidate)))
        {
            if (cv.CandidateUserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(cv);
        }

        if (cv.Status != CvStatus.Published)
        {
            return NotFound();
        }
        return Ok(cv);
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPost("for-position/{positionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCv(Guid positionId, CancellationToken token) {
        return Ok(await _cvService.CreateCvAsync(User.GetCurrentUserId(), positionId, token));
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPut("{cvId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCv(Guid cvId, SaveCvRequest request, CancellationToken token) {
        var versions = await _cvService.SaveCvAsync(User.GetCurrentUserId(), cvId, request, token);
        var savedCv = await _cvService.GetCvAsync(cvId, token: token) ?? throw new KeyNotFoundException("CV not found.");
        return Ok(new { versions, version = savedCv.Version});
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPost("{cvId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishCv(Guid cvId, CancellationToken token) {
        var result = await _cvService.PublishCvAsync(User.GetCurrentUserId(), cvId, token);
        return result.Published ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
    [HttpPost("{cvId:guid}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(Guid cvId, CancellationToken token) {
        var count = await _cvService.ToggleLikeAsync(User.GetCurrentUserId(), cvId, token);
        return Ok(new { likeCount = count });
    }
}
