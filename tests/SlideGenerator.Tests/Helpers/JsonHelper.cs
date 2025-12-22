using System.Text.Json;

namespace SlideGenerator.Tests.Helpers;

internal static class JsonHelper
{
    public static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}