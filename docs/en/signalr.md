# SignalR API

## Table of contents

1. [Endpoints](#endpoints)
2. [Request/response pattern](#requestresponse-pattern)
3. [Slide hub messages](#slide-hub-messages)
4. [Notifications](#notifications)

## Endpoints

The backend exposes SignalR hubs:

- `/hubs/slide`
- `/hubs/sheet`
- `/hubs/config`

## Request/response pattern

Clients send a JSON message to `ProcessRequest`.

- The message includes a `type` field.
- The hub responds via `ReceiveResponse`.

## Slide hub messages

See code: `SlideGenerator.Presentation/Hubs/SlideHub.cs`.

Common message types:

- `ScanShapes`
- `GroupCreate`
- `GroupStatus`
- `GroupControl`
- `JobStatus`
- `JobControl`
- `GlobalControl`
- `GetAllGroups`

## Notifications

Notifications are broadcast to all clients via `ReceiveNotification`.

Core notifications:

- Job progress
- Job status
- Job error
- Group progress
- Group status

See also:

- [Job system](job-system.md)
- [Architecture](architecture.md)
