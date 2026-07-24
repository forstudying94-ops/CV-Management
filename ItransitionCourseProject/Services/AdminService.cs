using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface IAdminService {
    Task<PagedResult<AdminUserItem>> GetUsersAsync(int page, CancellationToken token = default);

    Task<int> ChangeBlockedAsync(Guid currentAdminId, Guid userId, ChangeBlockedRequest request, CancellationToken token = default);

    Task<int> ChangeRoleAsync(Guid userId, ChangeRoleRequest request, CancellationToken token = default);

    Task DeleteUserAsync(Guid currentAdminId, Guid userId, CancellationToken token = default);
}

public class AdminService : IAdminService {
    private const int PageSize = 20;
    private readonly DatabaseContext _db;

    public AdminService(DatabaseContext db) {
        _db = db;
    }

    public async Task<PagedResult<AdminUserItem>> GetUsersAsync(int page, CancellationToken token = default) {
        page = Math.Max(page, 1);
        var query = _db.Users.AsNoTracking();

        var totalCount = await query.CountAsync(token);
        var users = await query
            .OrderBy(user => user.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(user => new AdminUserItem
            {
                UserId = user.UserId,
                Name = user.FirstName + " " + user.LastName,
                Email = user.Email,
                Role = user.Role,
                IsBlocked = user.IsBlocked,
                Version = user.Version,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync(token);

        return new PagedResult<AdminUserItem>
        {
            Items = users,
            Page = page,
            PageSize = PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<int> ChangeBlockedAsync(Guid currentAdminId, Guid userId, ChangeBlockedRequest request, CancellationToken token = default) {
        if (currentAdminId == userId && request.IsBlocked)
        {
            throw new InvalidOperationException("Administrator cannot do this");
        }

        var user = await FindUserAsync(userId, token);
        CheckVersion(user, request.Version);

        user.IsBlocked = request.IsBlocked;
        user.Version++;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
        return user.Version;
    }

    public async Task<int> ChangeRoleAsync(Guid userId, ChangeRoleRequest request, CancellationToken token = default) {
        var user = await FindUserAsync(userId, token);
        CheckVersion(user, request.Version);

        if (request.Role == UserRole.Candidate && user.ProfileForUser is null)
        {
            user.ProfileForUser = new ProfileCandidate
            {
                ProfileCandidateId = Guid.NewGuid(),
                UserId = user.UserId
            };
        }

        user.Role = request.Role;
        user.Version++;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
        return user.Version;
    }

    public async Task DeleteUserAsync(Guid currentAdminId, Guid userId, CancellationToken token = default) {
        if (currentAdminId == userId)
        {
            throw new InvalidOperationException("Delete the current administrator from another admin account.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(
            savedUser => savedUser.UserId == userId,
            token);

        if (user is null)
        {
            return;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(token);
    }

    private async Task<User> FindUserAsync(Guid userId, CancellationToken token) {
        return await _db.Users.Include(user => user.ProfileForUser).FirstOrDefaultAsync(user => user.UserId == userId, token)
               ?? throw new KeyNotFoundException("User not found");
    }

    private static void CheckVersion(User user, int version) {
        if (user.Version != version)
        {
            throw new DbUpdateConcurrencyException("User was changed by another admin");
        }
    }
}
