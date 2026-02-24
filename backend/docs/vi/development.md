# Phát triển

English version: [English](../en/development.md)

## Build và chạy

Từ thư mục `backend/`:

- Build: `dotnet build`
- Run: `dotnet run --project src/SlideGenerator.Ipc`

## Cấu trúc code

Code chia theo feature ở các layer:

- Presentation: `src/SlideGenerator.Ipc/Features/JsonRpc/*`
- Application: `src/SlideGenerator.Application/Features/*`
- Domain: `src/SlideGenerator.Domain/Features/*`
- Infrastructure: `src/SlideGenerator.Infrastructure/Features/*`

## Điểm vào chính

- `SlideGenerator.Ipc/Program.cs`: host và DI.
- `SlideGenerator.Ipc/Features/JsonRpc/Categories/RpcEndpoint*.cs`: entry point JSON-RPC API.
- `Infrastructure/Features/Jobs`: executor, state store, collections.

## Testing

- Test nằm trong `backend/tests`.
- Chạy bằng `dotnet test`.
