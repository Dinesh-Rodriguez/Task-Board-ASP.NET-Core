using TaskBoard.Api.Enums;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.DTOs.Tasks;

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public int? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
}
