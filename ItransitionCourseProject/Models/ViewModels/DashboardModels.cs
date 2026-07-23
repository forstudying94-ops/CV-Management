namespace ItransitionCourseProject.Models.ViewModels;

public class RecruitPageViewModel {
    public List<PositionListItem> Positions { get; set; } = [];
    public PagedResult<AttributeLibraryItem> Attributes { get; set; } = new();
    public List<AttributeLibraryItem> AttributeOptions { get; set; } = [];
    public List<CvListItem> PublishedCvs { get; set; } = [];
}

public class AdminPageViewModel {
    public PagedResult<AdminUserItem> Users { get; set; } = new();
    public HomeStatisticsResponse Statistics { get; set; } = new();
}
