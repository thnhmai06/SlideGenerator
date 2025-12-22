# Phát triển

## Mục lục

1. [Yêu cầu](#yêu-cầu)
2. [Build](#build)
3. [Run](#run)
4. [Cấu trúc code](#cấu-trúc-code)

## Yêu cầu

- .NET 10 SDK

## Build

Trong `backend/`:

- `dotnet build`

## Run

Trong `backend/SlideGenerator.Presentation/`:

- `dotnet run`

SignalR hubs được host trong project Presentation.

## Cấu trúc code

Bắt đầu từ:

- `SlideGenerator.Presentation/Program.cs`
- `SlideGenerator.Presentation/Hubs/*.cs`

Sau đó theo contract:

- `SlideGenerator.Application/*/Contracts/*.cs`

Xem thêm:

- [Architecture](../en/architecture.md)
- [SignalR API](../en/signalr.md)
