# Stdio Module

The **SlideGenerator.Stdio** module is the executable entry point. It hosts the JSON-RPC 2.0 sidecar consumed by the
Tauri frontend and wires every other module through DI.

## Responsibility

- Bootstrap the .NET host, configuration, and system logger.
- Build the DI container with every `Add*Services()` extension.
- Construct a `JsonRpc` instance over `stdin` (incoming) / `stdout` (outgoing) and register all RPC methods.
- Attach the `ProgressCoalescer` so Request/Job/Row progress and log lines are coalesced, persisted to `Studio.db`, and
  batched into `progress/request`/`progress/jobs`/`progress/rows`/`log/entries` notifications (≥1s apart).

## Layout

```
SlideGenerator.Stdio/
├── Program.cs                          - Entry point (Main)
├── WelcomeMessages.cs                  - Startup banner
├── Handlers/
│   ├── GeneratingActiveHandler.cs
│   ├── GeneratingCompletedHandler.cs
│   ├── RecipeHandler.cs
│   ├── SettingsHandler.cs
│   ├── SummarizationHandler.cs
│   └── Models/                         - Handler-local DTOs
├── Implementations/
│   ├── GeneratingEventBus.cs           - IEventBus impl: 3 Progress events + AnnounceExpectedJobCount
│   ├── LogNotifier.cs                  - ILogNotifier impl: 1 log-line event
│   ├── ProgressCoalescer.cs            - Coalesces + flushes Progress/Logs → Studio.db + JsonRpc (≥1s)
│   ├── JsonRpcBootstrap.cs             - Builds JsonRpc + JSON serializer options
│   └── Adapters/                       - STJ converters (RoiOption, RectangleF, Vector2)
└── Registration.cs                     - AddIpcServices()
```

## JsonRpc Setup

- `JsonRpc` is created **after** the DI container is built (it owns the raw stdio streams) and is NOT registered in DI.
- Framing: `NewLineDelimitedMessageHandler` (NDJSON).
- Serialization: `SystemTextJsonFormatter` with the options produced by `BuildJsonSerializerOptions()` — camelCase,
  `JsonStringEnumConverter`, `RoiOptionJsonAdapter`, `RectangleFJsonAdapter`.
- Methods are bound via `jsonRpc.AddLocalRpcMethod(...)`. A local helper `Attr(name)` constructs
  `JsonRpcMethodAttribute { UseSingleObjectParameterDeserialization = true }` for handlers that take a single DTO
  parameter.

## Stream Ownership

| Stream | Owner         | Purpose                     |
|--------|---------------|-----------------------------|
| stdin  | StreamJsonRpc | Incoming requests           |
| stdout | StreamJsonRpc | Responses and notifications |
| stderr | Serilog       | System logs only            |

## Registered Methods

See [IPC API Reference](../IPC-API-Reference.md) for the full table.

## Notifications

Emitted via `JsonRpc.NotifyAsync(method, payload)` — **not** `NotifyWithParameterObjectAsync`, which marshals a single
argument by reflecting over its public properties and would serialize a `List<T>` batch as an empty `{}`
instead of an array. Each notification's `params` is therefore a single positional array (`params[0]`), batched by
`ProgressCoalescer` at most once per second:

- `progress/request` — array of `RequestProgress`.
- `progress/jobs` — array of `JobProgress`.
- `progress/rows` — array of `RowProgress`.
- `log/entries` — array of `LogEntry`, append-only (never coalesced/dropped, unlike the 3 above).

See [IPC API Reference](../IPC-API-Reference.md) for full payload shapes.
