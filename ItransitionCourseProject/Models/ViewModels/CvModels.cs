namespace ItransitionCourseProject.Models.ViewModels;

public class CvListItem {
    public Guid CvId { get; set; }
    public Guid PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public CvStatus Status { get; set; }
    public int LikeCount { get; set; }
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CvDetailsResponse : CvListItem {
    public Guid CandidateUserId { get; set; }
    public string CandidateLocation { get; set; } = string.Empty;
    public string? CandidatePhotoUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool LikedByCurrentRecruiter { get; set; }
    public List<ProfileAttributeItem> Attributes { get; set; } = [];
}

public class CvAttributeValueInput {
    public Guid AttributeId { get; set; }
    public string? Value { get; set; }
    public int Version { get; set; }
}

public class SaveCvRequest {
    public int Version { get; set; }
    public List<CvAttributeValueInput> Values { get; set; } = [];
}

public class PublishCvResponse {
    public bool Published { get; set; }
    public List<string> EmptyAttributes { get; set; } = [];
    public int Version { get; set; }
}

public class DiscussionItem {
    public Guid DiscussionId { get; set; }
    public Guid PositionId { get; set; }
    public Guid? AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddDiscussionRequest {
    public string ContentMarkdown { get; set; } = string.Empty;
}
