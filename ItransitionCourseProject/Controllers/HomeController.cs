using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase {
    private readonly IHomeService _homeService;

    public HomeController(IHomeService homeService) {
        _homeService = homeService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token) {
        return Ok(await _homeService.GetStatisticsAsync(token));
    }
}
