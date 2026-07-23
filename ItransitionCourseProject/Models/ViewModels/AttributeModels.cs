using System.ComponentModel.DataAnnotations;

namespace ItransitionCourseProject.Models.ViewModels;

public class AttributeLibraryItem {
    public Guid AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public int Version { get; set; }
}

public class SaveAttributeRequest {
    public Guid? AttributeId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public int Version { get; set; }
}
