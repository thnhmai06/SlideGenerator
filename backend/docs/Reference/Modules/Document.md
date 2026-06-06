# Document Module

The **SlideGenerator.Document** module provides a high-level abstraction over **Syncfusion.Presentation** and *
*Syncfusion.XlsIO**.

## Responsibility

- Abstracting complex PowerPoint/Excel APIs into simple domain interfaces.
- Handling EMU-to-pixel conversions.
- Managing mustache-based text templating.

## Core Abstractions

### `IPresentationProvider` / `IWorkbookProvider`

Handles opening and saving files.

- **SfPresentation**: A wrapper around `IPresentation` that manages an internal `FileStream` for safe writing.

### `IShape`

A unified interface for PowerPoint shapes. Image replacements are performed by writing directly to the `ImageData`
property, which handles the underlying Syncfusion representation.

### `ITextComposer`

Coordinates the replacement of mustache variables in slide shapes.

- Uses `ITemplateEngine` to scan for `{{Variable}}` patterns.

## Domain Model

- **`IShape`**: A unified interface for PowerPoint shapes, providing access to `ImageData`, `Paragraphs`, and `Bounds`.
- **EMU Conversion**: Uses the constant `9525.0f` to convert between PowerPoint's EMU units and standard pixels.
