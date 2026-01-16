using System.Text.Json;
using System.Text.Json.Serialization;
using SlideGenerator.Presentation.Common.Exceptions.Hubs;

namespace SlideGenerator.Presentation.Common.Hubs;

public abstract class Hub : Microsoft.AspNetCore.SignalR.Hub
{
    protected static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    protected T Deserialize<T>(JsonElement message)
    {
        return message.Deserialize<T>(SerializerOptions)
               ?? throw new InvalidRequestFormat(typeof(T).Name);
    }
}