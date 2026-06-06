# Summarization Module

The **SlideGenerator.Summarization** module provides discovery and validation services. It replaces the former
`Scanning` module and additionally hosts the canonical recipe-summary models.

## Responsibility

- Analyzing Excel workbooks for sheet names, headers, and previews.
- Analyzing PowerPoint presentations for slides, shapes, and Mustache variables.
- Exposing the `RecipeSummary` model used by the Generator to materialize a recipe at runtime.

## Key Service

### `ISummarizationService`

The primary entry point. Implemented by `SummarizationService`.

- `SummarizeWorkbookAsync(path)` → `WorkbookSummary`: Sheet names, header→column index map, preview rows (
  `PreviewRule.PreviewRowCount`).
- `SummarizePresentationAsync(path)` → `PresentationSummary`: Slide list, shape inventory, distinct Mustache
  placeholders, thumbnail bytes.
- `SummarizeRecipeAsync(...)` and `SummarizeRecipeByIdAsync(...)`: Build a fully-resolved `RecipeSummary` from a recipe
  definition or from an entry stored in the Recipe module.

## Domain Model

- **Sheet/**: `WorkbookSummary`, `WorksheetSummary`, `WorksheetPreview`.
- **Slide/**: `PresentationSummary`, `SlideSummary`, `ShapeSummary`.
- **Recipes/**: `RecipeSummary`, `MapNode`, `TextInstruction`, `ImageInstruction`, `EditOptions`.

The `RecipeSummary` graph is the contract between Recipe (persistence) and Generator (execution): each `MapNode` maps
Excel sheets to a template slide, with `TextInstruction` and `ImageInstruction` describing placeholder replacement and
image composition.
