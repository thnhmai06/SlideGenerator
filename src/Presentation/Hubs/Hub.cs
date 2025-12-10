using System.Text.Json;

namespace Presentation.Hubs;

public abstract class Hub : Microsoft.AspNetCore.SignalR.Hub
{
    protected readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
}