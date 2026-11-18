using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Api.IntegrationTests.Sample;
using Yalfan.Domain.Identity;

namespace Yalfan.Api.IntegrationTests;

public sealed class RequestPipelineTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static CreateSampleCommand Valid(string name = "kif") => new(name, 5, "A1", [new SampleLine(10)]);

    [Fact]
    public async Task A_valid_request_returns_the_result()
    {
        var response = await _client.PostAsJsonAsync("/sample", Valid(), Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SampleResponse>(Cancel);
        Assert.Equal(new SampleResponse("kif", 5), body);
    }

    [Fact]
    public async Task An_invalid_request_returns_a_coded_validation_error_for_each_field()
    {
        var command = new CreateSampleCommand(Name: "", Quantity: 0, Code: "RESERVED", Lines: [new SampleLine(1), new SampleLine(5000)]);

        var response = await _client.PostAsJsonAsync("/sample", command, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await ReadJson(response);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Equal("validation.failed", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));

        var errors = problem.GetProperty("errors").EnumerateArray().ToDictionary(
            error => error.GetProperty("field").GetString()!,
            error => error);

        Assert.Equal(["name", "quantity", "code", "lines[1].quantity"], errors.Keys);

        // One error per field: an empty name is "required", not also "too long".
        Assert.Equal("validation.required", errors["name"].GetProperty("code").GetString());
        Assert.False(errors["name"].TryGetProperty("args", out _));

        Assert.Equal("validation.too_small", errors["quantity"].GetProperty("code").GetString());
        Assert.Equal(0, errors["quantity"].GetProperty("args").GetProperty("min").GetInt32());
        Assert.False(errors["quantity"].GetProperty("args").GetProperty("inclusive").GetBoolean());

        Assert.Equal("sample.code_reserved", errors["code"].GetProperty("code").GetString());

        var line = errors["lines[1].quantity"];
        Assert.Equal("validation.out_of_range", line.GetProperty("code").GetString());
        Assert.Equal(1, line.GetProperty("args").GetProperty("min").GetInt32());
        Assert.Equal(1000, line.GetProperty("args").GetProperty("max").GetInt32());
    }

    [Fact]
    public async Task A_length_error_names_the_limit_and_never_echoes_the_value()
    {
        var tooLong = new string('x', 21);

        var response = await _client.PostAsJsonAsync("/sample", Valid(tooLong), Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync(Cancel);
        Assert.DoesNotContain(tooLong, raw, StringComparison.Ordinal);

        var error = Assert.Single(Parse(raw).GetProperty("errors").EnumerateArray());
        Assert.Equal("validation.too_long", error.GetProperty("code").GetString());
        Assert.Equal(20, error.GetProperty("args").GetProperty("maxLength").GetInt32());
    }

    [Fact]
    public async Task An_invalid_request_never_reaches_the_handler()
    {
        // "boom" makes the handler throw; the validation step must stop the request first.
        var command = Valid("boom") with { Quantity = 0 };

        var response = await _client.PostAsJsonAsync("/sample", command, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("missing", HttpStatusCode.NotFound, "sample.not_found")]
    [InlineData("taken", HttpStatusCode.Conflict, "sample.name_taken")]
    [InlineData("locked", HttpStatusCode.Forbidden, "sample.locked")]
    [InlineData("closed", HttpStatusCode.UnprocessableEntity, "sample.closed")]
    public async Task A_business_failure_returns_its_status_and_code(string name, HttpStatusCode status, string code)
    {
        var response = await _client.PostAsJsonAsync("/sample", Valid(name), Cancel);

        Assert.Equal(status, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("errors", out _));
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task An_unexpected_exception_returns_a_coded_500_without_details()
    {
        var response = await _client.PostAsJsonAsync("/sample", Valid("boom"), Cancel);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync(Cancel);
        Assert.DoesNotContain("secret internal detail", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", raw, StringComparison.Ordinal);

        var problem = Parse(raw);
        Assert.Equal("server.unexpected", problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task A_malformed_body_returns_a_coded_400()
    {
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/sample", content, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("request.malformed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_malformed_body_returns_a_coded_400_also_when_minimal_apis_throw()
    {
        // Development makes minimal APIs throw BadHttpRequestException instead of answering 400;
        // the exception handler must still answer a coded 400, not a 500.
        using var throwing = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true)));
        using var client = throwing.CreateClient();
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/sample", content, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("request.malformed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task An_unknown_route_returns_a_coded_404_to_a_logged_in_user()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.GetAsync("/no-such-route", Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("request.not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task An_unknown_route_returns_401_to_an_anonymous_caller()
    {
        // The closed-by-default policy also covers routes that do not exist, so an outsider
        // cannot map which routes exist.
        var response = await _client.GetAsync("/no-such-route", Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("request.unauthenticated", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_wrong_method_returns_a_coded_405()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.GetAsync("/sample", Cancel);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("request.method_not_allowed", problem.GetProperty("code").GetString());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        Parse(await response.Content.ReadAsStringAsync(Cancel));

    private static JsonElement Parse(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }
}
