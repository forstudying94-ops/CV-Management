namespace ItransitionCourseProject.Models;

public class Position {
    public Guid PositionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public bool IsDeleted { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PositionAttributeBinding> AttributeBindingForPosition { get; set; } = new List<PositionAttributeBinding>();
    public ICollection<PositionProjectTag> ProjectTagBindingForPosition { get; set; } = new List<PositionProjectTag>();
    public ICollection<CV> CvForPosition { get; set; } = new List<CV>();
    public ICollection<Discussion> DiscussionForPosition { get; set; } = new List<Discussion>();
}
