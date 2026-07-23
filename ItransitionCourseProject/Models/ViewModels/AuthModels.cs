using System.ComponentModel.DataAnnotations;

namespace ItransitionCourseProject.Models.ViewModels;

public class LoginRequest {
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterCandidateRequest {
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRecruiterRequest : RegisterCandidateRequest {
    [Required, MaxLength(200)]
    public string Company { get; set; } = string.Empty;
}

public class CurrentUserResponse {
    public Guid UserId { get; set; }
    public Guid? ProfileId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? ProfilePicUrl { get; set; }
    public UserTheme Theme { get; set; }
    public int Version { get; set; }
}
