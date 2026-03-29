namespace TaskBoard.Api.Models;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Navigation
    public ICollection<TaskItem> Tasks { get; set; } = [];
    public ICollection<ProjectMember> Members { get; set; } = [];
}
