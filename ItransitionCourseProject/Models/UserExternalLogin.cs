namespace ItransitionCourseProject.Models;

public class UserExternalLogin {
    public string Provider { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public User UserForExternalLogin { get; set; } = null!;
}
