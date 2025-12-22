# Development

Vietnamese version: [Vietnamese](../vi/development.md)

## Table of contents

1. [Prerequisites](#prerequisites)
2. [Build](#build)
3. [Run](#run)
4. [Code structure](#code-structure)

## Prerequisites

- .NET 10 SDK

## Build

From `backend/`:

- `dotnet build`

## Run

From `backend/SlideGenerator.Presentation/`:

- `dotnet run`

SignalR hubs are hosted by the presentation project.

## Code structure

Start from:

- `SlideGenerator.Presentation/Program.cs`
- `SlideGenerator.Presentation/Hubs/*.cs`

Then follow contracts:

- `SlideGenerator.Application/*/Contracts/*.cs`

See also:

- [Architecture](architecture.md)
- [SignalR API](signalr.md)
