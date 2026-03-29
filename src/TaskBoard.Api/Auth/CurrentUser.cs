using TaskBoard.Api.Enums;

namespace TaskBoard.Api.Auth;

/// <summary>Represents the caller extracted from request headers.</summary>
public class CurrentUser
{
    public int Id { get; init; }
    public UserRole Role { get; init; }

    public bool IsAdmin => Role == UserRole.Admin;
}
