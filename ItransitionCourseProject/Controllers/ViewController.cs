using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using ItransitionCourseProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItransitionCourseProject.Controllers;

[Route("")]
public class ViewController : Controller {
    private readonly IAuthServices _authServices;
    private readonly IHomeService _homeService;
    private readonly IPositionService _positionService;
    private readonly IProfileService _profileService;
    private readonly IAttributeProfileService _attributeProfileService;
    private readonly IAttributeLibraryService _attributeLibraryService;
    private readonly ICvService _cvService;
    private readonly IAdminService _adminService;
    private readonly IUserSignInService _userSignInService;

    public ViewController(IAuthServices authServices, IHomeService homeService, IPositionService positionService, IProfileService profileService, IAttributeProfileService attributeProfileService, IAttributeLibraryService attributeLibraryService, ICvService cvService, IAdminService adminService, IUserSignInService userSignInService) {
        _authServices = authServices;
        _homeService = homeService;
        _positionService = positionService;
        _profileService = profileService;
        _attributeProfileService = attributeProfileService;
        _attributeLibraryService = attributeLibraryService;
        _cvService = cvService;
        _adminService = adminService;
        _userSignInService = userSignInService;
    }

    [AllowAnonymous]
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken token) {
        var model = await _homeService.GetStatisticsAsync(token);
        return View("~/Views/Index.cshtml", model);
    }

    [AllowAnonymous]
    [HttpGet("positions")]
    public async Task<IActionResult> Positions(string? search, CancellationToken token) {
        var model = await _positionService.GetPositionsAsync(search, token);
        return View("~/Views/CandidateViews/Positions.cshtml", model);
    }

    [AllowAnonymous]
    [HttpGet("positions/{positionId:guid}")]
    public async Task<IActionResult> Position(Guid positionId, CancellationToken token) {
        var position = await _positionService.GetPositionAsync(positionId, token);
        if (position is null)
        {
            return NotFound();
        }

        return View("~/Views/CandidateViews/Position.cshtml", position);
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
            
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View("~/Views/LoginViews/Login.cshtml", new LoginRequest());
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl, CancellationToken token) {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View("~/Views/LoginViews/Login.cshtml", request);
        }

        var user = await _authServices.AuthenticateAsync(
            request.Email,
            request.Password,
            token);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            ViewData["ReturnUrl"] = returnUrl;
            return View("~/Views/LoginViews/Login.cshtml", request);
        }

        await _userSignInService.SignInAsync(user);

        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpGet("register/candidate")]
    public IActionResult RegisterCandidate() {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(
            "~/Views/LoginViews/RegisterCandidate.cshtml",
            new RegisterCandidateRequest());
    }

    [AllowAnonymous]
    [HttpPost("register/candidate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterCandidate(RegisterCandidateRequest request, CancellationToken token) {
        if (!ModelState.IsValid)
        {
            return View("~/Views/LoginViews/RegisterCandidate.cshtml", request);
        }

        try
        {
            var user = await _authServices.RegisterCandidateAsync(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                token);

            await _userSignInService.SignInAsync(user);
            return RedirectToAction(nameof(Profile));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("~/Views/LoginViews/RegisterCandidate.cshtml", request);
        }
    }

    [AllowAnonymous]
    [HttpGet("register/recruiter")]
    public IActionResult RegisterRecruiter() {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(
            "~/Views/LoginViews/RegisterRecruiter.cshtml",
            new RegisterRecruiterRequest());
    }

    [AllowAnonymous]
    [HttpPost("register/recruiter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterRecruiter(RegisterRecruiterRequest request, CancellationToken token) {
        if (!ModelState.IsValid)
        {
            return View("~/Views/LoginViews/RegisterRecruiter.cshtml", request);
        }

        try
        {
            var user = await _authServices.RegisterRecruiterAsync(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                request.Company,
                token);

            await _userSignInService.SignInAsync(user);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("~/Views/LoginViews/RegisterRecruiter.cshtml", request);
        }
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken token) {
        var userId = User.GetCurrentUserId();
        var me = await _profileService.GetMeAsync(userId, token);

        if (me is null)
        {
            return NotFound();
        }

        var model = new ProfilePageViewModel
        {
            Me = me,
            Attributes = await _attributeProfileService.GetProfileAttributesAsync(userId, token),
            AvailableAttributes = await _attributeProfileService.GetAvailableAttributesAsync(
                userId,
                page: 1,
                prefix: null,
                category: null,
                recentFirst: true,
                token),
            Cvs = await _cvService.GetCandidateCvsAsync(userId, token)
        };

        return View("~/Views/CandidateViews/Profile.cshtml", model);
    }

    [Authorize]
    [HttpGet("cvs/{cvId:guid}")]
    public async Task<IActionResult> Cv(Guid cvId, CancellationToken token) {
        var userId = User.GetCurrentUserId();
        var recruiterId = User.IsInRole(nameof(UserRole.Recruiter)) || User.IsInRole(nameof(UserRole.Admin)) ? userId : (Guid?)null;

        var cv = await _cvService.GetCvAsync(cvId, recruiterId, token);
        if (cv is null)
        {
            return NotFound();
        }

        if (User.IsInRole(nameof(UserRole.Candidate)))
        {
            if (cv.CandidateUserId != userId)
            {
                return Forbid();
            }
        }
        else if (cv.Status != CvStatus.Published)
        {
            return NotFound();
        }

        return View("~/Views/CandidateViews/Cv.cshtml", cv);
    }

    [Authorize(Roles = nameof(UserRole.Recruiter) + "," + nameof(UserRole.Admin))]
    [HttpGet("recruit")]
    public async Task<IActionResult> Recruit(int attributePage = 1, CancellationToken token = default) {
        var model = new RecruitPageViewModel
        {
            Positions = await _positionService.GetPositionsAsync(null, token),
            Attributes = await _attributeLibraryService.GetAttributesAsync(attributePage, prefix: null, category: null, token),
            AttributeOptions = await _attributeLibraryService.GetAllAttributesAsync(token),
            PublishedCvs = await _cvService.GetPublishedCvsAsync(null, null, null, token)
        };

        return View("~/Views/RecruitViews/RecruitPage.cshtml", model);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("admin")]
    public async Task<IActionResult> Admin(int page = 1, CancellationToken token = default) {
        var model = new AdminPageViewModel
        {
            Users = await _adminService.GetUsersAsync(page, token),
            Statistics = await _homeService.GetStatisticsAsync(token)
        };

        return View("~/Views/AdminViews/AdminPage.cshtml", model);
    }


    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() {
        await _userSignInService.SignOutAsync();

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied() {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/LoginViews/AccessDenied.cshtml");
    }
}
