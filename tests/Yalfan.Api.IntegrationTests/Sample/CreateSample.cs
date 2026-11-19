using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Api.IntegrationTests.Sample;

// A use case that exists only in the tests, so the pipeline can be proven without putting a
// fake feature in the product. The handler's reaction is chosen by the name it receives.

public sealed record CreateSampleCommand(string? Name, int Quantity, string? Code, IReadOnlyList<SampleLine>? Lines);

public sealed record SampleLine(int Quantity);

public sealed record SampleResponse(string Name, int Quantity);

public sealed class CreateSampleValidator : AbstractValidator<CreateSampleCommand>
{
    public CreateSampleValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Quantity).GreaterThan(0);
        RuleFor(command => command.Code).Must(code => code != "RESERVED").WithErrorCode("sample.code_reserved");
        RuleForEach(command => command.Lines).ChildRules(line =>
            line.RuleFor(item => item.Quantity).InclusiveBetween(1, 1000));
    }
}

public sealed class CreateSampleHandler : ICommandHandler<CreateSampleCommand, SampleResponse>
{
    public Task<Result<SampleResponse>> Handle(CreateSampleCommand command, CancellationToken cancellationToken)
    {
        Result<SampleResponse> result = command.Name switch
        {
            "missing" => Error.NotFound("sample.not_found"),
            "taken" => Error.Conflict("sample.name_taken"),
            "locked" => Error.Forbidden("sample.locked"),
            "closed" => Error.Rule("sample.closed"),
            "boom" => throw new InvalidOperationException("secret internal detail"),
            _ => new SampleResponse(command.Name!, command.Quantity),
        };

        return Task.FromResult(result);
    }
}

public sealed class SampleEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/sample", async Task<Results<Ok<SampleResponse>, ProblemHttpResult>> (
            CreateSampleCommand command,
            ICommandHandler<CreateSampleCommand, SampleResponse> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken)).ToOk())
            .AllowAnonymous();
    }
}
