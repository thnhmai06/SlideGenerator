# Ipc Module

The **SlideGenerator.Ipc** module is the executable entry point. It hosts the JSON-RPC 2.0 sidecar consumed by the Tauri frontend and wires every other module through DI.

## Responsibility
- Bootstrap the .NET host, configuration, and system logger.
- Build the DI container with every `Add*Services()` extension.
- Construct a `JsonRpc` instance over `stdin` (incoming) / `stdout` (outgoing) and register all RPC methods.
- Attach the `WorkflowProgressObserver` so workflow lifecycle events become `workflow/progress` notifications.

## Layout
```
SlideGenerator.Ipc/
├── Program.cs                          - Entry point (Main)
├── WelcomeMessages.cs                  - Startup banner
├── Handlers/
│   ├── GeneratingActiveHandler.cs
│   ├── GeneratingCompletedHandler.cs
│   ├── RecipeHandler.cs
│   ├── SettingsHandler.cs
│   ├── SummarizationHandler.cs
│   └── Models/                         - Handler-local DTOs
├── Infrastructure/
│   ├── WorkflowProgressObserver.cs     - Bridges GeneratingEventBus → JsonRpc
│   └── Adapters/                       - STJ converters (RoiOption, RectangleF)
└── Injection/
    └── Registration.cs                 - AddIpcServices()
```

## JsonRpc Setup
- `JsonRpc` is created **after** the DI container is built (it owns the raw stdio streams) and is NOT registered in DI.
- Framing: `NewLineDelimitedMessageHandler` (NDJSON).
- Serialization: `SystemTextJsonFormatter` with the options produced by `BuildJsonSerializerOptions()` — camelCase, `JsonStringEnumConverter`, `RoiOptionJsonAdapter`, `RectangleFJsonAdapter`.
- Methods are bound via `jsonRpc.AddLocalRpcMethod(...)`. A local helper `Attr(name)` constructs `JsonRpcMethodAttribute { UseSingleObjectParameterDeserialization = true }` for handlers that take a single DTO parameter.

## Stream Ownership
| Stream | Owner | Purpose |
|---|---|---|
| stdin | StreamJsonRpc | Incoming requests |
| stdout | StreamJsonRpc | Responses and notifications |
| stderr | Serilog | System logs only |

## Registered Methods
See [IPC API Reference](../IPC-API-Reference.md) for the full table.

## Notifications
Emitted via `JsonRpc.NotifyWithParameterObjectAsync`:
- `workflow/progress` — forwarded from `IGeneratingEventBus` by `WorkflowProgressObserver`.
