namespace ItransitionCourseProject.Models;

public class CV {
    public Guid CvId { get; set; }
    public Guid ProfileCandidateId { get; set; }
    public Guid PositionId { get; set; }
    public CvStatus Status { get; set; } = CvStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProfileCandidate ProfileForCv { get; set; } = null!;
    public Position PositionForCv { get; set; } = null!;
    public ICollection<CvLike> LikeForCv { get; set; } = new List<CvLike>();
}
