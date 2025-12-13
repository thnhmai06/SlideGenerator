using System.Text.Json;

namespace SlideGenerator.Presentation.Hubs;

public abstract class Hub : Microsoft.AspNetCore.SignalR.Hub
{
    protected readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
}