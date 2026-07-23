using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface IDiscussionService {
    Task<DiscussionItem> AddDiscussionAsync(Guid positionId, Guid authorId, string contentMarkdown, CancellationToken token = default);

    Task<List<DiscussionItem>> GetDiscussionsAsync(Guid positionId, DateTime? after, CancellationToken token = default);
}

public class DiscussionService : IDiscussionService {
    private readonly DatabaseContext _db;

    public DiscussionService(DatabaseContext db) {
        _db = db;
    }

    public async Task<DiscussionItem> AddDiscussionAsync(Guid positionId, Guid authorId, string contentMarkdown, CancellationToken token = default) {
        if (string.IsNullOrWhiteSpace(contentMarkdown))
        {
            throw new InvalidOperationException("Discussion message cannot be empty.");
        }

        if (contentMarkdown.Length > 5_000)
        {
            throw new InvalidOperationException("Discussion message cannot be longer than 5000 characters.");
        }

        if (!await _db.Positions.AnyAsync(
                position => position.PositionId == positionId && !position.IsDeleted,
                token))
        {
            throw new KeyNotFoundException("Position not found.");
        }

        var author = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserId == authorId, token)
            ?? throw new KeyNotFoundException("Author not found.");

        var discussion = new Discussion
        {
            DiscussionId = Guid.NewGuid(),
            PositionId = positionId,
            AuthorId = authorId,
            AuthorDisplayName = $"{author.FirstName} {author.LastName}".Trim(),
            ContentMarkdown = contentMarkdown.Trim()
        };

        _db.Discussions.Add(discussion);
        await _db.SaveChangesAsync(token);
        return ToItem(discussion);
    }

    public Task<List<DiscussionItem>> GetDiscussionsAsync(Guid positionId, DateTime? after, CancellationToken token = default) {
        return _db.Discussions
            .AsNoTracking()
            .Where(discussion => discussion.PositionId == positionId &&
                                 !discussion.PositionForDiscussion.IsDeleted &&
                                 (after == null || discussion.CreatedAt > after))
            .OrderBy(discussion => discussion.CreatedAt)
            .Select(discussion => new DiscussionItem
            {
                DiscussionId = discussion.DiscussionId,
                PositionId = discussion.PositionId,
                AuthorId = discussion.AuthorId,
                AuthorName = discussion.AuthorDisplayName,
                ContentMarkdown = discussion.ContentMarkdown,
                CreatedAt = discussion.CreatedAt
            })
            .ToListAsync(token);
    }

    private static DiscussionItem ToItem(Discussion discussion) {
        return new DiscussionItem
        {
            DiscussionId = discussion.DiscussionId,
            PositionId = discussion.PositionId,
            AuthorId = discussion.AuthorId,
            AuthorName = discussion.AuthorDisplayName,
            ContentMarkdown = discussion.ContentMarkdown,
            CreatedAt = discussion.CreatedAt
        };
    }
}
