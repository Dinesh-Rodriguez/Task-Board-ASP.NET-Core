using FluentValidation;
using TaskBoard.Api.DTOs.Tasks;

namespace TaskBoard.Api.Validators;

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.");
    }
}
