using Yalfan.Application.Common.Security;

namespace Yalfan.Infrastructure.Identity;

/// <summary>Nobody is logged in: start-up work and background jobs. The audit records these as "system".</summary>
internal sealed class NoCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
}
