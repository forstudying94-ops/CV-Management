using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
[Route("api/attributes")]
public class AttributeController : ControllerBase {
    private readonly IAttributeLibraryService _attributeLibraryService;

    public AttributeController(IAttributeLibraryService attributeLibraryService) {
        _attributeLibraryService = attributeLibraryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAttributes(int page = 1, string? prefix = null, string? category = null, CancellationToken token = default) {
        return Ok(await _attributeLibraryService.GetAttributesAsync(page, prefix, category, token));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken token) {
        return Ok(await _attributeLibraryService.GetCategoriesAsync(token));
    }

    [HttpGet("{attributeId:guid}")]
    public async Task<IActionResult> GetAttribute(Guid attributeId, CancellationToken token) {
        var attribute = await _attributeLibraryService.GetAttributeAsync(attributeId, token);
        return attribute is null ? NotFound() : Ok(attribute);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAttribute(SaveAttributeRequest request, CancellationToken token) {
        request.AttributeId = null;
        return Ok(await _attributeLibraryService.SaveAttributeAsync(request, token));
    }

    [HttpPut("{attributeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttribute(Guid attributeId, SaveAttributeRequest request, CancellationToken token) {
        request.AttributeId = attributeId;
        return Ok(await _attributeLibraryService.SaveAttributeAsync(request, token));
    }

    [HttpDelete("{attributeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttribute(Guid attributeId, CancellationToken token) {
        await _attributeLibraryService.DeleteAttributeAsync(attributeId, token);
        return NoContent();
    }
}
