namespace Yalfan.Application.Common.Security;

/// <summary>The user making the current request, or nobody for an anonymous request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
