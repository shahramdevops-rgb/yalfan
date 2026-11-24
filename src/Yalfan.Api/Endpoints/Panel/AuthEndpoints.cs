using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Sms;
using Yalfan.Application.Identity.GetCurrentUser;
using Yalfan.Application.Identity.Login;
using Yalfan.Application.Identity.Logout;
using Yalfan.Application.Identity.RefreshSession;
using Yalfan.Application.Identity.RequestLoginCode;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
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

        if (endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            MapDevelopmentLoginCode(auth);
        }
    }

    /// <summary>
    /// Until the SMS provider is connected (task 3.0), the Development panel shows the login code
    /// on its page instead of sending the developer to the log. The route exists only in
    /// Development, where the API listens on this machine only, and it answers only while the
    /// code is still valid; it is left out of the OpenAPI contract (ADR 0012). A POST, so the
    /// mobile stays out of the address and the request log, as for <c>/otp</c>.
    /// </summary>
    private static void MapDevelopmentLoginCode(RouteGroupBuilder auth) =>
        auth.MapPost("/dev/otp", Results<Ok<DevelopmentLoginCode>, NotFound> (
                DevelopmentLoginCodeRequest request,
                HttpContext httpContext,
                IClock clock) =>
            {
                // A real provider has no outbox: then there is nothing to show.
                var outbox = httpContext.RequestServices.GetService<ISmsOutbox>();
                var mobile = MobileNumber.Create(request.MobileNumber);
                var kept = outbox is not null && mobile.IsSuccess ? outbox.Last(mobile.Value, SmsTemplates.LoginCode) : null;

                return kept is not null && clock.UtcNow - kept.SentAt < OtpCode.Lifetime
                    ? TypedResults.Ok(new DevelopmentLoginCode(kept.Message.Values["code"]))
                    : TypedResults.NotFound();
            })
            .AllowAnonymous()
            .ExcludeFromDescription();

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

/// <summary>Which number's login code the Development login page shows.</summary>
public sealed record DevelopmentLoginCodeRequest(string? MobileNumber);

/// <summary>The login code last "sent" to a number, shown on the Development login page.</summary>
public sealed record DevelopmentLoginCode(string Code);
