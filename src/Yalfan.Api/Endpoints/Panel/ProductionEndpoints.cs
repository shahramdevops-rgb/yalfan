using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Production;
using Yalfan.Application.Production.CreateStage;
using Yalfan.Application.Production.ListStages;
using Yalfan.Application.Production.RenameStage;

namespace Yalfan.Api.Endpoints.Panel;

/// <summary>
/// The shared stage list (ADR 0018). Every staff role may use it, like the catalog lists,
/// because the operator enters the master data (roadmap 1.6). There is no delete: work orders
/// will point at stages.
/// </summary>
public sealed class ProductionEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var production = endpoints.MapGroup("/panel/production").WithTags("Production").RequireAuthorization(Policies.Staff);

        MapStages(production.MapGroup("/stages"));
    }

    private static void MapStages(RouteGroupBuilder stages)
    {
        stages.MapGet("/", async Task<Results<Ok<IReadOnlyList<StageSummary>>, ProblemHttpResult>> (
                IQueryHandler<ListStagesQuery, IReadOnlyList<StageSummary>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListStagesQuery(), cancellationToken)).ToOk());

        stages.MapPost("/", async Task<Results<Ok<StageSummary>, ProblemHttpResult>> (
                CreateStageCommand command,
                ICommandHandler<CreateStageCommand, StageSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        stages.MapPut("/{id:guid}", async Task<Results<Ok<StageSummary>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<RenameStageCommand, StageSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameStageCommand(id, request.Name), cancellationToken)).ToOk());
    }
}
