using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Yalfan.Application.Common.Security;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Identity;

namespace Yalfan.Api.Common.Security;

/// <summary>Names of the authorization policies used by endpoints.</summary>
public static class Policies
{
    /// <summary>Any staff role. Every <c>/panel</c> route needs it, so a contractor's or customer's token never reaches the panel.</summary>
    public const string Staff = "staff";

    public const string Owner = "owner";
}

public static class AuthenticationSetup
{
    /// <summary>Small tolerance for clocks that differ a little between machines.</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddStaffAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, time) =>
            {
                // Keep the short claim names ("sub", "role") instead of the long .NET URIs.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = jwt.Value.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtClaimNames.Name,
                    RoleClaimType = JwtClaimNames.Role,
                    ClockSkew = ClockSkew,

                    // The lifetime is checked against the injected TimeProvider, the same clock
                    // that stamped the token, so tests that move time stay consistent.
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = time.GetUtcNow().UtcDateTime;
                        return expires is { } end && end > now - ClockSkew
                            && (notBefore is not { } start || start <= now + ClockSkew);
                    },
                };
            });

        services.AddAuthorizationBuilder()
            // Closed by default: an endpoint someone forgot to protect still needs a login.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Staff, policy => policy.RequireRole(
                nameof(Role.Owner), nameof(Role.Manager), nameof(Role.Operator)))
            .AddPolicy(Policies.Owner, policy => policy.RequireRole(nameof(Role.Owner)));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }
}
