# Copilot Instructions

## Source of Truth
- Read and follow [Constructon](../construction.md) before making architectural decisions.
- If `copilot-instructions.md` and `Constructon` differ, prefer `Constructon` for project-specific architecture/runtime rules.

## General Guidelines
- Keep changes minimal and scoped to the requested feature.
- Prefer fixing root causes over adding temporary workarounds.
- Do not introduce unrelated refactors while implementing a task.
- Keep public APIs stable unless the task explicitly requests breaking changes.

## Code Style
- Use C# 12+ style already used in this repo (`sealed`, file-scoped namespaces, explicit async APIs).
- Use meaningful names; avoid one-letter variables except in trivial loops.
- Add XML doc comments for public types and public methods in touched files.
- Prefer expression clarity over clever code.
- Preserve existing indentation and formatting conventions.
- Keep method bodies short and intention-revealing; extract private helpers when a method handles multiple concerns.
- Validate external inputs early (guard clauses) and fail fast with explicit exception types.
- Prefer `async`/`await` end-to-end for I/O paths; avoid sync-over-async patterns (`.Result`, `.Wait()`).
- Return structured results/models instead of loosely typed objects or magic dictionaries.
- Use `ILogger<T>` for operational logs; keep logs concise, contextual, and free of sensitive data.
- Avoid hidden side effects: methods should do what their names describe and keep state transitions explicit.
- Follow object-oriented design by default:
	- Encapsulate behavior in classes/services instead of top-level script style.
	- Keep methods focused and single-purpose; extract private helper methods when logic grows.
	- Prefer dependency inversion (interfaces/contracts) for cross-project dependencies.
	- Keep mutable state private and expose minimal public surface.

## Project-Specific Rules
- Keep architecture boundaries strict:
	- `Framework`: reusable low-level features (slide/sheet/image/cloud services), no app orchestration. Can be published as NuGet.
	- `Features`: domain entities/models (Configs, Jobs orchestration with persistence/workflow, Slides/Sheets domain models).
	- `Services`: application services (ScanService, GenerateService, DownloadService, FaceDetectorModelManager, ValidationService).
	- `Ipc`: JSON-RPC transport adapter.
- Configuration access for non-`Features` projects currently uses `IConfigProvider` mapping from singleton `ConfigManager` (in `Features.Configs`).
	- Register `ConfigManager` once in DI and map provider interfaces from it.
	- Avoid passing raw `Config` as root dependency unless taking a runtime snapshot in an internal service.
- Prefer Dependency Injection from `Program.cs` (`Microsoft.Extensions.DependencyInjection`).
	- Avoid manual `new` for service wiring in constructors.
- Face detection lifecycle contract:
	- Model initialization ownership is in `FaceDetectorModelManager` (in `Services`).
	- In `Framework`, `DetectAsync` must throw when model is not initialized.
	- `Framework` returns all detections; score filtering is handled by caller/business layer.
- Download behavior:
	- Use `DownloadService` (in `Services.Generating`, uses Downloader library) for remote downloads in Generate flow.
	- Avoid direct ad-hoc `HttpClient` download logic in generation pipeline.
- IPC endpoint structure:
	- Keep request DTO in `Services.Generating.Models` (for GenerateSlidesRequest) or `SlideGenerator.Ipc/Contracts/Requests`.
	- Keep RPC handlers in partial `RpcEndpoint.*.cs` files and call `BackendService` (in `Features.Jobs`) for orchestration.
- Framework reuse:
	- If logic already exists in `Framework` services, use it instead of duplicating logic in `Services` or `Features`.
- Data shape conventions:
	- Use `Entities` for domain/runtime entities (in `Features`).
	- Use `Models` for supporting option/value model types (in `Features` or `Services`).
- **Current architecture**: Solution organized into 4 projects:
	- `Framework` (low-level reusable library)
	- `Features` (domain entities, Jobs, Configs, domain models)
	- `Services` (application services for scan/generate/download/validation)
	- `Ipc` (JSON-RPC transport layer)