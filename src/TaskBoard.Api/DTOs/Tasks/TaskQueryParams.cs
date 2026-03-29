using TaskBoard.Api.Enums;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.DTOs.Tasks;

public class TaskQueryParams
{
    public TaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? AssigneeId { get; set; }
    public bool IncludeArchived { get; set; } = false;

    // Sorting
    public string SortBy { get; set; } = "createdAt";   // createdAt | dueDate | priority | status
    public string SortDir { get; set; } = "asc";         // asc | desc

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
