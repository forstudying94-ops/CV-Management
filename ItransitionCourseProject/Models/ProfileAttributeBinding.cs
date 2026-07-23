namespace ItransitionCourseProject.Models;

public class ProfileAttributeBinding {
    public Guid ProfileCandidateId { get; set; }
    public Guid AttributeId { get; set; }
    public int DisplayOrder { get; set; }

    public ProfileCandidate ProfileForBinding { get; set; } = null!;
    public Attribute AttributeForBinding { get; set; } = null!;
    public AttributeValue? ValueForBinding { get; set; }
}
