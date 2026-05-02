# SlideGenerator Project Guidelines

## 1. Project Overview
SlideGenerator is an automation tool for generating PowerPoint presentations from structured data and templates. It uses a **Modular Monolith** architecture with independent modules coordinated via WorkflowCore orchestration.

## 2. System Architecture: Modular Monolith

The project uses **Modular Monolith** architecture—a single deployment unit composed of independent, loosely-coupled modules with clear dependency boundaries.

### Module Hierarchy

```
Foundation (Zero Dependencies)
↓
Core Services
↓
Feature Modules
↓
Orchestration
```

| Category | Modules | Responsibility |
|---|---|---|
| **Foundation** | Registry, Settings, Cloud | No external dependencies; core infrastructure |
| **Core Services** | Gate, Download, Sheets | Depend on Settings; shared capabilities |
| **Features** | Images, Slides | Domain-specific operations |
| **Orchestration** | Workflows | Coordinates module composition |

### Architectural Rules

**Mandatory:**
- **No Circular Dependencies**: Module A cannot depend on a module that (directly or transitively) depends on A
- **DI-Based Composition**: Modules register via `Registration.cs`; no service locator pattern
- **Module Independence**: Each module can be understood and tested in isolation
- **Clear Contracts**: All public APIs have XML documentation

**Encouraged:**
- **Data Classes**: Use `record` for DTOs, `sealed record` for strongly-scoped values
- **Service Classes**: Use `sealed class` with dependency-injected constructor
- **Naming**: Keep module names short and intent-driven (e.g., `Gate`, not `ConcurrencyGate`)

## 3. Image Processing (May 2026 Update)

### MagickImage-First Architecture
Both **SlideGenerator.Images** and **SlideGenerator.Slides** now use **ImageMagick's MagickImage** as the primary image type:

- **SlideGenerator.Images**: `RoiResolver`, ROI calculators, and utilities use `MagickImage`
- **SlideGenerator.Slides**: Shape preview extraction and image composition use `MagickImage`

### Image Format Handling

#### Decoding
```csharp
// Convert bytes to MagickImage
var imageBytes = await File.ReadAllBytesAsync("image.jpg");
using var magickImage = Utilities.Decode(imageBytes);
```

#### Common Operations
```csharp
// Crop
using var cropped = Utilities.Crop(magickImage, new Rectangle(100, 100, 200, 200));

// Resize
using var resized = Utilities.Resize(magickImage, new Size(1920, 1080));
```

#### Face Detection (Internal Mat Conversion)
When face detection is needed, MagickImage is temporarily converted to OpenCV Mat:
```csharp
// Extension method - handles conversion internally
using var mat = magickImage.ToMat();
var faces = await faceDetector.DetectAsync(mat);
```

### Key Principles
- Always use `using` statements for MagickImage disposal
- Face detection uses Mat internally (abstracted via RoiCalculator)
- Byte arrays used only at boundaries (file I/O, Syncfusion API)

## 4. XML Documentation Standard

All public classes, methods, properties, and significant private members have XML documentation.

### Required Tags

#### **`<summary>`** (Required)
Brief, single-sentence description of purpose.
```csharp
/// <summary>
///     Calculates the Region of Interest for an image.
/// </summary>
```

#### **`<param>`** (for parameters)
Describe each parameter's purpose and constraints.
```csharp
/// <param name="magickImage">The source image to process.</param>
/// <param name="targetSize">The desired output size.</param>
```

#### **`<returns>`** (for return values)
Describe what is returned and its lifecycle.
```csharp
/// <returns>
///     A new MagickImage with the cropped region.
///     Caller is responsible for disposal.
/// </returns>
```

#### **`<exception>`** (for thrown exceptions)
Document thrown exceptions with conditions.
```csharp
/// <exception cref="ArgumentNullException">Thrown when magickImage is null.</exception>
/// <exception cref="ArgumentOutOfRangeException">Thrown when rectangle is outside image bounds.</exception>
```

#### **`<remarks>`** (for details)
Implementation details, algorithms, performance notes, or usage patterns.
```csharp
/// <remarks>
///     <para>
///         This method performs an in-place rotation. Use Clone() first if you need
///         to preserve the original.
///     </para>
///     <list type="bullet">
///         <item>Optimization: Uses SIMD for best performance</item>
///         <item>Thread-safe: Each operation uses its own buffer</item>
///     </list>
/// </remarks>
```

### Documentation Best Practices
- **Action verbs**: Use imperative mood ("Crops the image", not "Cropping the image")
- **Assume null**: Always document null parameter handling (throw vs. return default)
- **Reference types**: Always mention disposal requirements for disposable types
- **Async methods**: Note whether `ConfigureAwait(false)` is used and if `CancellationToken` is propagated
- **Complex logic**: Explain the "why" in remarks, not just the "what"

### Example: Well-Documented Method
```csharp
/// <summary>
///     Extracts shape preview from slide preview image using intelligent cropping.
/// </summary>
/// <remarks>
///     <para>
///         This method uses EMU-to-pixel conversion (1 pixel = 9525 EMU per Syncfusion).
///         The returned MagickImage is a clone and won't affect the input.
///     </para>
/// </remarks>
/// <param name="shape">The shape to extract preview for. Must not be null.</param>
/// <param name="slidePreviewImage">The full slide preview image. Must not be null.</param>
/// <returns>
///     A cloned MagickImage containing the extracted shape preview.
///     The caller is responsible for disposal via `using` or `Dispose()`.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown if shape or slidePreviewImage is null.</exception>
public static MagickImage GetPreviewImage(this IShape shape, MagickImage slidePreviewImage)
```

## 5. Workflow System (WorkflowCore)
The project uses the **WorkflowCore** engine directly in the Workflows module for orchestrating long-running processes.

### Workflow Definition
Workflows are defined by implementing `IWorkflow<TData>`.
- **`TData`**: A strongly-typed class (e.g., `ScanningData`, `GeneratingData`) used to persist the state of the workflow.
- **`Build` method**: Uses the Fluent API to define the sequence, parallel branches, and loops.

### Throttling (Gates & Slots)
To manage resource-intensive operations (I/O, Network, CPU), the system uses a custom throttling mechanism:
- **`GateType`**: Defines the type of resource being throttled (e.g., `DownloadImage`, `EditPresentation`).
- **`AcquireSlotStep`**: Acquires a permit from the `SlotManager`. If no slots are available, it suspends and waits for a "SlotAvailable" event.
- **`ReleaseSlotStep`**: Releases the permit and triggers the event for waiting workflows.

## 6. Execution State & Persistence
State management is data-driven: all workflow state lives in strongly-typed data classes.

### Data Classes
All workflow state lives in `Services/*/Workflows/*Data.cs` files (or equivalent).
- **Concurrent Access**: Use `ConcurrentDictionary` for state shared across parallel branches.
- **Normalized Paths**: Use absolute, normalized file paths as keys for consistency.

### Error Handling & Resilience
The system prioritizes continuity over stopping on the first failure.
- **`Errors` Dictionary**: Each data class contains a `ConcurrentDictionary<string, Exception> Errors`.
- **Activity Wrapping**: Activities wrap their logic in `try-catch` blocks.
- **Full Exceptions**: Store the entire `Exception` object (including stack traces) for better diagnostics.
- **Partial Success**: The workflow continues to process remaining items (e.g., other rows or files) even if one specific task fails.

## 7. Development Patterns

### Module Registration
Each module provides a static `Registration` class:
```csharp
public static IServiceCollection AddImageServices(this IServiceCollection services)
{
    services.AddSingleton<FaceDetector>(/* factory */);
    services.AddSingleton<RoiResolver>(/* factory */);
    return services;
}
```

Usage:
```csharp
services.AddImageServices();
services.AddSlideServices();
services.AddGateServices();
```

### Activity (StepBody) Model
Activities inherit from `StepBody` (synchronous) or `StepBodyAsync` (asynchronous).
- **Dependency Injection**: Use standard constructor injection for services.
- **Inputs/Outputs**: Map properties in the workflow's `Build` method.
- **PresentationStepBase**: A specialized base class for PowerPoint operations that ensures file leases are always disposed of.

### Workflow Control
`IScanningService` and `IGeneratingService` provide methods to manage active workflows:
- **`Stop`**: Terminates the workflow immediately.
- **`Pause`**: Suspends execution; current state is persisted.
- **`Resume`**: Continues a previously paused workflow.

### Registry & Leasing Pattern
Used for managing access to shared resources (e.g., file streams).
- **FileRegistry<TResource>**: Manages file-backed resource lifecycle with integrated reader-writer locking.
- **Lease<T>**: Returned by `AcquireAsync()`. Ensure `await using` or explicit disposal to prevent file locks.

### Gate Locking (Throttling)
Controlled via `GateWrapper` and `GateLocker`. Limits are configured per `GateType` (e.g., Download, EditImage, Worksheet) to prevent resource exhaustion.
- `GateLocker`: semaphore-based, exposes `LockAsync` only (no read/write distinction).
- `FileLocker`: file-path reader-writer lock, exposes `ReadLockAsync` / `WriteLockAsync`.

### Service Injection
- **Activities**: Registered as **Singletons** — inject all required services via constructor.
- **Workflows**: Inject **only `IServiceProvider`** via constructor. Use it to resolve all required services (e.g., `GateLocker`, `ISettingProvider`, or other modules' services).
- **Workflow Metadata**: Workflows must also have a **parameterless constructor** for registration purposes.

## 8. Technical Standards

### Async/Await
- Always use `ConfigureAwait(false)` in library and module code.
- Propagate `CancellationToken` throughout the call chain.

### Logging
- Use appropriate logging frameworks per module context.
- Avoid direct dependency on framework loggers inside business logic services.

### Coding Style
- **Namespaces**: File-scoped namespaces.
- **Types**: Prefer `record` for DTOs/Value Objects; `sealed` for classes by default.
- **Documentation**: XML documentation is required for all public APIs.
- **Image Format**: Use MagickImage for image processing; convert to/from bytes only at boundaries.

### Invariants Checklist
- [ ] Each module has a `Registration.cs` with DI setup.
- [ ] Module dependencies flow downward only (no circular dependencies).
- [ ] Activities implement `StepBody` or `StepBodyAsync`.
- [ ] Workflows implement `IWorkflow<TData>`.
- [ ] Workflow classes have a parameterless constructor for registration.
- [ ] All async code uses `ConfigureAwait(false)`.
- [ ] Transient resources (leases, handles) are properly disposed.
- [ ] Workflows only inject `IServiceProvider` in their parameterless constructor.
- [ ] Activities inject services via their parameterized constructor.
- [ ] All public APIs have comprehensive XML documentation.
- [ ] Image processing uses MagickImage (exception: face detection uses Mat internally).
- [ ] Module isolation is maintained (testable in isolation).


### Mandatory Architectural Rules:
- **Dependency Inversion**: Application must NOT reference Infrastructure implementations or specific engine libraries (e.g., WorkflowCore).
- **Domain Purity**: The Domain layer must remain free of external framework or library logic.
- **Service/Module Separation**:
    - `Modules/`: Feature-agnostic, reusable capabilities (e.g., Cloud, Download, Resources).
    - `Services/`: High-level feature orchestration that uses multiple modules.
- **Uniform Folder Structure**: Every module/service follows a consistent layout: `Abstractions/`, `Entities/`, `Models/`, `Rules/`, `Services/`, and where applicable, `Activities/` and `Workflows/`.

## 3. Image Processing (May 2026 Update)

### MagickImage-First Architecture
Both **SlideGenerator.Images** and **SlideGenerator.Slides** now use **ImageMagick's MagickImage** as the primary image type:

- **SlideGenerator.Images**: `RoiResolver`, ROI calculators, and utilities use `MagickImage`
- **SlideGenerator.Slides**: Shape preview extraction and image composition use `MagickImage`

### Image Format Handling

#### Decoding
```csharp
// Convert bytes to MagickImage
var imageBytes = await File.ReadAllBytesAsync("image.jpg");
using var magickImage = Utilities.Decode(imageBytes);
```

#### Common Operations
```csharp
// Crop
using var cropped = Utilities.Crop(magickImage, new Rectangle(100, 100, 200, 200));

// Resize
using var resized = Utilities.Resize(magickImage, new Size(1920, 1080));
```

#### Face Detection (Internal Mat Conversion)
When face detection is needed, MagickImage is temporarily converted to OpenCV Mat:
```csharp
// Extension method - handles conversion internally
using var mat = magickImage.ToMat();
var faces = await faceDetector.DetectAsync(mat);
```

### Key Principles
- Always use `using` statements for MagickImage disposal
- Face detection uses Mat internally (abstracted via RoiCalculator)
- Byte arrays used only at boundaries (file I/O, Syncfusion API)
- Extended via ClusterMode comparison across byte-aligned image data

## 4. XML Documentation Standard

All public classes, methods, properties, and significant private members have XML documentation.

### Required Tags

#### **`<summary>`** (Required)
Brief, single-sentence description of purpose.
```csharp
/// <summary>
///     Calculates the Region of Interest for an image.
/// </summary>
```

#### **`<param>`** (for parameters)
Describe each parameter's purpose and constraints.
```csharp
/// <param name="magickImage">The source image to process.</param>
/// <param name="targetSize">The desired output size.</param>
```

#### **`<returns>`** (for return values)
Describe what is returned and its lifecycle.
```csharp
/// <returns>
///     A new MagickImage with the cropped region.
///     Caller is responsible for disposal.
/// </returns>
```

#### **`<exception>`** (for thrown exceptions)
Document thrown exceptions with conditions.
```csharp
/// <exception cref="ArgumentNullException">Thrown when magickImage is null.</exception>
/// <exception cref="ArgumentOutOfRangeException">Thrown when rectangle is outside image bounds.</exception>
```

#### **`<remarks>`** (for details)
Implementation details, algorithms, performance notes, or usage patterns.
```csharp
/// <remarks>
///     <para>
///         This method performs an in-place rotation. Use Clone() first if you need
///         to preserve the original.
///     </para>
///     <list type="bullet">
///         <item>Optimization: Uses SIMD for best performance</item>
///         <item>Thread-safe: Each operation uses its own buffer</item>
///     </list>
/// </remarks>
```

### Documentation Best Practices
- **Action verbs**: Use imperative mood ("Crops the image", not "Cropping the image")
- **Assume null**: Always document null parameter handling (throw vs. return default)
- **Reference types**: Always mention disposal requirements for disposable types
- **Async methods**: Note whether `ConfigureAwait(false)` is used and if `CancellationToken` is propagated
- **Complex logic**: Explain the "why" in remarks, not just the "what"

### Example: Well-Documented Method
```csharp
/// <summary>
///     Extracts shape preview from slide preview image using intelligent cropping.
/// </summary>
/// <remarks>
///     <para>
///         This method uses EMU-to-pixel conversion (1 pixel = 9525 EMU per Syncfusion).
///         The returned MagickImage is a clone and won't affect the input.
///     </para>
/// </remarks>
/// <param name="shape">The shape to extract preview for. Must not be null.</param>
/// <param name="slidePreviewImage">The full slide preview image. Must not be null.</param>
/// <returns>
///     A cloned MagickImage containing the extracted shape preview.
///     The caller is responsible for disposal via `using` or `Dispose()`.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown if shape or slidePreviewImage is null.</exception>
public static MagickImage GetPreviewImage(this IShape shape, MagickImage slidePreviewImage)
```

## 5. Workflow System (WorkflowCore)
The project uses the **WorkflowCore** engine directly in the Application layer for orchestrating long-running processes.

### Workflow Definition
Workflows are defined by implementing `IWorkflow<TData>`.
- **`TData`**: A strongly-typed class (e.g., `ScanningData`, `GeneratingData`) used to persist the state of the workflow.
- **`Build` method**: Uses the Fluent API to define the sequence, parallel branches, and loops.

### Throttling (Gates & Slots)
To manage resource-intensive operations (I/O, Network, CPU), the system uses a custom throttling mechanism:
- **`GateType`**: Defines the type of resource being throttled (e.g., `DownloadImage`, `EditPresentation`).
- **`AcquireSlotStep`**: Acquires a permit from the `SlotManager`. If no slots are available, it suspends and waits for a "SlotAvailable" event.
- **`ReleaseSlotStep`**: Releases the permit and triggers the event for waiting workflows.

## 6. Execution State & Persistence
State management has shifted from a scoped variable system to a direct data persistence model.

### Data Classes
All workflow state lives in `Services/*/Workflows/*Data.cs` files.
- **Concurrent Access**: Use `ConcurrentDictionary` for state shared across parallel branches.
- **Normalized Paths**: Use absolute, normalized file paths as keys for consistency.

### Error Handling & Resilience
The system prioritizes continuity over stopping on the first failure.
- **`Errors` Dictionary**: Each data class contains a `ConcurrentDictionary<string, Exception> Errors`.
- **Activity Wrapping**: Activities wrap their logic in `try-catch` blocks.
- **Full Exceptions**: Store the entire `Exception` object (including stack traces) for better diagnostics.
- **Partial Success**: The workflow continues to process remaining items (e.g., other rows or files) even if one specific task fails.

## 7. Development Patterns

### Activity (StepBody) Model
Activities inherit from `StepBody` (synchronous) or `StepBodyAsync` (asynchronous).
- **Dependency Injection**: Use standard constructor injection for services.
- **Inputs/Outputs**: Map properties in the workflow's `Build` method.
- **PresentationStepBase**: A specialized base class for PowerPoint operations that ensures file leases are always disposed of.

### Workflow Control
`IScanningService` and `IGeneratingService` provide methods to manage active workflows:
- **`Stop`**: Terminates the workflow immediately.
- **`Pause`**: Suspends execution; current state is persisted.
- **`Resume`**: Continues a previously paused workflow.

### Registry & Leasing Pattern
Used for managing access to shared resources (e.g., file streams).
- **FileRegistry<TResource>**: Manages file-backed resource lifecycle with integrated reader-writer locking.
- **Lease<T>**: Returned by `AcquireAsync()`. Ensure `await using` or explicit disposal to prevent file locks.

### Gate Locking (Throttling)
Controlled via `GateWrapper` and `GateLocker`. Limits are configured per `GateType` (e.g., Download, EditImage, Worksheet) to prevent resource exhaustion.
- `GateLocker`: semaphore-based, exposes `LockAsync` only (no read/write distinction).
- `FileLocker`: file-path reader-writer lock, exposes `ReadLockAsync` / `WriteLockAsync`.

### Service Injection
- **Activities**: Registered as **Singletons** — inject all required services via constructor.
- **Workflows**: Inject **only `IServiceProvider`** via constructor. Use it to resolve all required services (e.g., `GateLocker`, `ISettingProvider`, or other Workflows).
- **Workflow Metadata**: Workflows must also have a **parameterless constructor** for registration purposes.

## 8. Technical Standards

### Async/Await
- Always use `ConfigureAwait(false)` in library and service code.
- Propagate `CancellationToken` throughout the call chain.

### Logging
- Use appropriate logging frameworks per module context.
- Avoid direct dependency on framework loggers inside business logic services.

### Coding Style
- **Namespaces**: File-scoped namespaces.
- **Types**: Prefer `record` for DTOs/Value Objects; `sealed` for classes by default.
- **Documentation**: XML documentation is required for all public APIs.
- **Image Format**: Use MagickImage for image processing; convert to/from bytes only at boundaries.

### Invariants Checklist
- [ ] Each module has a `Registration.cs` with DI setup.
- [ ] Module dependencies flow downward only (no circular dependencies).
- [ ] Activities implement `StepBody` or `StepBodyAsync`.
- [ ] Workflows implement `IWorkflow<TData>`.
- [ ] Workflow classes have a parameterless constructor for registration.
- [ ] All async code uses `ConfigureAwait(false)`.
- [ ] Transient resources (leases, handles) are properly disposed.
- [ ] Workflows only inject `IServiceProvider` in their parameterless constructor.
- [ ] Activities inject services via their parameterized constructor.
- [ ] All public APIs have comprehensive XML documentation.
- [ ] Image processing uses MagickImage (exception: face detection uses Mat internally).
- [ ] Module isolation is maintained (testable in isolation).
