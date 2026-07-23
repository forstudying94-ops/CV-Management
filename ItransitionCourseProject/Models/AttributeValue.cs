namespace ItransitionCourseProject.Models;

public class AttributeValue {
    public Guid AttributeValueId { get; set; }
    public Guid ProfileCandidateId { get; set; }
    public Guid AttributeId { get; set; }
    public string? Value { get; set; }
    public int Version { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProfileAttributeBinding BindingForValue { get; set; } = null!;
}
