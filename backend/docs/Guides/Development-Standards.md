# Development Standards

This guide outlines the mandatory coding standards and conventions for the SlideGenerator backend. Consistency across
modules is critical for the Modular Monolith architecture.

## 1. C# Language Features

We use **.NET 10.0** and take advantage of modern C# features:

- **C# 12 Primary Constructors**: Use for dependency injection in services.
- **C# 14 Extension Members**: Use for DI registration (`Registration.cs`) and utility classes.
- **File-Scoped Namespaces**: Required for all files.
- **Records**: Use for DTOs, value objects, and workflow context data.

## 2. Folder Structure

Every module MUST follow this consistent layout:

- `Domain/`: Models, Enums, and Abstractions (interfaces) owned by the business logic.
- `Application/`: Services and Step bodies.
- `Infrastructure/`: Concrete implementations (Adapters, SQL, HTTP).
- `Injection/`: `Registration.cs` for DI setup.

## 3. Dependency Injection Rules

- **Registration**: Each module provides an extension method `Add[ModuleName]Services(this IServiceCollection services)`
  declared inside `extension(IServiceCollection services)` (C# 14 extension member syntax) where applicable. Examples:
  `AddSettingsServices()`, `AddCloudServices()`, `AddLoggingServices(IConfiguration?)`, `AddGeneratorServices()`,
  `AddIpcServices()`.
- **Constructor Injection**: Always favor constructor injection over property injection.
- **Lifetimes**:
    - Stateless services: `Singleton` or `Transient`.
    - Workflow Steps: `Transient`.
    - Resource Handles: Managed manually or via scoped DI if appropriate.

## 4. Async/Await Patterns

- **ConfigureAwait(false)**: Mandatory for all library/module code.
- **CancellationToken**: Must be propagated through every async method call.
- **Task Overloads**: Always provide `Async` suffix for methods returning `Task`.

## 5. Image Handling (MagickImage)

- **Type**: Use `MagickImage` as the primary image type within services.
- **Disposal**: `MagickImage` implements `IDisposable`. Always use `using` blocks.
- **Conversion**: Convert to `byte[]` only when interacting with Syncfusion or writing to files.

## 6. XML Documentation

All **public** members must have XML documentation tags:

- `<summary>`: Single sentence description.
- `<param>`: Description for each parameter.
- `<returns>`: Description of return value and its lifecycle (e.g., if it needs disposal).
- `<exception>`: Document any expected exceptions.
