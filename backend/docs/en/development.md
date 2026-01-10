# Development

Vietnamese version: [Vietnamese](../vi/development.md)

## Build and run

From `backend/`:

- Build: `dotnet build`
- Run: `dotnet run --project src/SlideGenerator.Presentation`

## Code structure

Feature-based slices live across layers:

- Presentation: `src/SlideGenerator.Presentation/Features/*/*Hub.cs`
- Application: `src/SlideGenerator.Application/Features/*`
- Domain: `src/SlideGenerator.Domain/Features/*`
- Infrastructure: `src/SlideGenerator.Infrastructure/Features/*`

## Key entry points

- `SlideGenerator.Presentation/Program.cs`: host setup and DI wiring.
- `Presentation/Features/Tasks/TaskHub.cs`: task API entry.
- `Infrastructure/Features/Jobs`: Hangfire executor, state store, collections.

## Testing

- Tests live under `backend/tests`.
- Use `dotnet test` to run the suite.
