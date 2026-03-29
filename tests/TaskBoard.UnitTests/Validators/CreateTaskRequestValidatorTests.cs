using FluentAssertions;
using FluentValidation.TestHelper;
using TaskBoard.Api.DTOs.Tasks;
using TaskBoard.Api.Enums;
using TaskBoard.Api.Validators;

namespace TaskBoard.UnitTests.Validators;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    // ── Title ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Title_Empty_ShouldHaveValidationError()
    {
        var model = ValidRequest(); model.Title = "";
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_Null_ShouldHaveValidationError()
    {
        var model = ValidRequest(); model.Title = null!;
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_TooLong_ShouldHaveValidationError()
    {
        var model = ValidRequest(); model.Title = new string('x', 301);
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_MaxLength_ShouldPass()
    {
        var model = ValidRequest(); model.Title = new string('x', 300);
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    // ── Description ───────────────────────────────────────────────────────────

    [Fact]
    public void Description_TooLong_ShouldHaveValidationError()
    {
        var model = ValidRequest(); model.Description = new string('d', 2001);
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_Null_ShouldPass()
    {
        var model = ValidRequest(); model.Description = null;
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // ── ProjectId ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ProjectId_Invalid_ShouldHaveValidationError(int projectId)
    {
        var model = ValidRequest(); model.ProjectId = projectId;
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void ProjectId_Positive_ShouldPass()
    {
        var model = ValidRequest(); model.ProjectId = 1;
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // ── AssigneeId ────────────────────────────────────────────────────────────

    [Fact]
    public void AssigneeId_Zero_ShouldHaveValidationError()
    {
        var model = ValidRequest(); model.AssigneeId = 0;
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.AssigneeId);
    }

    [Fact]
    public void AssigneeId_Null_ShouldPass()
    {
        var model = ValidRequest(); model.AssigneeId = null;
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.AssigneeId);
    }

    [Fact]
    public void AssigneeId_PositiveInt_ShouldPass()
    {
        var model = ValidRequest(); model.AssigneeId = 5;
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.AssigneeId);
    }

    // ── Priority ──────────────────────────────────────────────────────────────

    [Fact]
    public void Priority_InvalidEnumValue_ShouldHaveValidationError()
    {
        var model = ValidRequest(); model.Priority = (TaskPriority)99;
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Critical)]
    public void Priority_ValidValues_ShouldPass(TaskPriority priority)
    {
        var model = ValidRequest(); model.Priority = priority;
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    // ── DueDate ───────────────────────────────────────────────────────────────

    [Fact]
    public void DueDate_InThePast_ShouldHaveValidationError()
    {
        var model = ValidRequest(); model.DueDate = DateTime.UtcNow.AddDays(-1);
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void DueDate_InTheFuture_ShouldPass()
    {
        var model = ValidRequest(); model.DueDate = DateTime.UtcNow.AddDays(7);
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void DueDate_Null_ShouldPass()
    {
        var model = ValidRequest(); model.DueDate = null;
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    // ── Full valid model ──────────────────────────────────────────────────────

    [Fact]
    public void ValidRequest_PassesAllRules()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static CreateTaskRequest ValidRequest() => new()
    {
        Title     = "Valid task title",
        ProjectId = 1,
        Priority  = TaskPriority.Medium,
        DueDate   = DateTime.UtcNow.AddDays(5)
    };
}
