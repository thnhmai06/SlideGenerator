# Copilot Instructions

## Source of Truth
- Read and follow [`construction.md`](../construction.md) before making architectural decisions.
- If this file and `construction.md` differ, prefer `construction.md`.
- Validate architecture assumptions against current code in `SlideGenerator.Application`, `SlideGenerator.Domain`, `SlideGenerator.Framework`, and `SlideGenerator.Ipc` before large changes.

## Solution Shape (Current Codebase)
- `SlideGenerator.Framework`: reusable low-level library (Cloud, Sheet, Slide, Image), publishable as standalone package.
- `SlideGenerator.Domain`: domain/runtime concerns (settings, download orchestration, workflow activity models).
- `SlideGenerator.Application`: application-facing services/models (currently scanning models/services).
- `SlideGenerator.Ipc`: stdio JSON-RPC adapter and composition root (`Program.cs`, `Endpoints/RpcEndpoint.*.cs`).

## Layer Boundaries
- Keep `Framework` free of app orchestration and transport concerns.
- Keep `Domain` focused on domain models, state/config access abstractions, and workflow-level activities.
- Keep `Application` focused on use-case services that compose `Domain` + `Framework`.
- Keep `Ipc` thin: validate request payloads, call backend/application orchestration services, return DTOs.
- Do not move business logic into endpoint classes or DTOs.

## DI and Runtime Composition
- Register services in `SlideGenerator.Ipc/Program.cs` using `Microsoft.Extensions.DependencyInjection`.
- Keep singleton config owner in `Domain` (`SettingManager`), and expose read-only access via `ISettingProvider`.
- Prefer constructor injection; avoid manual service wiring inside service constructors.
- Preserve async disposal/lifecycle semantics for long-lived services that hold unmanaged/native resources.

## Contracts Inferred from Code
- Scanning flow should reuse `Framework` extension/services (`ShapeService`, `WorkbookService`, presentation scanners) instead of re-parsing formats.
- Cloud link resolution should go through `Framework.Cloud.Services.CloudResolver` and `Domain.Download.Services.ResolveService`.
- Download orchestration should stay in `Domain.Download.Services` (`DownloadManager`, `FileDownloader`) with concurrency-safe state management.
- Face detection model lifecycle is explicit:
  - initialization and model selection in `Framework.Image.Services.FaceDetectorModelManager`.
  - `FaceDetectionModel.DetectAsync` implementations (for example `YuNetModel`) must throw when model is not initialized.
  - detection results are raw/full; confidence filtering belongs to caller/business layer.

## Coding Guidelines
- Keep changes minimal, scoped, and backward-compatible unless a breaking change is requested.
- Use C# 12+ style already present in repo (file-scoped namespaces, `sealed` where appropriate, async APIs).
- Add XML docs for public types and public methods in touched files.
- Prefer clear, intention-revealing code over compact but opaque expressions.
- Validate inputs with guard clauses and fail fast with explicit exceptions.
- Avoid sync-over-async (`.Result`, `.Wait()`).

## Naming and Data Shape
- Keep DTO/model placement consistent with project responsibilities:
  - transport request contracts under `SlideGenerator.Ipc/Contracts/Requests`.
  - application response models under `SlideGenerator.Application/.../Models`.
  - domain/runtime models under `SlideGenerator.Domain/.../Models` and entities under `.../Entities`.
- Maintain existing naming unless task asks for explicit renaming; avoid incidental churn.

## Change Safety Checklist
- Confirm touched code stays inside the correct project boundary.
- Reuse existing `Framework` and `Domain` helpers before adding new low-level logic.
- Build affected projects first, then build solution when practical.
- If pre-existing compile/runtime issues are found, call them out separately from new changes.
