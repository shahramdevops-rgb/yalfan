using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Identity.CreateStaffUser;

namespace Yalfan.Api.Endpoints.Panel;

public sealed class UserEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/panel/users").WithTags("Users");

        // Only the owner manages staff users.
        users.MapPost("/", async Task<Results<Ok<CreateStaffUserResponse>, ProblemHttpResult>> (
                CreateStaffUserCommand command,
                ICommandHandler<CreateStaffUserCommand, CreateStaffUserResponse> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk())
            .RequireAuthorization(Policies.Owner);
    }
}
