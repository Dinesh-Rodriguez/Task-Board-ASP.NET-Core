using TaskBoard.Api.Enums;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.DTOs.Tasks;

public class TaskResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
}
