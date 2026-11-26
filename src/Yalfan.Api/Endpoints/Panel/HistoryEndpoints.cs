using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.History;
using Yalfan.Application.History.GetContractorHistory;
using Yalfan.Application.History.GetModelHistory;
using Yalfan.Application.History.GetWorkerHistory;

namespace Yalfan.Api.Endpoints.Panel;

/// <summary>
/// The owner's change history of a model, a contractor and a worker, read from the audit
/// (ADR 0013, ADR 0027). Owner only, on the whole group, so a route added here later cannot be
/// left open by forgetting it. Read only: nothing here writes, and the audit cannot be changed.
/// Twenty saves a page, newest first; <c>?before=</c> takes the <c>before</c> of the last page.
/// </summary>
public sealed class HistoryEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var history = endpoints.MapGroup("/panel/history").WithTags("History").RequireAuthorization(Policies.Owner);

        history.MapGet("/models/{id:guid}", async Task<Results<Ok<HistoryPage>, ProblemHttpResult>> (
                Guid id,
                DateTimeOffset? before,
                IQueryHandler<GetModelHistoryQuery, HistoryPage> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetModelHistoryQuery(id, before), cancellationToken)).ToOk());

        history.MapGet("/contractors/{id:guid}", async Task<Results<Ok<HistoryPage>, ProblemHttpResult>> (
                Guid id,
                DateTimeOffset? before,
                IQueryHandler<GetContractorHistoryQuery, HistoryPage> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetContractorHistoryQuery(id, before), cancellationToken)).ToOk());

        history.MapGet("/workers/{id:guid}", async Task<Results<Ok<HistoryPage>, ProblemHttpResult>> (
                Guid id,
                DateTimeOffset? before,
                IQueryHandler<GetWorkerHistoryQuery, HistoryPage> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetWorkerHistoryQuery(id, before), cancellationToken)).ToOk());
    }
}
