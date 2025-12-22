# Usage

Vietnamese version: [Vietnamese](../vi/usage.md)

## Table of contents

1. [Prerequisites](#prerequisites)
2. [Run the backend](#run-the-backend)
3. [Verify](#verify)
4. [Connect from the frontend](#connect-from-the-frontend)
5. [API examples](#api-examples)

## Prerequisites

- .NET 10 SDK or runtime
- Valid backend config (`backend.config.yaml`)

## Run the backend

From `backend/SlideGenerator.Presentation/`:

```
dotnet run
```

The server binds to the host/port defined in config and exposes SignalR hubs.

## Verify

- Open `/hangfire` on localhost to check background jobs.
- The backend is offline-only and listens on localhost/127.0.0.1.

## Connect from the frontend

- Set the backend URL in the frontend Settings page.
- The frontend connects to `/hubs/slide`, `/hubs/sheet`, and `/hubs/config`.

## API examples

See [SignalR API](signalr.md#examples) for sample requests and subscriptions.
