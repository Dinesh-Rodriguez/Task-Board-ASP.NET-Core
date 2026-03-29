using FluentValidation;
using TaskBoard.Api.DTOs.Tasks;

namespace TaskBoard.Api.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(300).WithMessage("Task title must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(x => x.AssigneeId)
            .GreaterThan(0).WithMessage("Assignee ID must be a valid user ID.")
            .When(x => x.AssigneeId.HasValue);

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
            .When(x => x.DueDate.HasValue);
    }
}
