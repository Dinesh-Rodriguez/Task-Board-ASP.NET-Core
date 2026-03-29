using FluentValidation;
using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Enums;

namespace TaskBoard.Api.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(300).WithMessage("Task title must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("A valid project must be specified.");

        RuleFor(x => x.AssigneeId)
            .GreaterThan(0).WithMessage("Assignee ID must be a valid user ID.")
            .When(x => x.AssigneeId.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
            .When(x => x.DueDate.HasValue);
    }
}
