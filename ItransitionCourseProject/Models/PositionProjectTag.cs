namespace ItransitionCourseProject.Models;

public class PositionProjectTag {
    public Guid PositionId { get; set; }
    public Guid TechnologyTagId { get; set; }

    public Position PositionForBinding { get; set; } = null!;
    public TechnologyTag TechnologyTagForBinding { get; set; } = null!;
}
