using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Attribute = ItransitionCourseProject.Models.Attribute;

namespace ItransitionCourseProject.Services;

public interface IAttributeLibraryService {
    Task<PagedResult<AttributeLibraryItem>> GetAttributesAsync(int page, string? prefix, string? category, CancellationToken token = default);

    Task<List<AttributeLibraryItem>> GetAllAttributesAsync(CancellationToken token = default);

    Task<AttributeLibraryItem?> GetAttributeAsync(Guid attributeId, CancellationToken token = default);

    Task<List<string>> GetCategoriesAsync(CancellationToken token = default);

    Task<AttributeLibraryItem> SaveAttributeAsync(SaveAttributeRequest request, CancellationToken token = default);

    Task DeleteAttributeAsync(Guid attributeId, CancellationToken token = default);
}

public class AttributeLibraryService : IAttributeLibraryService {
    private const int PageSize = 20;
    private readonly DatabaseContext _db;

    public AttributeLibraryService(DatabaseContext db) {
        _db = db;
    }

    public async Task<PagedResult<AttributeLibraryItem>> GetAttributesAsync(int page, string? prefix, string? category, CancellationToken token = default) {
        page = Math.Max(page, 1);
        var query = _db.Attributes.AsNoTracking().Where(attribute => !attribute.IsDeleted);

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

        var totalCount = await query.CountAsync(token);
        var items = await SelectItems(query.OrderBy(attribute => attribute.AttributeName)
                .Skip((page - 1) * PageSize).Take(PageSize)).ToListAsync(token);

        return new PagedResult<AttributeLibraryItem>
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalCount = totalCount
        };
    }

    public Task<List<AttributeLibraryItem>> GetAllAttributesAsync(CancellationToken token = default) {
        return SelectItems(_db.Attributes.AsNoTracking().Where(attribute => !attribute.IsDeleted)
                .OrderBy(attribute => attribute.Category).ThenBy(attribute => attribute.AttributeName)).ToListAsync(token);
    }

    public Task<AttributeLibraryItem?> GetAttributeAsync(Guid attributeId, CancellationToken token = default) {
        return SelectItems(_db.Attributes.AsNoTracking().Where(attribute => attribute.AttributeId == attributeId && !attribute.IsDeleted))
            .FirstOrDefaultAsync(token);
    }

    public Task<List<string>> GetCategoriesAsync(CancellationToken token = default) {
        return _db.Attributes.AsNoTracking().Where(attribute => !attribute.IsDeleted).Select(attribute => attribute.Category)
            .Distinct().OrderBy(category => category).ToListAsync(token);
    }

    public async Task<AttributeLibraryItem> SaveAttributeAsync(SaveAttributeRequest request, CancellationToken token = default) {
        var preparedName = request.Name.Trim();
        var attributeWithSameName = await _db.Attributes
            .Where(attribute => attribute.AttributeId != request.AttributeId && EF.Functions.ILike(attribute.AttributeName, preparedName))
            .OrderBy(attribute => attribute.IsDeleted).FirstOrDefaultAsync(token);

        Attribute attribute;
        if (request.AttributeId is null)
        {
            if (attributeWithSameName is { IsDeleted: false })
            {
                throw new InvalidOperationException("An attribute with this name already exists.");
            }

            if (attributeWithSameName is not null)
            {
                attribute = attributeWithSameName;
                attribute.IsDeleted = false;
                attribute.Version++;
            }
            else
            {
                attribute = new Attribute
                {
                    AttributeId = Guid.NewGuid()
                };
                _db.Attributes.Add(attribute);
            }
        }
        else
        {
            if (attributeWithSameName is not null)
            {
                throw new InvalidOperationException("An attribute with this name already exists.");
            }

            attribute = await _db.Attributes
                .FirstOrDefaultAsync(
                    savedAttribute => savedAttribute.AttributeId == request.AttributeId &&
                                      !savedAttribute.IsDeleted,
                    token)
                ?? throw new KeyNotFoundException("Attribute not found.");

            if (attribute.Version != request.Version)
            {
                throw new DbUpdateConcurrencyException("Attribute was changed in another tab.");
            }
            attribute.Version++;
        }

        attribute.AttributeName = preparedName;
        attribute.Category = request.Category.Trim();
        attribute.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(token);
        return await GetAttributeAsync(attribute.AttributeId, token)
               ?? throw new InvalidOperationException("Saved attribute could not be loaded.");
    }

    public async Task DeleteAttributeAsync(Guid attributeId, CancellationToken token = default) {
        var attribute = await _db.Attributes.FirstOrDefaultAsync(
            savedAttribute => savedAttribute.AttributeId == attributeId &&
                              !savedAttribute.IsDeleted,
            token);

        if (attribute is null)
        {
            return;
        }

        if (attribute.IsSystem)
        {
            throw new InvalidOperationException("System attributes cannot be deleted.");
        }

        attribute.IsDeleted = true;
        attribute.Version++;
        attribute.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
    }

    private static IQueryable<AttributeLibraryItem> SelectItems(IQueryable<Attribute> query) {
        return query.Select(attribute => new AttributeLibraryItem { AttributeId = attribute.AttributeId,
            Name = attribute.AttributeName, Category = attribute.Category, IsSystem = attribute.IsSystem, Version = attribute.Version });
    }
}
