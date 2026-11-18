using Microsoft.Extensions.DependencyInjection;
using Yalfan.Api.IntegrationTests.Sample;
using Yalfan.Application.Common.Handlers;

namespace Yalfan.Api.IntegrationTests;

public sealed class UseCaseRegistrationTests
{
    [Fact]
    public void A_resolved_handler_is_wrapped_in_the_pipeline()
    {
        using var provider = new ServiceCollection()
            .AddUseCases(typeof(CreateSampleHandler).Assembly)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateSampleCommand, SampleResponse>>();

        Assert.IsNotType<CreateSampleHandler>(handler);
    }

    [Fact]
    public void Registering_the_same_handlers_twice_stops_the_start()
    {
        var services = new ServiceCollection().AddUseCases(typeof(CreateSampleHandler).Assembly);

        Assert.Throws<InvalidOperationException>(() => services.AddUseCases(typeof(CreateSampleHandler).Assembly));
    }
}
