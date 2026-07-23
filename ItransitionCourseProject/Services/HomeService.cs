using ItransitionCourseProject.DataBase;
using ItransitionCourseProject.Models;
using ItransitionCourseProject.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Services;

public interface IHomeService {
    Task<HomeStatisticsResponse> GetStatisticsAsync(CancellationToken token = default);
}

public class HomeService : IHomeService {
    private readonly DatabaseContext _db;

    public HomeService(DatabaseContext db) {
        _db = db;
    }

    public async Task<HomeStatisticsResponse> GetStatisticsAsync(CancellationToken token = default) {
        var lastDay = DateTime.UtcNow.AddHours(-24);

        var recentPositions = await _db.Positions
            .AsNoTracking()
            .Where(position => !position.IsDeleted)
            .OrderByDescending(position => position.CreatedAt)
            .Take(5)
            .Select(position => new PositionListItem
            {
                PositionId = position.PositionId,
                Title = position.Title,
                ShortDescription = position.ShortDescription,
                CvCount = position.CvForPosition.Count,
                Version = position.Version,
                UpdatedAt = position.UpdatedAt
            })
            .ToListAsync(token);

        var topPositions = await _db.Positions
            .AsNoTracking()
            .Where(position => !position.IsDeleted)
            .OrderByDescending(position => position.CvForPosition.Count)
            .Take(5)
            .Select(position => new PositionListItem
            {
                PositionId = position.PositionId,
                Title = position.Title,
                ShortDescription = position.ShortDescription,
                CvCount = position.CvForPosition.Count,
                Version = position.Version,
                UpdatedAt = position.UpdatedAt
            })
            .ToListAsync(token);

        return new HomeStatisticsResponse
        {
            Positions = await _db.Positions.CountAsync(position => !position.IsDeleted, token),
            Candidates = await _db.Users.CountAsync(user => user.Role == UserRole.Candidate, token),
            Recruiters = await _db.Users.CountAsync(user => user.Role == UserRole.Recruiter, token),
            Cvs = await _db.Cvs.CountAsync(token),
            CvsLast24Hours = await _db.Cvs.CountAsync(cv => cv.CreatedAt >= lastDay, token),
            RecentPositions = recentPositions,
            TopPositions = topPositions
        };
    }
}
