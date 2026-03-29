using FluentAssertions;
using FluentValidation.TestHelper;
using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Validators;
using TaskStatus = TaskBoard.Api.Enums.TaskStatus;

namespace TaskBoard.UnitTests.Validators;

public class UpdateTaskStatusRequestValidatorTests
{
    private readonly UpdateTaskStatusRequestValidator _validator = new();

    [Theory]
    [InlineData(TaskStatus.Todo)]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.InReview)]
    [InlineData(TaskStatus.Done)]
    [InlineData(TaskStatus.Cancelled)]
    public void Status_ValidValues_ShouldPass(TaskStatus status)
    {
        var model = new UpdateTaskStatusRequest { Status = status };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Status_InvalidEnumValue_ShouldHaveValidationError()
    {
        var model = new UpdateTaskStatusRequest { Status = (TaskStatus)99 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
