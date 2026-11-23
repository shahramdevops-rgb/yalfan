using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Paging;
using Yalfan.Application.Parties;
using Yalfan.Application.Parties.ChangeContractorIban;
using Yalfan.Application.Parties.ChangeWorkerIban;
using Yalfan.Application.Parties.CreateContractor;
using Yalfan.Application.Parties.CreateWorker;
using Yalfan.Application.Parties.GetContractor;
using Yalfan.Application.Parties.GetContractorIban;
using Yalfan.Application.Parties.GetWorker;
using Yalfan.Application.Parties.GetWorkerIban;
using Yalfan.Application.Parties.RemoveContractorIban;
using Yalfan.Application.Parties.RemoveWorkerIban;
using Yalfan.Application.Parties.SearchContractors;
using Yalfan.Application.Parties.SearchWorkers;
using Yalfan.Application.Parties.SetContractorActive;
using Yalfan.Application.Parties.SetWorkerActive;
using Yalfan.Application.Parties.UpdateContractor;
using Yalfan.Application.Parties.UpdateWorker;

namespace Yalfan.Api.Endpoints.Panel;

/// <summary>
/// Contractors (ADR 0019) and in-house workers (ADR 0020). Every staff role may use them, because
/// the operator enters them from the paper books; only the owner may read a whole IBAN. There is
/// no delete, only deactivation: work orders, attendance and accounts will point at them.
/// </summary>
public sealed class PartiesEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var parties = endpoints.MapGroup("/panel/parties").WithTags("Parties").RequireAuthorization(Policies.Staff);

        MapContractors(parties.MapGroup("/contractors"));
        MapWorkers(parties.MapGroup("/workers"));
    }

    private static void MapContractors(RouteGroupBuilder contractors)
    {
        contractors.MapGet("/", async Task<Results<Ok<PagedList<ContractorListItem>>, ProblemHttpResult>> (
                string? search,
                bool? isActive,
                int? page,
                int? pageSize,
                IQueryHandler<SearchContractorsQuery, PagedList<ContractorListItem>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SearchContractorsQuery(search, isActive, page, pageSize), cancellationToken)).ToOk());

        contractors.MapPost("/", async Task<Results<Ok<ContractorDetails>, ProblemHttpResult>> (
                CreateContractorCommand command,
                ICommandHandler<CreateContractorCommand, ContractorDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        contractors.MapGet("/{id:guid}", async Task<Results<Ok<ContractorDetails>, ProblemHttpResult>> (
                Guid id,
                IQueryHandler<GetContractorQuery, ContractorDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetContractorQuery(id), cancellationToken)).ToOk());

        contractors.MapPut("/{id:guid}", async Task<Results<Ok<ContractorDetails>, ProblemHttpResult>> (
                Guid id,
                UpdateContractorRequest request,
                ICommandHandler<UpdateContractorCommand, ContractorDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(
                    new UpdateContractorCommand(id, request.FullName, request.MobileNumber, request.StageIds, request.Notes),
                    cancellationToken)).ToOk());

        contractors.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<ContractorDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetContractorActiveCommand, ContractorDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetContractorActiveCommand(id, IsActive: false), cancellationToken)).ToOk());

        contractors.MapPost("/{id:guid}/activate", async Task<Results<Ok<ContractorDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetContractorActiveCommand, ContractorDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetContractorActiveCommand(id, IsActive: true), cancellationToken)).ToOk());

        MapContractorIban(contractors.MapGroup("/{id:guid}/iban"));
    }

    private static void MapContractorIban(RouteGroupBuilder iban)
    {
        // Decided 1405/09/02: only the owner sees the whole number; he pays by hand until Phase 15.
        iban.MapGet("/", async Task<Results<Ok<ContractorIbanResponse>, ProblemHttpResult>> (
                Guid id,
                IQueryHandler<GetContractorIbanQuery, ContractorIbanResponse> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetContractorIbanQuery(id), cancellationToken)).ToOk())
            .RequireAuthorization(Policies.Owner);

        iban.MapPut("/", async Task<Results<Ok<ContractorDetails>, ProblemHttpResult>> (
                Guid id,
                ChangeIbanRequest request,
                ICommandHandler<ChangeContractorIbanCommand, ContractorDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ChangeContractorIbanCommand(id, request.Iban), cancellationToken)).ToOk());

        iban.MapDelete("/", async Task<Results<Ok<ContractorDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<RemoveContractorIbanCommand, ContractorDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RemoveContractorIbanCommand(id), cancellationToken)).ToOk());
    }

    private static void MapWorkers(RouteGroupBuilder workers)
    {
        workers.MapGet("/", async Task<Results<Ok<PagedList<WorkerListItem>>, ProblemHttpResult>> (
                string? search,
                bool? isActive,
                int? page,
                int? pageSize,
                IQueryHandler<SearchWorkersQuery, PagedList<WorkerListItem>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SearchWorkersQuery(search, isActive, page, pageSize), cancellationToken)).ToOk());

        workers.MapPost("/", async Task<Results<Ok<WorkerDetails>, ProblemHttpResult>> (
                CreateWorkerCommand command,
                ICommandHandler<CreateWorkerCommand, WorkerDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        workers.MapGet("/{id:guid}", async Task<Results<Ok<WorkerDetails>, ProblemHttpResult>> (
                Guid id,
                IQueryHandler<GetWorkerQuery, WorkerDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetWorkerQuery(id), cancellationToken)).ToOk());

        workers.MapPut("/{id:guid}", async Task<Results<Ok<WorkerDetails>, ProblemHttpResult>> (
                Guid id,
                UpdateWorkerRequest request,
                ICommandHandler<UpdateWorkerCommand, WorkerDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(
                    new UpdateWorkerCommand(id, request.FullName, request.MobileNumber, request.StageIds, request.StartDate, request.Notes),
                    cancellationToken)).ToOk());

        workers.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<WorkerDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetWorkerActiveCommand, WorkerDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetWorkerActiveCommand(id, IsActive: false), cancellationToken)).ToOk());

        workers.MapPost("/{id:guid}/activate", async Task<Results<Ok<WorkerDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetWorkerActiveCommand, WorkerDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetWorkerActiveCommand(id, IsActive: true), cancellationToken)).ToOk());

        MapWorkerIban(workers.MapGroup("/{id:guid}/iban"));
    }

    private static void MapWorkerIban(RouteGroupBuilder iban)
    {
        // As for contractors: only the owner sees the whole number (ADR 0020).
        iban.MapGet("/", async Task<Results<Ok<WorkerIbanResponse>, ProblemHttpResult>> (
                Guid id,
                IQueryHandler<GetWorkerIbanQuery, WorkerIbanResponse> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetWorkerIbanQuery(id), cancellationToken)).ToOk())
            .RequireAuthorization(Policies.Owner);

        iban.MapPut("/", async Task<Results<Ok<WorkerDetails>, ProblemHttpResult>> (
                Guid id,
                ChangeIbanRequest request,
                ICommandHandler<ChangeWorkerIbanCommand, WorkerDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ChangeWorkerIbanCommand(id, request.Iban), cancellationToken)).ToOk());

        iban.MapDelete("/", async Task<Results<Ok<WorkerDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<RemoveWorkerIbanCommand, WorkerDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RemoveWorkerIbanCommand(id), cancellationToken)).ToOk());
    }
}

/// <summary>The body of saving a contractor's profile; his id is in the path. It has no IBAN (ADR 0019).</summary>
public sealed record UpdateContractorRequest(string? FullName, string? MobileNumber, IReadOnlyList<Guid>? StageIds, string? Notes);

/// <summary>The body of saving a worker's profile; the id is in the path. It has no IBAN (ADR 0019).</summary>
public sealed record UpdateWorkerRequest(
    string? FullName,
    string? MobileNumber,
    IReadOnlyList<Guid>? StageIds,
    DateOnly? StartDate,
    string? Notes);

/// <summary>The body of setting or replacing an IBAN.</summary>
public sealed record ChangeIbanRequest(string? Iban);
