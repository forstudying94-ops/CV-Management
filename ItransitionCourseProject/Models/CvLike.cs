namespace ItransitionCourseProject.Models;

public class CvLike {
    public Guid CvId { get; set; }
    public Guid RecruiterId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CV CvForLike { get; set; } = null!;
    public User RecruiterForLike { get; set; } = null!;
}
