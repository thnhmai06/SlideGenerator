# Module: Pipelines & Workflows

## The Hook (Q&A)

**Q: How is the slide generation structured?**  
The `GeneratingWorkflow` is divided into three logical phases: **Validation (Phase A)**, **Preparation (Phase B)**, and **Assembly (Phase C)**. Each phase ensures the system only proceeds with verified, pre-processed data, minimizing runtime errors during the final PowerPoint generation.

**Q: What is the "Smart Idempotency" principle?**  
Steps like `DownloadImage` and `EditImage` check if the final artifact already exists in the temporary directory before execution. This prevents redundant network calls and CPU-intensive image processing if a workflow is restarted or if multiple slides share the same assets.

---

## 1. Workflow Phases

### Phase A: Validation & Template Setup
- **ValidateRequest**: Checks if Excel sheets, slides, and shapes exist. Invalid requests are pruned.
- **CreateTemplate**: Copies the base template and prepares the "mold" (a single-slide PPTX).

### Phase B: Resource Preparation
- **ExtractData**: Parses Excel rows into discrete generation tasks.
- **DownloadImage**: Fetches raw images from Cloud/Web.
- **EditImage**: Crops and resizes images to fit the exact dimensions of the target shape.

### Phase C: Assembly & Finalization
- **ReplaceSlideData**: Clones the mold, fills text (Mustache tags), and inserts images.
- **CloseAllHandles**: Cleans up file handles and deletes the mold slide.

---

## 2. Directory Structure (Temp)

Images are stored systematically to support idempotency:
- `Temp/{Workbook}/{Sheet}/{Column}/Download/`: Raw files.
- `Temp/{Workbook}/{Sheet}/{Column}/Edit/`: Cropped/Resized files ready for insertion.