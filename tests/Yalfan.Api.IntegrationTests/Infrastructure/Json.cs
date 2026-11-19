using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yalfan.Api.IntegrationTests.Infrastructure;

/// <summary>The API's JSON settings: web defaults, enums as camelCase names, numbers only as numbers.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        NumberHandling = JsonNumberHandling.Strict,
    };
}
