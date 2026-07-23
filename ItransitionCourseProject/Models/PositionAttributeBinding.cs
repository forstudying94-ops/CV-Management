namespace ItransitionCourseProject.Models;

public class PositionAttributeBinding {
    public Guid PositionId { get; set; }
    public Guid AttributeId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int DisplayOrder { get; set; }

    public Position PositionForBinding { get; set; } = null!;
    public Attribute AttributeForBinding { get; set; } = null!;
}
