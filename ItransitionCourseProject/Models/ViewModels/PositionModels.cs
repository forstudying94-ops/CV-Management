using System.ComponentModel.DataAnnotations;

namespace ItransitionCourseProject.Models.ViewModels;

public class PositionAttributeInput {
    public Guid AttributeId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class SavePositionRequest {
    public Guid? PositionId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1_000)]
    public string? ShortDescription { get; set; }

    public List<PositionAttributeInput> Attributes { get; set; } = [];
    public List<string> ProjectTags { get; set; } = [];
    public int Version { get; set; }
}

public class PositionListItem {
    public Guid PositionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public int CvCount { get; set; }
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> ProjectTags { get; set; } = [];
}

public class PositionDetailsResponse : PositionListItem {
    public List<ProfileAttributeItem> Attributes { get; set; } = [];
}
