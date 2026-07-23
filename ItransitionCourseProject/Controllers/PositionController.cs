using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[ApiController]
[Route("api/positions")]
public class PositionController : ControllerBase {
    private readonly IPositionService _positionService;
    private readonly IDiscussionService _discussionService;

    public PositionController(IPositionService positionService, IDiscussionService discussionService) {
        _positionService = positionService;
        _discussionService = discussionService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPositions(string? search, CancellationToken token) {
        return Ok(await _positionService.GetPositionsAsync(search, token));
    }

    [AllowAnonymous]
    [HttpGet("{positionId:guid}")]
    public async Task<IActionResult> GetPosition(Guid positionId, CancellationToken token) {
        var position = await _positionService.GetPositionAsync(positionId, token);
        if (position is null)
        {
            return NotFound();
        }

        return Ok(position);
    }

    [Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePosition(SavePositionRequest request, CancellationToken token) {
        request.PositionId = null;
        return Ok(await _positionService.SavePositionAsync(request, token));
    }

    [Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
    [HttpPut("{positionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePosition(Guid positionId, SavePositionRequest request, CancellationToken token) {
        request.PositionId = positionId;
        return Ok(await _positionService.SavePositionAsync(request, token));
    }

    [Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
    [HttpPost("{positionId:guid}/duplicate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DuplicatePosition(Guid positionId, CancellationToken token) {
        return Ok(await _positionService.DuplicatePositionAsync(positionId, token));
    }

    [Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
    [HttpDelete("{positionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePosition(Guid positionId, CancellationToken token) {
        await _positionService.DeletePositionAsync(positionId, token);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{positionId:guid}/discussions")]
    public async Task<IActionResult> GetDiscussions(Guid positionId, DateTime? after, CancellationToken token) {
        var position = await _positionService.GetPositionAsync(positionId, token);
        if (position is null)
        {
            return NotFound();
        }

        return Ok(await _discussionService.GetDiscussionsAsync(positionId, after, token));
    }

    [Authorize]
    [HttpPost("{positionId:guid}/discussions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDiscussion(Guid positionId, AddDiscussionRequest request, CancellationToken token) {
        return Ok(await _discussionService.AddDiscussionAsync(
            positionId,
            User.GetCurrentUserId(),
            request.ContentMarkdown,
            token));
    }
}
