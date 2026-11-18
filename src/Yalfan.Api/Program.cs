using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Logging;
using Yalfan.Api.Common.Security;
using Yalfan.Application;
using Yalfan.Infrastructure;

// Logs failures that happen before the configured logger exists (bad configuration, a failed
// migration); without it a crash at start leaves no trace. Replaced once the host is built.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Sinks and levels come from the "Serilog" section of appsettings (console always, Seq in
    // Development).
    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    // Enums travel as camelCase names ("operator"), never numbers: reordering an enum must not
    // change what the API means (ADR 0012).
    builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddErrorHandling();
    builder.Services.AddStaffAuthentication();
    builder.Services.AddEndpointModules(typeof(Program).Assembly);

    var app = builder.Build();

    // Outermost, so the logged status is the one the caller really received.
    app.UseRequestLogging();
    app.UseErrorHandling();

    app.UseAuthentication();
    app.UseAuthorization();

    // 200 "Healthy" when the API runs and can reach its database, 503 otherwise.
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapEndpointModules();

    await app.RunAsync();
    return 0;
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is how `dotnet ef` and the test host stop the app on purpose.
    Log.Fatal(exception, "The API stopped because of an unexpected error");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
