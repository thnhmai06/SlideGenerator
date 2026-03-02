# Development

Vietnamese version: [Vietnamese](../vi/development.md)

## Build and run

From `backend/`:

- Build: `dotnet build`
- Run: `dotnet run --project src/SlideGenerator.Ipc`

## Code structure

Feature-based slices live across layers:

- Presentation: `src/SlideGenerator.Ipc/Features/*/*Hub.cs`
- Presentation: `src/SlideGenerator.Ipc/Features/JsonRpc/*`
- Application: `src/SlideGenerator.Application/Features/*`
- Domain: `src/SlideGenerator.Domain/Features/*`
- Infrastructure: `src/SlideGenerator.Infrastructure/Features/*`

## Key entry points

- `SlideGenerator.Ipc/Program.cs`: host setup and DI wiring.
- `SlideGenerator.Ipc/Features/JsonRpc/Categories/RpcEndpoint*.cs`: JSON-RPC API entry points.
- `Infrastructure/Features/Jobs`: Hangfire executor, state store, collections.

## Testing

- Tests live under `backend/tests`.
- Use `dotnet test` to run the suite.
