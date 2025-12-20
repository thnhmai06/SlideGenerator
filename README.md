# SlideGenerator Backend

## Table of contents

- [SlideGenerator Backend](#slidegenerator-backend)
  - [Table of contents](#table-of-contents)
  - [Overview](#overview)
  - [Architecture](#architecture)
  - [Job system](#job-system)
  - [SignalR API](#signalr-api)
  - [Configuration](#configuration)
  - [Development](#development)
  - [Deployment](#deployment)
  - [Framework library](#framework-library)

## Overview

This folder contains the backend services for SlideGenerator.

- Target runtime: .NET 10
- Host: ASP.NET Core + SignalR
- Background jobs: Hangfire
- Layers: Application / Domain / Infrastructure / Presentation

## Architecture

See: [Architecture](docs/en/architecture.md)

## Job system

See: [Job system](docs/en/job-system.md)

## SignalR API

See: [SignalR API](docs/en/signalr.md)

## Configuration

See: [Configuration](docs/en/configuration.md)

## Development

See: [Development](docs/en/development.md)

## Deployment

See: [Deployment](docs/en/deployment.md)

## Framework library

The framework used by this backend is documented here:

- [SlideGenerator.Framework](SlideGenerator.Framework/README.md)
