namespace ItransitionCourseProject.Models;

public class Attribute {
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsDeleted { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProfileAttributeBinding> ProfileBindingForAttribute { get; set; } = new List<ProfileAttributeBinding>();
    public ICollection<PositionAttributeBinding> PositionBindingForAttribute { get; set; } = new List<PositionAttributeBinding>();
}
