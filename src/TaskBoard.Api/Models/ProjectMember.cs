using TaskBoard.Api.Enums;

namespace TaskBoard.Api.Models;

/// <summary>Join entity representing a user's membership in a project.</summary>
public class ProjectMember
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
