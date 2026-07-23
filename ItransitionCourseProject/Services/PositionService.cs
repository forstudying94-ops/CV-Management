using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface IPositionService {
    Task<List<PositionListItem>> GetPositionsAsync(string? search, CancellationToken token = default);
    Task<PositionDetailsResponse?> GetPositionAsync(Guid positionId, CancellationToken token = default);
    Task<PositionDetailsResponse> SavePositionAsync(SavePositionRequest request, CancellationToken token = default);
    Task<PositionDetailsResponse> DuplicatePositionAsync(Guid positionId, CancellationToken token = default);
    Task DeletePositionAsync(Guid positionId, CancellationToken token = default);
}

public class PositionService : IPositionService {
    private readonly DatabaseContext _db;

    public PositionService(DatabaseContext db) {
        _db = db;
    }

    public Task<List<PositionListItem>> GetPositionsAsync(string? search, CancellationToken token = default) {
        return ProjectList(ApplySearch(
                _db.Positions.AsNoTracking().Where(position => !position.IsDeleted),
                search))
            .OrderByDescending(position => position.UpdatedAt)
            .ToListAsync(token);
    }

    public async Task<PositionDetailsResponse?> GetPositionAsync(Guid positionId, CancellationToken token = default) {
        var position = await _db.Positions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(savedPosition => savedPosition.AttributeBindingForPosition)
                .ThenInclude(binding => binding.AttributeForBinding)
            .Include(savedPosition => savedPosition.ProjectTagBindingForPosition)
                .ThenInclude(binding => binding.TechnologyTagForBinding)
            .Include(savedPosition => savedPosition.CvForPosition)
            .FirstOrDefaultAsync(
                savedPosition => savedPosition.PositionId == positionId &&
                                 !savedPosition.IsDeleted,
                token);

        return position is null ? null : ToDetails(position);
    }

    public async Task<PositionDetailsResponse> SavePositionAsync(SavePositionRequest request, CancellationToken token = default) {
        Position position;
        if (request.PositionId is null)
        {
            position = new Position { PositionId = Guid.NewGuid() };
            _db.Positions.Add(position);
        }
        else
        {
            position = await _db.Positions
                .Include(savedPosition => savedPosition.AttributeBindingForPosition)
                .Include(savedPosition => savedPosition.ProjectTagBindingForPosition)
                .FirstOrDefaultAsync(
                    savedPosition => savedPosition.PositionId == request.PositionId &&
                                     !savedPosition.IsDeleted,
                    token)
                ?? throw new KeyNotFoundException("Position not found.");

            if (position.Version != request.Version)
            {
                throw new DbUpdateConcurrencyException("Position was changed in another tab.");
            }

            _db.PositionAttributeBindings.RemoveRange(position.AttributeBindingForPosition);
            _db.PositionProjectTags.RemoveRange(position.ProjectTagBindingForPosition);
            position.Version++;
        }

        position.Title = request.Title.Trim();
        position.ShortDescription = request.ShortDescription?.Trim();
        position.UpdatedAt = DateTime.UtcNow;

        var attributeInputs = request.Attributes
            .Where(input => input.AttributeId != Guid.Empty)
            .GroupBy(input => input.AttributeId)
            .Select(group => group.Last())
            .ToList();

        var attributeIds = attributeInputs.Select(input => input.AttributeId).ToList();
        var existingAttributeCount = await _db.Attributes.CountAsync(
            attribute => attributeIds.Contains(attribute.AttributeId) && !attribute.IsDeleted,
            token);

        if (existingAttributeCount != attributeIds.Count)
        {
            throw new KeyNotFoundException("One or more attributes were not found.");
        }

        foreach (var input in attributeInputs)
        {
            position.AttributeBindingForPosition.Add(new PositionAttributeBinding
            {
                PositionId = position.PositionId,
                AttributeId = input.AttributeId,
                IsRequired = input.IsRequired,
                DisplayOrder = input.DisplayOrder
            });
        }

        await AddProjectTagsAsync(position, request.ProjectTags, token);
        await _db.SaveChangesAsync(token);

        return await GetPositionAsync(position.PositionId, token)
               ?? throw new InvalidOperationException("Saved position could not be loaded.");
    }

    public async Task<PositionDetailsResponse> DuplicatePositionAsync(Guid positionId, CancellationToken token = default) {
        var source = await _db.Positions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(position => position.AttributeBindingForPosition)
            .Include(position => position.ProjectTagBindingForPosition)
                .ThenInclude(binding => binding.TechnologyTagForBinding)
            .FirstOrDefaultAsync(
                position => position.PositionId == positionId && !position.IsDeleted,
                token)
            ?? throw new KeyNotFoundException("Position not found.");

        var request = new SavePositionRequest
        {
            Title = $"{source.Title} (copy)",
            ShortDescription = source.ShortDescription,
            Attributes = source.AttributeBindingForPosition.Select(binding => new PositionAttributeInput
            {
                AttributeId = binding.AttributeId,
                IsRequired = binding.IsRequired,
                DisplayOrder = binding.DisplayOrder
            }).ToList(),
            ProjectTags = source.ProjectTagBindingForPosition
                .Select(binding => binding.TechnologyTagForBinding.Name)
                .ToList()
        };

        return await SavePositionAsync(request, token);
    }

    public async Task DeletePositionAsync(Guid positionId, CancellationToken token = default) {
        var position = await _db.Positions.FirstOrDefaultAsync(
            savedPosition => savedPosition.PositionId == positionId && !savedPosition.IsDeleted,
            token);

        if (position is null)
        {
            return;
        }

        position.IsDeleted = true;
        position.Version++;
        position.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
    }

    private async Task AddProjectTagsAsync(Position position, IEnumerable<string> requestedTags, CancellationToken token) {
        var tagNames = requestedTags
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Where(name => name.Length <= 100)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lowerNames = tagNames.Select(name => name.ToLowerInvariant()).ToList();
        var existingTags = await _db.TechnologyTags
            .Where(tag => lowerNames.Contains(tag.Name.ToLower()))
            .ToListAsync(token);

        var tagsByName = existingTags.ToDictionary(tag => tag.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var tagName in tagNames)
        {
            if (!tagsByName.TryGetValue(tagName, out var tag))
            {
                tag = new TechnologyTag { TechnologyTagId = Guid.NewGuid(), Name = tagName };
                _db.TechnologyTags.Add(tag);
                tagsByName[tagName] = tag;
            }

            position.ProjectTagBindingForPosition.Add(new PositionProjectTag
            {
                PositionId = position.PositionId,
                TechnologyTagId = tag.TechnologyTagId,
                TechnologyTagForBinding = tag
            });
        }
    }

    private static IQueryable<Position> ApplySearch(IQueryable<Position> query, string? search) {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var preparedSearch = search.Trim();
        return query.Where(position =>
            EF.Functions.ToTsVector(
                    "simple",
                    position.Title + " " + (position.ShortDescription ?? string.Empty))
                .Matches(EF.Functions.PlainToTsQuery("simple", preparedSearch)) ||
            position.ProjectTagBindingForPosition.Any(binding =>
                EF.Functions.ILike(binding.TechnologyTagForBinding.Name, $"%{preparedSearch}%")));
    }

    private static IQueryable<PositionListItem> ProjectList(IQueryable<Position> query) {
        return query.Select(position => new PositionListItem
        {
            PositionId = position.PositionId,
            Title = position.Title,
            ShortDescription = position.ShortDescription,
            CvCount = position.CvForPosition.Count,
            Version = position.Version,
            UpdatedAt = position.UpdatedAt,
            ProjectTags = position.ProjectTagBindingForPosition
                .OrderBy(binding => binding.TechnologyTagForBinding.Name)
                .Select(binding => binding.TechnologyTagForBinding.Name)
                .ToList()
        });
    }

    private static PositionDetailsResponse ToDetails(Position position) {
        return new PositionDetailsResponse
        {
            PositionId = position.PositionId,
            Title = position.Title,
            ShortDescription = position.ShortDescription,
            CvCount = position.CvForPosition.Count,
            Version = position.Version,
            UpdatedAt = position.UpdatedAt,
            ProjectTags = position.ProjectTagBindingForPosition
                .OrderBy(binding => binding.TechnologyTagForBinding.Name)
                .Select(binding => binding.TechnologyTagForBinding.Name)
                .ToList(),
            Attributes = position.AttributeBindingForPosition
                .OrderBy(binding => binding.DisplayOrder)
                .Select(binding => new ProfileAttributeItem
                {
                    AttributeId = binding.AttributeId,
                    Name = binding.AttributeForBinding.AttributeName,
                    Category = binding.AttributeForBinding.Category,
                    DisplayOrder = binding.DisplayOrder,
                    IsRequired = binding.IsRequired
                }).ToList()
        };
    }
}
