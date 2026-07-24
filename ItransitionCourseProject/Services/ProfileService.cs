using System.Security.Claims;
using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface IProfileService {
    Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken token = default);

    Task<CurrentUserResponse?> GetCurrentUserInfoAsync(Guid userId, CancellationToken token = default);

    Task<ProfileMeResponse?> GetMeAsync(Guid userId, CancellationToken token = default);

    Task<ProfileMeResponse> SaveMeAsync(Guid userId, UpdateProfileMeRequest request, CancellationToken token = default);

    Task<int> SavePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken token = default);

    Task<string> UploadProfilePictureAsync(Guid userId, IFormFile file, CancellationToken token = default);
}

public class ProfileService : IProfileService {
    private readonly DatabaseContext _db;
    private readonly IImageService _imageService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(DatabaseContext db, IImageService imageService, ILogger<ProfileService> logger) {
        _db = db;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken token = default) {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserId == userId, token);
    }

    public Task<CurrentUserResponse?> GetCurrentUserInfoAsync(Guid userId, CancellationToken token = default) {
        return _db.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId)
            .Select(user => new CurrentUserResponse
            {
                UserId = user.UserId,
                ProfileId = user.ProfileForUser == null
                    ? null
                    : user.ProfileForUser.ProfileCandidateId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                ProfilePicUrl = user.ProfilePicUrl,
                Theme = user.Theme,
                Version = user.Version
            })
            .FirstOrDefaultAsync(token);
    }

    public Task<ProfileMeResponse?> GetMeAsync(Guid userId, CancellationToken token = default) {
        return _db.ProfileCandidates
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new ProfileMeResponse {
                ProfileId = profile.ProfileCandidateId,
                FirstName = profile.UserForProfile.FirstName,
                LastName = profile.UserForProfile.LastName,
                Location = profile.Location,
                ProfilePicUrl = profile.UserForProfile.ProfilePicUrl,
                Email = profile.UserForProfile.Email,
                Theme = profile.UserForProfile.Theme,
                UserVersion = profile.UserForProfile.Version,
                ProfileVersion = profile.Version
            })
            .FirstOrDefaultAsync(token);
    }

    public async Task<ProfileMeResponse> SaveMeAsync(Guid userId, UpdateProfileMeRequest request, CancellationToken token = default) {
        var profile = await _db.ProfileCandidates
            .Include(savedProfile => savedProfile.UserForProfile)
            .FirstOrDefaultAsync(savedProfile => savedProfile.UserId == userId, token)
            ?? throw new KeyNotFoundException("Candidate profile not found.");

        if (profile.Version != request.ProfileVersion ||
            profile.UserForProfile.Version != request.UserVersion)
        {
            throw new DbUpdateConcurrencyException("Profile was changed in another tab.");
        }

        profile.UserForProfile.FirstName = request.FirstName.Trim();
        profile.UserForProfile.LastName = request.LastName.Trim();
        profile.UserForProfile.Version++;
        profile.UserForProfile.UpdatedAt = DateTime.UtcNow;

        profile.Location = request.Location.Trim();
        profile.Version++;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(token);
        return await GetMeAsync(userId, token)
               ?? throw new KeyNotFoundException("Candidate profile not found.");
    }

    public async Task<int> SavePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken token = default) {
        var user = await _db.Users.FirstOrDefaultAsync(
            savedUser => savedUser.UserId == userId,
            token)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Version != request.Version)
        {
            throw new DbUpdateConcurrencyException("Preferences were changed in another tab.");
        }

        user.Theme = request.Theme;
        user.Version++;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(token);
        return user.Version;
    }

    public async Task<string> UploadProfilePictureAsync(Guid userId, IFormFile file, CancellationToken token = default) {
        var user = await _db.Users.FirstOrDefaultAsync(
            savedUser => savedUser.UserId == userId,
            token)
            ?? throw new KeyNotFoundException("User not found.");

        var oldPublicId = user.ProfilePicPublicId;
        var uploadedImage = await _imageService.UploadAvatarAsync(file, token);

        user.ProfilePicUrl = uploadedImage.Url;
        user.ProfilePicPublicId = uploadedImage.PublicId;
        user.Version++;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(token);

        try
        {
            await _imageService.DeleteAsync(oldPublicId, token);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The old profile image could not be deleted.");
        }

        return uploadedImage.Url;
    }
}
