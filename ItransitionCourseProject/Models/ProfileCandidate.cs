namespace ItransitionCourseProject.Models;

public class ProfileCandidate {
    public Guid ProfileCandidateId { get; set; }
    public Guid UserId { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User UserForProfile { get; set; } = null!;
    public ICollection<ProfileAttributeBinding> BindingForProfile { get; set; } = new List<ProfileAttributeBinding>();
    public ICollection<CV> CvForProfile { get; set; } = new List<CV>();
}
