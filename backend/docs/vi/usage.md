# Huong dan su dung

## Muc luc

1. [Yeu cau](#yeu-cau)
2. [Chay backend](#chay-backend)
3. [Kiem tra](#kiem-tra)
4. [Ket noi tu frontend](#ket-noi-tu-frontend)
5. [Vi du API](#vi-du-api)

## Yeu cau

- .NET 10 SDK hoac runtime
- File cau hinh hop le (`backend.config.yaml`)

## Chay backend

Trong `backend/SlideGenerator.Presentation/`:

```
dotnet run
```

Server bind theo host/port trong config va mo SignalR hubs.

## Kiem tra

- Mo `/hangfire` tren localhost de xem job.
- Backend chay offline va chi lang nghe localhost/127.0.0.1.

## Ket noi tu frontend

- Thiet lap URL backend trong trang Cai dat cua frontend.
- Frontend ket noi toi `/hubs/slide`, `/hubs/sheet`, va `/hubs/config`.

## Vi du API

Xem [SignalR API](signalr.md#vi-du) de tham khao request va subscribe.
