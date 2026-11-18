using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Identity.GetCurrentUser;
using Yalfan.Application.Identity.Login;
using Yalfan.Application.Identity.Logout;
using Yalfan.Application.Identity.RefreshSession;
using Yalfan.Application.Identity.RequestLoginCode;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Identity;

namespace Yalfan.Api.Endpoints.Panel;

/// <summary>
/// Staff login with an SMS code (ADR 0012). The access token goes in the body; the refresh
/// token only in an HttpOnly cookie, which the panel's JavaScript cannot read, so a script
/// injected into the page cannot steal a seven-day session.
/// </summary>
public sealed class AuthEndpoints : IEndpointModule
{
    public const string RefreshCookieName = "yalfan_refresh";
    public const string RoutePrefix = "/panel/auth";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup(RoutePrefix).WithTags("Auth");

        auth.MapPost("/otp", async Task<Results<Ok<LoginCodeRequested>, ProblemHttpResult>> (
                RequestLoginCodeCommand command,
                ICommandHandler<RequestLoginCodeCommand, LoginCodeRequested> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk())
            .AllowAnonymous();

        auth.MapPost("/login", async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> (
                LoginCommand command,
                ICommandHandler<LoginCommand, AuthSession> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
                ToSessionResponse(await handler.Handle(command, cancellationToken), httpContext))
            .AllowAnonymous();

        auth.MapPost("/refresh", async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> (
                ICommandHandler<RefreshSessionCommand, AuthSession> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var command = new RefreshSessionCommand(httpContext.Request.Cookies[RefreshCookieName]);
                var result = await handler.Handle(command, cancellationToken);

                // A dead session's cookie is useless; a superseded one is about to be replaced
                // by the other tab's response, so it is left alone.
                if (result.IsFailure && result.Error == IdentityErrors.RefreshInvalid)
                {
                    DeleteRefreshCookie(httpContext);
                }

                return ToSessionResponse(result, httpContext);
            })
            .AllowAnonymous();

        auth.MapPost("/logout", async Task<NoContent> (
                ICommandHandler<LogoutCommand, LogoutResult> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                await handler.Handle(new LogoutCommand(httpContext.Request.Cookies[RefreshCookieName]), cancellationToken);
                DeleteRefreshCookie(httpContext);
                return TypedResults.NoContent();
            })
            .AllowAnonymous();

        auth.MapGet("/me", async Task<Results<Ok<UserSummary>, ProblemHttpResult>> (
                IQueryHandler<GetCurrentUserQuery, UserSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetCurrentUserQuery(), cancellationToken)).ToOk())
            .RequireAuthorization(Policies.Staff);
    }

    private static Results<Ok<SessionResponse>, ProblemHttpResult> ToSessionResponse(
        Domain.Common.Result<AuthSession> result,
        HttpContext httpContext)
    {
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var session = result.Value;
        httpContext.Response.Cookies.Append(RefreshCookieName, session.RefreshToken, RefreshCookie(session.RefreshTokenExpiresAt));

        return TypedResults.Ok(new SessionResponse(session.AccessToken, session.AccessTokenExpiresAt, session.User));
    }

    private static void DeleteRefreshCookie(HttpContext httpContext) =>
        httpContext.Response.Cookies.Delete(RefreshCookieName, RefreshCookie(expires: null));

    /// <summary>
    /// HttpOnly: no script can read it. Secure: never sent over plain HTTP (browsers treat
    /// localhost as secure). SameSite=Strict: never sent by another site's page. Path: sent only
    /// to the auth routes, not with every API call.
    /// </summary>
    private static CookieOptions RefreshCookie(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = RoutePrefix,
        Expires = expires,
        IsEssential = true,
    };
}

/// <summary>A started or renewed session as the panel receives it. The refresh token is in the cookie.</summary>
public sealed record SessionResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserSummary User);
