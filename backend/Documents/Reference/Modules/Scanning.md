# Scanning Module

The **SlideGenerator.Scanning** module provides discovery services to help the frontend understand the structure of user-provided files.

## Responsibility
- Analyzing Excel workbooks for sheet names and headers.
- Analyzing PowerPoint presentations for slides and placeholders.
- Generating small image previews for UI display.

## Key Services

### `IScanningService`
The primary entry point, typically called before a generation workflow starts.

#### `ScanWorkbookAsync`
- Extracts sheet names.
- Maps headers to column indices.
- Captures a preview of the first 20 rows.

#### `ScanPresentationAsync`
- Lists all slides.
- Identifies every unique `{{Mustache}}` variable in text shapes.
- Identifies "Image Shapes" (shapes capable of receiving an image replacement).
- Generates a thumbnail for each slide.
