using System.Text.Json;
using SlideGenerator.Presentation.Common.Exceptions.Hubs;

namespace SlideGenerator.Presentation.Common.Hubs;

public abstract class Hub : Microsoft.AspNetCore.SignalR.Hub
{
    protected readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    protected T Deserialize<T>(JsonElement message)
    {
        return message.Deserialize<T>(SerializerOptions)
               ?? throw new InvalidRequestFormat(typeof(T).Name);
    }
}