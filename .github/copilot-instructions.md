# SlideGenerator Coding Instructions

## Goal
This document summarizes the conventions currently used across the three main projects:
- SlideGenerator.Domain
- SlideGenerator.Application
- SlideGenerator.Infrastructure

When creating or modifying code, prioritize following the rules below.

## Overall Architecture: Clean Architecture

This solution follows Clean Architecture with clear dependency rules:

Exception:
- Elsa workflow dependencies are allowed in Application as an explicit architectural exception.
- Domain must not depend on Elsa.

1. Domain is the innermost layer.
- Contains business models, business rules, and domain abstractions.
- Must not depend on Application or Infrastructure.
- Should avoid framework-specific technical details whenever possible.

2. Application is the use-case/orchestration layer.
- Depends on Domain.
- Contains orchestration services, registries, and workflow-level logic.
- Defines abstractions that are implemented in Infrastructure (for example serializer, file registry, image compute service).

3. Infrastructure is the implementation/adapters layer.
- May depend on Domain and Application to implement abstractions.
- Contains integrations with external libraries (OpenXml, Spire, OpenCvSharp, ClosedXML, YamlDotNet, Downloader, ...).
- Must not contain core business rules.

## Folder Structure And Naming Conventions

Use consistent naming by module:
- Abstractions: interfaces/contracts (typically start with I).
- Entities: business objects or implementation-level objects with identity/behavior.
- Models: DTOs/value models/preview models.
- Rules: constants, enums, extension mappings, domain rules.
- Services: services for use-case/module processing.
- Activities: workflow activities (Elsa).
- Adapters: wrappers/converters between external libraries and internal abstractions.

Naming interfaces and classes:
- Interface: I + Name (ISettingProvider, IReadOnlyWorkbook, IVisionComputer...).
- Concrete class names should clearly describe their role (YamlSerializer, Cv2Computer, TextReplacer...).
- Prefer sealed for concrete classes when inheritance is not needed.

## C# Coding Conventions

1. Platform/language settings
- Target framework: net10.0.
- Nullable: enable.
- ImplicitUsings: enable.
- File-scoped namespace.

2. General style
- Prefer one top-level type per file (SA1402 is set to suggestion).
- Minimize style noise; focus on clarity and stability.
- Add XML docs for many public APIs (summary/param/returns).
- For any new or modified public API, always add clear XML documentation in English.

3. Data types and models
- Prefer record for data-centric models (identifier/instruction/request).
- Use enum + extension method for technical mappings (for example file extension, document type).
- Use readonly/required/init where appropriate to clarify intent.

4. Asynchrony and resources
- Prefer async/await for I/O or heavy operations.
- Frequently use ConfigureAwait(false) in library/service code.
- Use IDisposable/IAsyncDisposable when holding native resources/streams/models.

5. Input guards and null-safety
- Validate inputs early (null/empty/file exists/valid range).
- Prefer TryGet/Try... patterns for operations that may fail.
- If an operation can fail safely, returning bool/null is preferred over excessive throwing.

6. Concurrency
- Use ConcurrentDictionary/locks when there is shared multi-threaded state.
- For event/callback flows, ensure cleanup state (for example remove items from registry on completion).

## Layer-Specific Rules

### Domain
- Keep domain models/rules simple and business-focused.
- Define abstractions for external capabilities (cloud/image/settings/sheet/slide...).
- Do not include external-library implementation details in Domain.

### Application
- Acts as the bridge for use-cases, workflows, and policies.
- Use registry/provider/service patterns to manage runtime state.
- Avoid pushing framework-specific technical logic too deep when it can be moved to Infrastructure.

### Infrastructure
- Implement Domain/Application abstractions using concrete libraries.
- Prefer adapter pattern to wrap external libraries.
- Place technical handling (format conversion, OpenXml relationships, CV compute, YAML serializer...) here.

## Workflow Conventions (Elsa)

When implementing workflow activities:
- Inherit from WorkflowBase.
- Define Input/Output/Variable with clear names and semantics.
- Build graphs with Sequence/ParallelForEach/Inline in a readable manner.
- Store temporary runtime objects in WorkflowExecutionContext.TransientProperties when needed.
- Validate inputs before I/O operations.

## Error Handling And Logging

- Some places currently use broad catch for fail-safe behavior; when extending, add contextual logging.
- Do not swallow errors silently in business-critical paths.
- Prefer clear, action-oriented error messages.

## Dependency Rule Checklist (Mandatory)

Before merging new code, self-check:
- Does Domain reference Application/Infrastructure? If yes: incorrect.
- Are core business rules placed in Infrastructure? If yes: incorrect.
- Does Application call concrete implementations directly instead of abstractions? If yes: review required.

## When Copilot Generates Code

- Always respect this solution's Clean Architecture dependency rules.
- Keep naming/folder structure aligned with existing modules.
- Prefer defining abstractions in Domain/Application first, then implement in Infrastructure.
- Do not change existing public API/behavior without explicit request.
- If adding a new library: add it only to the project that needs implementation; avoid leaking dependencies into Domain.
