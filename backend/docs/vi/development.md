# Phát triển

English version: [English](../en/development.md)

## Build và chạy

Từ thư mục `backend/`:

- Build: `dotnet build`
- Run: `dotnet run --project src/SlideGenerator.Presentation`

## Cấu trúc code

Code chia theo feature ở các layer:

- Presentation: `src/SlideGenerator.Presentation/Features/*/*Hub.cs`
- Application: `src/SlideGenerator.Application/Features/*`
- Domain: `src/SlideGenerator.Domain/Features/*`
- Infrastructure: `src/SlideGenerator.Infrastructure/Features/*`

## Điểm vào chính

- `SlideGenerator.Presentation/Program.cs`: host và DI.
- `Presentation/Features/Tasks/TaskHub.cs`: API task.
- `Infrastructure/Features/Jobs`: executor, state store, collections.

## Testing

- Test nằm trong `backend/tests`.
- Chạy bằng `dotnet test`.
