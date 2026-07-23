using System.ComponentModel.DataAnnotations;

namespace ItransitionCourseProject.Models.ViewModels;

public class ProfileMeResponse {
    public Guid ProfileId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? ProfilePicUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserTheme Theme { get; set; }
    public int UserVersion { get; set; }
    public int ProfileVersion { get; set; }
}

public class UpdateProfileMeRequest {
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public int UserVersion { get; set; }
    public int ProfileVersion { get; set; }
}

public class UpdatePreferencesRequest {
    public UserTheme Theme { get; set; }
    public int Version { get; set; }
}

public class ProfileAttributeItem {
    public Guid AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Value { get; set; }
    public int ValueVersion { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
}

public class AddProfileAttributeRequest {
    public Guid AttributeId { get; set; }
}

public class SaveProfileAttributeValueRequest {
    public string? Value { get; set; }
    public int Version { get; set; }
}

public class AvatarResponse {
    public string Url { get; set; } = string.Empty;
}

public class ProfilePageViewModel {
    public ProfileMeResponse Me { get; set; } = new();
    public List<ProfileAttributeItem> Attributes { get; set; } = [];
    public PagedResult<AttributeLibraryItem> AvailableAttributes { get; set; } = new();
    public List<CvListItem> Cvs { get; set; } = [];
}
