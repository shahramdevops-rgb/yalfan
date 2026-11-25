using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Identity;
using Yalfan.Application.Identity.ChangeUserMobileNumber;
using Yalfan.Application.Identity.ChangeUserRole;
using Yalfan.Application.Identity.CreateStaffUser;
using Yalfan.Application.Identity.ListStaffUsers;
using Yalfan.Application.Identity.RenameUser;
using Yalfan.Application.Identity.SetLoginNumberActive;
using Yalfan.Application.Identity.SetUserActive;
using Yalfan.Domain.Identity;

namespace Yalfan.Api.Endpoints.Panel;

/// <summary>
/// The owner's users screen (ADR 0012, ADR 0026). Only an owner reaches it: whoever can change
/// users can take over the panel. Every change answers the whole user.
/// </summary>
public sealed class UserEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/panel/users").WithTags("Users").RequireAuthorization(Policies.Owner);

        users.MapGet("/", async Task<Results<Ok<IReadOnlyList<StaffUserDetails>>, ProblemHttpResult>> (
                IQueryHandler<ListStaffUsersQuery, IReadOnlyList<StaffUserDetails>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListStaffUsersQuery(), cancellationToken)).ToOk());

        users.MapPost("/", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                CreateStaffUserCommand command,
                ICommandHandler<CreateStaffUserCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        users.MapPut("/{id:guid}/name", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                Guid id,
                RenameUserRequest request,
                ICommandHandler<RenameUserCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameUserCommand(id, request.FullName), cancellationToken)).ToOk());

        users.MapPut("/{id:guid}/role", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                Guid id,
                ChangeUserRoleRequest request,
                ICommandHandler<ChangeUserRoleCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ChangeUserRoleCommand(id, request.Role), cancellationToken)).ToOk());

        users.MapPut("/{id:guid}/mobile", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                Guid id,
                ChangeUserMobileNumberRequest request,
                ICommandHandler<ChangeUserMobileNumberCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ChangeUserMobileNumberCommand(id, request.MobileNumber), cancellationToken)).ToOk());

        users.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetUserActiveCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetUserActiveCommand(id, IsActive: false), cancellationToken)).ToOk());

        users.MapPost("/{id:guid}/activate", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetUserActiveCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetUserActiveCommand(id, IsActive: true), cancellationToken)).ToOk());

        users.MapPost("/{id:guid}/numbers/{numberId:guid}/deactivate", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                Guid id,
                Guid numberId,
                ICommandHandler<SetLoginNumberActiveCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetLoginNumberActiveCommand(id, numberId, IsActive: false), cancellationToken)).ToOk());

        users.MapPost("/{id:guid}/numbers/{numberId:guid}/activate", async Task<Results<Ok<StaffUserDetails>, ProblemHttpResult>> (
                Guid id,
                Guid numberId,
                ICommandHandler<SetLoginNumberActiveCommand, StaffUserDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetLoginNumberActiveCommand(id, numberId, IsActive: true), cancellationToken)).ToOk());
    }
}

public sealed record RenameUserRequest(string? FullName);

public sealed record ChangeUserRoleRequest(Role? Role);

public sealed record ChangeUserMobileNumberRequest(string? MobileNumber);
