# Module: Documents (Office Logic)

## The Hook (Q&A)

**Q: How does the system handle high-fidelity PPTX/XLSX without Office installed?**  
We use **Syncfusion File Formats** libraries. This allows server-side generation that is 100% compatible with Microsoft PowerPoint and Excel without relying on heavy and unstable COM Interop.

**Q: How are text and images actually replaced?**  
- **Text**: The `TextComposer` scans for `{{Mustache}}` tags in shapes and replaces them with data from Excel.
- **Images**: The `ImageComposer` targets specific picture shapes, clears their existing fill, and applies a new `BlipFill` with the processed image.

---

## 1. Core Composers

- **TextComposer**: Handles complex text replacement including multi-line text and preservation of original font styling.
- **ImageComposer**: Ensures images are inserted correctly into PowerPoint shapes while maintaining proper aspect ratios (handled pre-insertion by the Image module).

---

## 2. Abstractions

- **SfWorkbook / SfPresentation**: Wrapper classes that manage the lifecycle of Syncfusion instances and provide a cleaner API for the pipeline steps.