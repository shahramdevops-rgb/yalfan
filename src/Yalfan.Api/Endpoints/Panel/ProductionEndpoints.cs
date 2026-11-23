using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Production;
using Yalfan.Application.Production.AddPieceRate;
using Yalfan.Application.Production.CreateStage;
using Yalfan.Application.Production.GetPieceRateHistory;
using Yalfan.Application.Production.GetRateCard;
using Yalfan.Application.Production.ListStages;
using Yalfan.Application.Production.RenameStage;

namespace Yalfan.Api.Endpoints.Panel;

/// <summary>
/// The shared stage list (ADR 0018) and the rate card (ADR 0021). Every staff role may use
/// them, like the catalog lists, because the operator enters the master data with the rates
/// (roadmap 1.6). There is no delete: work orders will point at stages, and the rate card is
/// append-only.
/// </summary>
public sealed class ProductionEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var production = endpoints.MapGroup("/panel/production").WithTags("Production").RequireAuthorization(Policies.Staff);

        MapStages(production.MapGroup("/stages"));
        MapRates(production.MapGroup("/rates"));
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

    private static void MapRates(RouteGroupBuilder rates)
    {
        // The rates of one model in force on a day (?on=2026-11-23); without it, the workshop's today.
        rates.MapGet("/", async Task<Results<Ok<RateCardDetails>, ProblemHttpResult>> (
                Guid? productId,
                DateOnly? on,
                IQueryHandler<GetRateCardQuery, RateCardDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetRateCardQuery(productId, on), cancellationToken)).ToOk());

        rates.MapGet("/history", async Task<Results<Ok<IReadOnlyList<PieceRateEntry>>, ProblemHttpResult>> (
                Guid? productId,
                Guid? stageId,
                IQueryHandler<GetPieceRateHistoryQuery, IReadOnlyList<PieceRateEntry>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetPieceRateHistoryQuery(productId, stageId), cancellationToken)).ToOk());

        rates.MapPost("/", async Task<Results<Ok<PieceRateEntry>, ProblemHttpResult>> (
                AddPieceRateCommand command,
                ICommandHandler<AddPieceRateCommand, PieceRateEntry> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());
    }
}
