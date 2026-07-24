using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface ICvService {
    Task<List<CvListItem>> GetCandidateCvsAsync(Guid userId, CancellationToken token = default);

    Task<List<CvListItem>> GetPublishedCvsAsync(string? search, Guid? positionId, string? technologyTag, CancellationToken token = default);

    Task<CvDetailsResponse?> GetCvAsync(Guid cvId, Guid? recruiterId = null, CancellationToken token = default);

    Task<CvDetailsResponse> CreateCvAsync(Guid userId, Guid positionId, CancellationToken token = default);

    Task<Dictionary<Guid, int>> SaveCvAsync(Guid userId, Guid cvId, SaveCvRequest request, CancellationToken token = default);

    Task<PublishCvResponse> PublishCvAsync(Guid userId, Guid cvId, CancellationToken token = default);

    Task<int> ToggleLikeAsync(Guid recruiterId, Guid cvId, CancellationToken token = default);
}

public class CvService : ICvService {
    private readonly DatabaseContext _db;
    private readonly IAttributeProfileService _attributeProfileService;

    public CvService(DatabaseContext db, IAttributeProfileService attributeProfileService) {
        _db = db;
        _attributeProfileService = attributeProfileService;
    }

    public async Task<List<CvListItem>> GetCandidateCvsAsync(Guid userId, CancellationToken token = default) {
        return await _db.Cvs
            .AsNoTracking()
            .Where(cv => cv.ProfileForCv.UserId == userId && !cv.PositionForCv.IsDeleted)
            .OrderByDescending(cv => cv.UpdatedAt)
            .Select(cv => new CvListItem
            {
                CvId = cv.CvId,
                PositionId = cv.PositionId,
                PositionTitle = cv.PositionForCv.Title,
                CandidateName = cv.ProfileForCv.UserForProfile.FirstName + " " +
                                cv.ProfileForCv.UserForProfile.LastName,
                Status = cv.Status,
                LikeCount = cv.LikeForCv.Count,
                Version = cv.Version,
                UpdatedAt = cv.UpdatedAt
            })
            .ToListAsync(token);
    }

    public async Task<List<CvListItem>> GetPublishedCvsAsync(string? search, Guid? positionId, string? technologyTag, CancellationToken token = default) {
        var query = _db.Cvs
            .Where(cv => cv.Status == CvStatus.Published &&
                         !cv.PositionForCv.IsDeleted);

        if (positionId is not null)
        {
            query = query.Where(cv => cv.PositionId == positionId);
        }

        if (!string.IsNullOrWhiteSpace(technologyTag))
        {
            var preparedTag = technologyTag.Trim();
            query = query.Where(cv => cv.PositionForCv.ProjectTagBindingForPosition.Any(binding =>
                EF.Functions.ILike(binding.TechnologyTagForBinding.Name, preparedTag)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var preparedSearch = search.Trim();
            query = query.Where(cv =>
                EF.Functions.ToTsVector(
                        "simple",
                        cv.ProfileForCv.UserForProfile.FirstName + " " +
                        cv.ProfileForCv.UserForProfile.LastName + " " +
                        cv.PositionForCv.Title)
                    .Matches(EF.Functions.PlainToTsQuery("simple", preparedSearch)) ||
                cv.ProfileForCv.BindingForProfile.Any(binding =>
                    binding.ValueForBinding != null &&
                    EF.Functions.ILike(binding.ValueForBinding.Value!, $"%{preparedSearch}%")));
        }

        var cvs = await query
            .AsNoTracking()
            .AsSplitQuery()
            .Include(cv => cv.PositionForCv)
            .Include(cv => cv.ProfileForCv)
                .ThenInclude(profile => profile.UserForProfile)
            .Include(cv => cv.ProfileForCv)
                .ThenInclude(profile => profile.BindingForProfile)
                    .ThenInclude(binding => binding.ValueForBinding)
            .Include(cv => cv.LikeForCv)
            .OrderByDescending(cv => cv.PublishedAt)
            .ToListAsync(token);

        return cvs.Select(ToListItem).ToList();
    }

    public async Task<CvDetailsResponse?> GetCvAsync(Guid cvId, Guid? recruiterId = null, CancellationToken token = default) {
        var cv = await LoadCvAsync(cvId, tracking: false, token);
        return cv is null ? null : ToDetails(cv, recruiterId);
    }

    public async Task<CvDetailsResponse> CreateCvAsync(Guid userId, Guid positionId, CancellationToken token = default) {
        var profileId = await _attributeProfileService.GetProfileIdAsync(userId, token);

        var existingCvId = await _db.Cvs
            .Where(cv => cv.ProfileCandidateId == profileId &&
                         cv.PositionId == positionId &&
                         !cv.PositionForCv.IsDeleted)
            .Select(cv => (Guid?)cv.CvId)
            .FirstOrDefaultAsync(token);

        if (existingCvId is not null)
        {
            return await GetCvAsync(existingCvId.Value, token: token)
                   ?? throw new InvalidOperationException("Existing CV could not be loaded.");
        }

        var position = await _db.Positions
            .AsNoTracking()
            .Include(savedPosition => savedPosition.AttributeBindingForPosition)
            .FirstOrDefaultAsync(
                savedPosition => savedPosition.PositionId == positionId &&
                                 !savedPosition.IsDeleted,
                token)
            ?? throw new KeyNotFoundException("Position not found.");

        var attributeIds = position.AttributeBindingForPosition
            .Select(binding => binding.AttributeId)
            .ToList();

        await _attributeProfileService.EnsureAttributesAsync(profileId, attributeIds, token);

        var cv = new CV
        {
            CvId = Guid.NewGuid(),
            ProfileCandidateId = profileId,
            PositionId = positionId,
            Status = CvStatus.Draft
        };

        _db.Cvs.Add(cv);
        await _db.SaveChangesAsync(token);

        return await GetCvAsync(cv.CvId, token: token)
               ?? throw new InvalidOperationException("Created CV could not be loaded.");
    }

    public async Task<Dictionary<Guid, int>> SaveCvAsync(Guid userId, Guid cvId, SaveCvRequest request, CancellationToken token = default) {
        var cv = await _db.Cvs
            .Include(savedCv => savedCv.PositionForCv)
                .ThenInclude(position => position.AttributeBindingForPosition)
            .FirstOrDefaultAsync(
                savedCv => savedCv.CvId == cvId &&
                           savedCv.ProfileForCv.UserId == userId &&
                           !savedCv.PositionForCv.IsDeleted,
                token)
            ?? throw new KeyNotFoundException("CV not found.");

        if (cv.Version != request.Version)
        {
            throw new DbUpdateConcurrencyException("CV was changed in another tab.");
        }

        var allowedIds = cv.PositionForCv.AttributeBindingForPosition
            .Select(binding => binding.AttributeId)
            .ToHashSet();

        if (request.Values.Any(value => !allowedIds.Contains(value.AttributeId)))
        {
            throw new InvalidOperationException("CV contains an attribute that is not selected by its position.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(token);
        var versions = await _attributeProfileService.SaveValuesAsync(userId, request.Values, token);

        cv.Version++;
        cv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);

        return versions;
    }

    public async Task<PublishCvResponse> PublishCvAsync(Guid userId, Guid cvId, CancellationToken token = default) {
        var cv = await LoadCvAsync(cvId, tracking: true, token)
                 ?? throw new KeyNotFoundException("CV not found");
        var values = cv.ProfileForCv.BindingForProfile.ToDictionary(
            binding => binding.AttributeId,
            binding => binding.ValueForBinding?.Value);

        var emptyAttributes = cv.PositionForCv.AttributeBindingForPosition
            .Where(binding => binding.IsRequired &&
                              (!values.TryGetValue(binding.AttributeId, out var value) ||
                               !AttributeValueValidator.IsFilled(value)))
            .Select(binding => binding.AttributeForBinding.AttributeName)
            .ToList();

        if (emptyAttributes.Count > 0)
        {
            return new PublishCvResponse
            {
                Published = false,
                EmptyAttributes = emptyAttributes,
                Version = cv.Version
            };
        }

        cv.Status = CvStatus.Published;
        cv.PublishedAt = DateTime.UtcNow;
        cv.UpdatedAt = DateTime.UtcNow;
        cv.Version++;
        await _db.SaveChangesAsync(token);

        return new PublishCvResponse
        {
            Published = true,
            Version = cv.Version
        };
    }

    public async Task<int> ToggleLikeAsync(Guid recruiterId, Guid cvId, CancellationToken token = default) {
        var recruiter = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserId == recruiterId, token)
            ?? throw new KeyNotFoundException("Recruiter not found");

        if (recruiter.Role is not (UserRole.Recruiter or UserRole.Admin))
        {
            throw new UnauthorizedAccessException("Only recruiters can like CVs");
        }

        var cvInfo = await _db.Cvs
            .AsNoTracking()
            .Where(cv => cv.CvId == cvId &&
                         cv.Status == CvStatus.Published &&
                         !cv.PositionForCv.IsDeleted)
            .Select(cv => new
            {
                cv.PositionId,
                CandidateUserId = cv.ProfileForCv.UserId
            })
            .FirstOrDefaultAsync(token);

        if (cvInfo is null)
        {
            throw new KeyNotFoundException("CV not found");
        }

        var like = await _db.CvLikes.FirstOrDefaultAsync(
            savedLike => savedLike.CvId == cvId && savedLike.RecruiterId == recruiterId,
            token);

        if (like is null)
        {
            _db.CvLikes.Add(new CvLike
            {
                CvId = cvId,
                RecruiterId = recruiterId
            });
        }
        else
        {
            _db.CvLikes.Remove(like);
        }

        await _db.SaveChangesAsync(token);
        return await _db.CvLikes.CountAsync(savedLike => savedLike.CvId == cvId, token);
    }

    private async Task<CV?> LoadCvAsync(Guid cvId, bool tracking, CancellationToken token) {
        var query = _db.Cvs.AsSplitQuery();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Include(cv => cv.ProfileForCv)
                .ThenInclude(profile => profile.UserForProfile)
            .Include(cv => cv.ProfileForCv)
                .ThenInclude(profile => profile.BindingForProfile)
                    .ThenInclude(binding => binding.ValueForBinding)
            .Include(cv => cv.PositionForCv)
                .ThenInclude(position => position.AttributeBindingForPosition)
                    .ThenInclude(binding => binding.AttributeForBinding)
            .Include(cv => cv.LikeForCv)
            .FirstOrDefaultAsync(
                cv => cv.CvId == cvId && !cv.PositionForCv.IsDeleted,
                token);
    }

    private static CvListItem ToListItem(CV cv) {
        return new CvListItem
        {
            CvId = cv.CvId,
            PositionId = cv.PositionId,
            PositionTitle = cv.PositionForCv.Title,
            CandidateName = $"{cv.ProfileForCv.UserForProfile.FirstName} {cv.ProfileForCv.UserForProfile.LastName}".Trim(),
            Status = cv.Status,
            LikeCount = cv.LikeForCv.Count,
            Version = cv.Version,
            UpdatedAt = cv.UpdatedAt
        };
    }

    private static CvDetailsResponse ToDetails(CV cv, Guid? recruiterId) {
        var values = cv.ProfileForCv.BindingForProfile.ToDictionary(
            binding => binding.AttributeId,
            binding => binding.ValueForBinding);

        return new CvDetailsResponse
        {
            CvId = cv.CvId,
            CandidateUserId = cv.ProfileForCv.UserId,
            PositionId = cv.PositionId,
            PositionTitle = cv.PositionForCv.Title,
            CandidateName = $"{cv.ProfileForCv.UserForProfile.FirstName} {cv.ProfileForCv.UserForProfile.LastName}".Trim(),
            CandidateLocation = cv.ProfileForCv.Location,
            CandidatePhotoUrl = cv.ProfileForCv.UserForProfile.ProfilePicUrl,
            Status = cv.Status,
            PublishedAt = cv.PublishedAt,
            LikeCount = cv.LikeForCv.Count,
            LikedByCurrentRecruiter = recruiterId is not null &&
                                        cv.LikeForCv.Any(like => like.RecruiterId == recruiterId),
            Version = cv.Version,
            UpdatedAt = cv.UpdatedAt,
            Attributes = cv.PositionForCv.AttributeBindingForPosition
                .OrderBy(binding => binding.DisplayOrder)
                .Select(binding =>
                {
                    values.TryGetValue(binding.AttributeId, out var value);
                    return new ProfileAttributeItem
                    {
                        AttributeId = binding.AttributeId,
                        Name = binding.AttributeForBinding.AttributeName,
                        Category = binding.AttributeForBinding.Category,
                        Value = value?.Value,
                        ValueVersion = value?.Version ?? 0,
                        DisplayOrder = binding.DisplayOrder,
                        IsRequired = binding.IsRequired
                    };
                }).ToList()
        };
    }
}
