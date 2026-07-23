using System.ComponentModel.DataAnnotations;

namespace ItransitionCourseProject.Models;

public class User {
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? ProfilePicPublicId { get; set; }
    public string? Company { get; set; }
    public UserTheme Theme { get; set; } = UserTheme.Light;
    public bool IsBlocked { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public ProfileCandidate? ProfileForUser { get; set; }
    public ICollection<UserExternalLogin> ExternalLoginForUser { get; set; } = new List<UserExternalLogin>();
    public ICollection<Discussion> DiscussionForUser { get; set; } = new List<Discussion>();
    public ICollection<CvLike> CvLikeForUser { get; set; } = new List<CvLike>();
}
