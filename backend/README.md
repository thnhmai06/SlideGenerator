# SlideGenerator Backend

The robust backend service that powers SlideGenerator, built with **ASP.NET Core 10** and **SignalR**. It handles slide generation logic, job management, and background processing with resilience and performance in mind.

## Table of Contents

- [SlideGenerator Backend](#slidegenerator-backend)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Architecture](#architecture)
  - [Key Systems](#key-systems)
    - [Job System](#job-system)
    - [SignalR API](#signalr-api)
  - [Getting Started](#getting-started)
    - [Configuration](#configuration)
    - [Usage](#usage)
  - [Development Guide](#development-guide)
    - [Development](#development)
    - [Deployment](#deployment)
  - [Framework Library](#framework-library)

## Overview

This directory contains the backend source code, structured as a Clean Architecture solution.

- **Target Runtime:** .NET 10
- **Host:** ASP.NET Core Web API + SignalR
- **Background Jobs:** Hangfire (Persistent job execution)
- **Database:** SQLite (Job state storage)
- **Architectural Pattern:** Clean Architecture (Domain, Application, Infrastructure, Presentation)

## Architecture

The backend is designed to be modular and testable. It strictly separates concerns between the core domain logic and external infrastructure.

👉 **Deep Dive:** [Architecture Documentation](docs/en/architecture.md)

## Key Systems

### Job System

The heart of the application. It manages the lifecycle of slide generation tasks, from parsing Excel files to rendering PowerPoint slides.

- **Features:** Parallel processing, Pause/Resume/Cancel capabilities, Crash recovery.
- **Learn more:** [Job System Documentation](docs/en/job-system.md)

### SignalR API

Real-time bi-directional communication with the Frontend.

- **Protocol:** WebSocket (primary)
- **Features:** Real-time progress updates, Job control commands, Configuration sync.
- **Learn more:** [SignalR API Documentation](docs/en/signalr.md)

## Getting Started

### Configuration

Customize server settings, job concurrency limits, and image processing parameters.

- **Guide:** [Configuration Guide](docs/en/configuration.md)

### Usage

How to run, interact, and troubleshoot the backend service.

- **Guide:** [Usage Guide](docs/en/usage.md)

## Development Guide

### Development

Setup your environment, run the server locally, and run tests.

- **Guide:** [Development Guide](docs/en/development.md)

### Deployment

How to publish and deploy the backend for production (Windows/Linux).

- **Guide:** [Deployment Guide](docs/en/deployment.md)

## Framework Library

The core logic for slide manipulation is abstracted into a reusable framework.

- **Repository:** [SlideGenerator.Framework](../src/SlideGenerator.Framework/README.md)

---

[🇻🇳 Vietnamese Documentation](docs/vi)
