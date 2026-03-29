using TaskBoard.Api.Enums;

namespace TaskBoard.Api.Auth;

/// <summary>
/// Marks a controller or action as requiring a minimum user role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : Attribute
{
    public UserRole MinimumRole { get; }

    public RequireRoleAttribute(UserRole minimumRole = UserRole.Member)
    {
        MinimumRole = minimumRole;
    }
}
