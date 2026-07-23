using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface IAttributeProfileService {
    Task<Guid> GetProfileIdAsync(Guid userId, CancellationToken token = default);

    Task<List<ProfileAttributeItem>> GetProfileAttributesAsync(Guid userId, CancellationToken token = default);

    Task<PagedResult<AttributeLibraryItem>> GetAvailableAttributesAsync(Guid userId, int page, string? prefix, string? category, bool recentFirst, CancellationToken token = default);

    Task<bool> AddAttributeAsync(Guid userId, Guid attributeId, CancellationToken token = default);

    Task<bool> DeleteAttributeAsync(Guid userId, Guid attributeId, CancellationToken token = default);

    Task<int> SaveAttributeValueAsync(Guid userId, Guid attributeId, string? value, int version, CancellationToken token = default);

    Task<Dictionary<Guid, int>> SaveValuesAsync(Guid userId, IReadOnlyCollection<CvAttributeValueInput> values, CancellationToken token = default);

    Task EnsureAttributesAsync(Guid profileId, IReadOnlyCollection<Guid> attributeIds, CancellationToken token = default);
}

public class AttributeProfileService : IAttributeProfileService {
    private const int PageSize = 20;
    private readonly DatabaseContext _db;

    public AttributeProfileService(DatabaseContext db) {
        _db = db;
    }

    public async Task<Guid> GetProfileIdAsync(Guid userId, CancellationToken token = default) {
        var profileId = await _db.ProfileCandidates
            .Where(profile => profile.UserId == userId)
            .Select(profile => (Guid?)profile.ProfileCandidateId)
            .FirstOrDefaultAsync(token);

        return profileId ?? throw new KeyNotFoundException("Candidate profile not found.");
    }

    public Task<List<ProfileAttributeItem>> GetProfileAttributesAsync(Guid userId, CancellationToken token = default) {
        return _db.ProfileAttributeBindings
            .AsNoTracking()
            .Where(binding => binding.ProfileForBinding.UserId == userId &&
                              !binding.AttributeForBinding.IsDeleted)
            .OrderBy(binding => binding.DisplayOrder)
            .Select(binding => new ProfileAttributeItem
            {
                AttributeId = binding.AttributeId,
                Name = binding.AttributeForBinding.AttributeName,
                Category = binding.AttributeForBinding.Category,
                Value = binding.ValueForBinding == null ? null : binding.ValueForBinding.Value,
                ValueVersion = binding.ValueForBinding == null ? 0 : binding.ValueForBinding.Version,
                DisplayOrder = binding.DisplayOrder
            })
            .ToListAsync(token);
    }

    public async Task<PagedResult<AttributeLibraryItem>> GetAvailableAttributesAsync(Guid userId, int page, string? prefix, string? category, bool recentFirst, CancellationToken token = default) {
        page = Math.Max(page, 1);
        var profileId = await GetProfileIdAsync(userId, token);

        var query = _db.Attributes
            .AsNoTracking()
            .Where(attribute => !attribute.IsDeleted &&
                                !_db.ProfileAttributeBindings.Any(binding =>
                                    binding.ProfileCandidateId == profileId &&
                                    binding.AttributeId == attribute.AttributeId));

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var preparedPrefix = prefix.Trim();
            query = query.Where(attribute =>
                EF.Functions.ILike(attribute.AttributeName, $"{preparedPrefix}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(attribute => attribute.Category == category.Trim());
        }

        query = recentFirst
            ? query.OrderByDescending(attribute => attribute.ProfileBindingForAttribute.Count)
                .ThenBy(attribute => attribute.AttributeName)
            : query.OrderBy(attribute => attribute.AttributeName);

        var totalCount = await query.CountAsync(token);
        var items = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(attribute => new AttributeLibraryItem
            {
                AttributeId = attribute.AttributeId,
                Name = attribute.AttributeName,
                Category = attribute.Category,
                IsSystem = attribute.IsSystem,
                Version = attribute.Version
            })
            .ToListAsync(token);

        return new PagedResult<AttributeLibraryItem>
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> AddAttributeAsync(Guid userId, Guid attributeId, CancellationToken token = default) {
        var profileId = await GetProfileIdAsync(userId, token);

        if (await _db.ProfileAttributeBindings.AnyAsync(
                binding => binding.ProfileCandidateId == profileId &&
                           binding.AttributeId == attributeId,
                token))
        {
            return false;
        }

        if (!await _db.Attributes.AnyAsync(
                attribute => attribute.AttributeId == attributeId && !attribute.IsDeleted,
                token))
        {
            throw new KeyNotFoundException("Attribute not found.");
        }

        var nextOrder = await _db.ProfileAttributeBindings
            .Where(binding => binding.ProfileCandidateId == profileId)
            .Select(binding => (int?)binding.DisplayOrder)
            .MaxAsync(token) ?? -1;

        _db.ProfileAttributeBindings.Add(new ProfileAttributeBinding
        {
            ProfileCandidateId = profileId,
            AttributeId = attributeId,
            DisplayOrder = nextOrder + 1,
            ValueForBinding = new AttributeValue
            {
                AttributeValueId = Guid.NewGuid()
            }
        });

        await _db.SaveChangesAsync(token);
        return true;
    }

    public async Task<bool> DeleteAttributeAsync(Guid userId, Guid attributeId, CancellationToken token = default) {
        var binding = await _db.ProfileAttributeBindings
            .Include(savedBinding => savedBinding.AttributeForBinding)
            .FirstOrDefaultAsync(
                savedBinding => savedBinding.ProfileForBinding.UserId == userId &&
                                savedBinding.AttributeId == attributeId,
                token);

        if (binding is null)
        {
            return false;
        }

        if (binding.AttributeForBinding.IsSystem)
        {
            throw new InvalidOperationException("System attributes cannot be removed from a profile.");
        }

        _db.ProfileAttributeBindings.Remove(binding);
        await _db.SaveChangesAsync(token);
        return true;
    }

    public async Task<int> SaveAttributeValueAsync(Guid userId, Guid attributeId, string? value, int version, CancellationToken token = default) {
        var binding = await _db.ProfileAttributeBindings
            .Include(savedBinding => savedBinding.AttributeForBinding)
            .Include(savedBinding => savedBinding.ValueForBinding)
            .FirstOrDefaultAsync(
                savedBinding => savedBinding.ProfileForBinding.UserId == userId &&
                                savedBinding.AttributeId == attributeId,
                token)
            ?? throw new KeyNotFoundException("Profile attribute not found.");

        var savedValue = binding.ValueForBinding
                         ?? throw new InvalidOperationException("Attribute value was not initialized.");

        if (savedValue.Version != version)
        {
            throw new DbUpdateConcurrencyException("Attribute value was changed in another tab.");
        }

        savedValue.Value = AttributeValueValidator.Normalize(value);
        savedValue.Version++;
        savedValue.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(token);
        return savedValue.Version;
    }

    public async Task<Dictionary<Guid, int>> SaveValuesAsync(Guid userId, IReadOnlyCollection<CvAttributeValueInput> values, CancellationToken token = default) {
        var requestedValues = values
            .GroupBy(value => value.AttributeId)
            .Select(group => group.Last())
            .ToDictionary(value => value.AttributeId);

        var requestedIds = requestedValues.Keys.ToList();
        var bindings = await _db.ProfileAttributeBindings
            .Include(binding => binding.ValueForBinding)
            .Where(binding => binding.ProfileForBinding.UserId == userId &&
                              requestedIds.Contains(binding.AttributeId))
            .ToListAsync(token);

        if (bindings.Count != requestedIds.Count)
        {
            throw new KeyNotFoundException("One or more profile attributes were not found.");
        }

        foreach (var binding in bindings)
        {
            var input = requestedValues[binding.AttributeId];
            var savedValue = binding.ValueForBinding
                             ?? throw new InvalidOperationException("Attribute value was not initialized.");

            if (savedValue.Version != input.Version)
            {
                throw new DbUpdateConcurrencyException("Attribute values were changed in another tab.");
            }

            savedValue.Value = AttributeValueValidator.Normalize(input.Value);
            savedValue.Version++;
            savedValue.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(token);
        return bindings.ToDictionary(
            binding => binding.AttributeId,
            binding => binding.ValueForBinding!.Version);
    }

    public async Task EnsureAttributesAsync(Guid profileId, IReadOnlyCollection<Guid> attributeIds, CancellationToken token = default) {
        var distinctIds = attributeIds.Distinct().ToList();
        var existingIds = await _db.ProfileAttributeBindings
            .Where(binding => binding.ProfileCandidateId == profileId &&
                              distinctIds.Contains(binding.AttributeId))
            .Select(binding => binding.AttributeId)
            .ToListAsync(token);

        var missingIds = distinctIds.Except(existingIds).ToList();
        if (missingIds.Count == 0)
        {
            return;
        }

        var activeMissingIds = await _db.Attributes
            .Where(attribute => missingIds.Contains(attribute.AttributeId) && !attribute.IsDeleted)
            .Select(attribute => attribute.AttributeId)
            .ToListAsync(token);

        var nextOrder = await _db.ProfileAttributeBindings
            .Where(binding => binding.ProfileCandidateId == profileId)
            .Select(binding => (int?)binding.DisplayOrder)
            .MaxAsync(token) ?? -1;

        var newBindings = activeMissingIds.Select((attributeId, index) => new ProfileAttributeBinding
        {
            ProfileCandidateId = profileId,
            AttributeId = attributeId,
            DisplayOrder = nextOrder + index + 1,
            ValueForBinding = new AttributeValue
            {
                AttributeValueId = Guid.NewGuid()
            }
        });

        _db.ProfileAttributeBindings.AddRange(newBindings);
        await _db.SaveChangesAsync(token);
    }
}
