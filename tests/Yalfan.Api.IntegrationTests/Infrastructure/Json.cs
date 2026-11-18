using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yalfan.Api.IntegrationTests.Infrastructure;

/// <summary>The API's JSON settings: web defaults plus enums as camelCase names.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };
}
