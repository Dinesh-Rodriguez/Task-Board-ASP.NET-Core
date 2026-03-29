using TaskBoard.Api.Enums;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.Api.DTOs.Tasks;

public class UpdateTaskStatusRequest
{
    public TaskStatus Status { get; set; }
}
