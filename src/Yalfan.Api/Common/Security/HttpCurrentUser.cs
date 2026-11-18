using Yalfan.Application.Common.Security;
using Yalfan.Infrastructure.Identity;

namespace Yalfan.Api.Common.Security;

/// <summary>Reads the user id from the validated access token of the current request.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(JwtClaimNames.Subject)?.Value, out var id) ? id : null;
}
