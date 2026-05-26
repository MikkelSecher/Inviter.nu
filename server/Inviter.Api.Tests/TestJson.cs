using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inviter.Api.Tests;

internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
