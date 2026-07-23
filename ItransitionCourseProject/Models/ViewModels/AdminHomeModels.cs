namespace ItransitionCourseProject.Models.ViewModels;

public class AdminUserItem {
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsBlocked { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangeRoleRequest {
    public UserRole Role { get; set; }
    public int Version { get; set; }
}

public class ChangeBlockedRequest {
    public bool IsBlocked { get; set; }
    public int Version { get; set; }
}

public class HomeStatisticsResponse {
    public int Positions { get; set; }
    public int Candidates { get; set; }
    public int Recruiters { get; set; }
    public int Cvs { get; set; }
    public int CvsLast24Hours { get; set; }
    public List<PositionListItem> RecentPositions { get; set; } = [];
    public List<PositionListItem> TopPositions { get; set; } = [];
}
