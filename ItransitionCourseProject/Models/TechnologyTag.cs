namespace ItransitionCourseProject.Models;

public class TechnologyTag {
    public Guid TechnologyTagId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<PositionProjectTag> PositionBindingForTechnologyTag { get; set; } = new List<PositionProjectTag>();
}
