namespace ItransitionCourseProject.Models;

public class Discussion {
    public Guid DiscussionId { get; set; }
    public Guid PositionId { get; set; }
    public Guid? AuthorId { get; set; }
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Position PositionForDiscussion { get; set; } = null!;
    public User? AuthorForDiscussion { get; set; }
}
