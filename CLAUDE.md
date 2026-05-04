# CLAUDE.md - SlideGenerator Development Guide

This file provides high-signal guidance for working with the SlideGenerator repository.

## Build Commands

```bash
# Build (uses SlideGenerator.slnx)
dotnet build

# Build release
dotnet build -c Release

# Clean
dotnet clean
```

SDK: .NET 10.0 (`global.json` pins to `latestMajor`).

## Architecture: Modular Monolith

SlideGenerator uses a **Modular Monolith** architecture with independent, composable modules organized by feature and responsibility.

### Module Organization

```
Foundation Modules (no external dependencies)
├── SlideGenerator.Settings      - Configuration management (YAML-based)
└── SlideGenerator.Cloud         - Multi-cloud resolver (Google Drive, OneDrive, SharePoint)

Core Services (depend on Settings/foundational modules)
├── SlideGenerator.Gate          - Concurrency throttling via gates and slots
├── SlideGenerator.Download      - HTTP resource downloading with throttling
└── SlideGenerator.Sheets        - Excel workbook operations (read-only scanning)

Feature Modules
├── SlideGenerator.Images        - Image processing with ROI and face detection (MagickImage-based)
└── SlideGenerator.Slides        - PowerPoint operations (MagickImage-based shape handling)

Orchestration Layer
└── SlideGenerator.Services      - WorkflowCore-based process orchestration
```

### Dependency Principles

- **Isolation**: Each module is independently deployable and testable
- **Clear Contracts**: Modules expose public services via `Registration.cs` for DI setup
- **No Circular Dependencies**: Dependencies flow downward only
- **Layered Communication**: Modules compose via dependency injection, not direct coupling
- **Internal Freedom**: Each module organizes internals via Entities/Models/Services pattern

## Project Modules

### Foundation Modules (Zero External Dependencies)

| **Foundation** | Settings, Cloud | No external dependencies; core infrastructure |
| **Core Services** | Gate, Download, Sheets | Depend on Settings; shared capabilities |
| **Features** | Images, Slides | Domain-specific operations |
| **Orchestration** | Services | Coordinates module composition |

**SlideGenerator.Settings**
- YAML-based configuration system
- Provides `ISettingProvider` for all modules
- DI integration via `SettingsRegistration.AddSettings()`

**SlideGenerator.Cloud**
- Multi-cloud resolver supporting Google Drive, OneDrive, SharePoint
- Cloud resolver implementations for URI resolution
- DI setup via `Registration.AddCloudServices()`

### Core Services (Depend on Settings/Foundation)

**SlideGenerator.Gate**
- Concurrency control with configurable gates per resource type
- Semaphore-based throttling: `GateLocker.LockAsync(gateType)`
- Used by Download, Sheets, and Workflows for resource management

**SlideGenerator.Download**
- HTTP-based resource downloading with throttling
- Progress reporting and error resilience
- Integrates with Gate for concurrency limits
- DI setup via `Registration.AddDownloadServices()`

**SlideGenerator.Sheets**
- Excel workbook read-only scanning via Syncfusion
- Worksheet enumeration and column extraction
- Integrates with Gate for throttling
- DI setup via `Registration.AddSheetServices()`

### Feature Modules

**SlideGenerator.Images**
- Image processing with MagickImage as primary format
- ROI (Region of Interest) calculation with multiple algorithms
- Face detection via OpenCV YuNet model
- Key Services:
  - `RoiResolver` - intelligent crop region calculation
  - `Utilities.Decode()`, `Crop()`, `Resize()` - image operations
  - `ToMat()` / `ToMagickImage()` - format conversion extensions
- DI setup via `Registration.AddImageServices()`

**SlideGenerator.Slides**
- PowerPoint presentation operations via Syncfusion
- Shape preview extraction using MagickImage
- Text placeholder scanning and replacement (Stubble template engine)
- Image composition with multiple input formats
- Key Services:
  - `ImageComposer` - replace shape images
  - `TextComposer` - render mustache templates
  - `Utilities` - shape bounds and preview extraction

### Orchestration Layer

**SlideGenerator.Services**
- WorkflowCore-based process orchestration
- Supports complex workflows: scanning, generating, error handling
- Data-driven persistence via strongly-typed data classes
- Activity-based step execution with DI support
- Integrates all modules for end-to-end automation

## Image Processing (May 2026)

### MagickImage-First Architecture
Both **SlideGenerator.Images** and **SlideGenerator.Slides** now use **ImageMagick's MagickImage** as the primary image type:
- Better format compatibility and conversion
- Consistent image processing pipeline
- Improved memory efficiency
- Cleaner API surface

**Exception**: Face detection in `RoiResolver` uses **OpenCV Mat** internally (converted on-demand from MagickImage).

### Updated APIs

#### SlideGenerator.Images
- `Utilities.Decode(byte[])` → returns `MagickImage`
- `Utilities.Crop(MagickImage, Rectangle)` → returns cropped `MagickImage`
- `Utilities.Resize(MagickImage, Size)` → returns resized `MagickImage`
- `RoiResolver.CalculateRoiAsync()` → accepts `MagickImage` parameter

#### SlideGenerator.Slides
- `Utilities.GetPreviewImage(IShape, byte[])` → returns `MagickImage`
- `Utilities.GetPreviewImage(IShape, MagickImage)` → returns cloned cropped `MagickImage`
- `ImageComposer.Replace()` → supports `MagickImage`, `Stream`, and `byte[]` inputs

## XML Documentation Standard

All public classes, methods, and properties have XML documentation comments:
- **`<summary>`**: Brief description (one sentence when possible)
- **`<remarks>`**: Implementation details, algorithms, performance notes
- **`<param>`**: Parameter descriptions with type expectations
- **`<returns>`**: Return value description
- **`<exception>`**: Thrown exceptions with conditions

## Workflow System (WorkflowCore)

The system uses **WorkflowCore** directly for orchestration.

**Phase-Sequential, Item-Parallel Architecture**:
- Workflows are divided into 3 logical phases (Setup, Resource Prep, Assembly) separated by `ExecutionResult.Next()` barriers.
- **Strict Iteration (MANDATORY)**: Iteration over local data (Workbooks, Presentations, File lists) MUST use WorkflowCore's native `.ForEach()` iterator.
  - **PROHIBITED**: Using C# `foreach`, `Parallel.ForEach`, or `Task.WhenAll` inside an Activity for processing multiple items.
- **Activity Chaining**: Within a `.ForEach` block, chain activities (`.StartWith<X>().Then<Y>()`) so items transition immediately between steps without waiting for the entire collection.

**Data Persistence**:
- Workflows use strongly-typed data classes (e.g., `ScanningData`, `BookTask`).
- State is persisted via these classes; use `ConcurrentDictionary` for parallel safety.
- Normalized absolute file paths are preferred as dictionary keys.

**Slide Generation Mapping Rules**:
- **Text Instruction**: Defines mapping between mustache variables and sheet columns. Variables not in an instruction are considered non-existent in the replacement context.
- **Image Instruction**: Maps sheet columns to slide shapes, including `EditOptions` (ROI, Resizing).

**Error Resilience**:
- All data classes include an `Errors` dictionary storing full `Exception` objects.
- Activities use `try-catch` to capture exceptions, allowing partial success in parallel loops.

## Development Patterns

### Activity (StepBody)
- Inherit from `StepBody` or `StepBodyAsync`.
- **Single Task Focus**: Activities should ideally focus on a single piece of data (mapped from `context.Item`).
- **Throttling**: Inject and use `GateLocker` internally within `RunAsync` to throttle shared resource access (I/O, Workbook reads, Presentation edits).
- Activities are **Singletons** or **Transients** — inject services via constructor.

### Workflow
- Inject `IServiceProvider` or specific services via constructor.
- Workflows MUST have a parameterless constructor for registration.
- Register all Workflows and Activities in the module's `Registration.cs`.

### Locking
- `GateLocker`: semaphore-based concurrency limit per `GateType`.

## Invariants Checklist
- [ ] Each module has a `Registration.cs` with DI setup
- [ ] Module dependencies flow downward only
- [ ] Activities inherit from `StepBody` or `StepBodyAsync`
- [ ] Workflows implement `IWorkflow<TData>`
- [ ] Async code uses `ConfigureAwait(false)`
- [ ] Use `record` for data, `sealed` for logic by default
- [ ] Services are injected via constructor in Activities
- [ ] Image handling uses MagickImage
- [ ] All public APIs have XML documentation comments
