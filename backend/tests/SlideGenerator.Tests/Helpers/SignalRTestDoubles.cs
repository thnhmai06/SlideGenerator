using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace SlideGenerator.Tests.Helpers;

internal sealed class CaptureClientProxy : IClientProxy
{
    public string? Method { get; private set; }
    public object?[]? Args { get; private set; }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Method = method;
        Args = args;
        return Task.CompletedTask;
    }

    public T? GetPayload<T>()
    {
        if (Args == null || Args.Length == 0)
            return default;
        return (T)Args[0]!;
    }
}

internal sealed class TestHubCallerClients(IClientProxy caller) : IHubCallerClients
{
    public IClientProxy Caller { get; } = caller;
    public IClientProxy Others => throw new NotSupportedException();
    public IClientProxy OthersInGroup(string groupName) => throw new NotSupportedException();
    public IClientProxy All => throw new NotSupportedException();
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    public IClientProxy Client(string connectionId) => throw new NotSupportedException();
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
    public IClientProxy Group(string groupName) => throw new NotSupportedException();
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
    public IClientProxy User(string userId) => throw new NotSupportedException();
    public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
}

internal sealed class TestHubCallerContext(string connectionId) : HubCallerContext
{
    public override string ConnectionId { get; } = connectionId;
    public override string? UserIdentifier { get; }
    public override ClaimsPrincipal? User { get; }
    private readonly IDictionary<object, object?> _items = new Dictionary<object, object?>();
    public override IDictionary<object, object?> Items => _items;
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted { get; }
    public override void Abort()
    {
    }
}

internal sealed class TestGroupManager : IGroupManager
{
    public List<(string ConnectionId, string GroupName)> Added { get; } = [];
    public List<(string ConnectionId, string GroupName)> Removed { get; } = [];

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Added.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Removed.Add((connectionId, groupName));
        return Task.CompletedTask;
    }
}

internal static class HubTestHelper
{
    public static CaptureClientProxy Attach(
        Microsoft.AspNetCore.SignalR.Hub hub,
        string connectionId,
        TestGroupManager? groupManager = null)
    {
        var proxy = new CaptureClientProxy();
        var clients = new TestHubCallerClients(proxy);
        var context = new TestHubCallerContext(connectionId);

        SetHubProperty(hub, "Clients", clients);
        SetHubProperty(hub, "Context", context);
        if (groupManager != null)
            SetHubProperty(hub, "Groups", groupManager);

        return proxy;
    }

    private static void SetHubProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().BaseType?.GetProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Hub property '{propertyName}' not found.");
        property.SetValue(target, value);
    }
}
