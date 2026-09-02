# SlideGenerator V2 — Domain Reference

**Source of truth:** the V2 codebase at `V:\Code\cs\SlideGenerator` on branch `develop`, as of 2026-09-02.
**Audience:** an external UI/UX design agent. This document describes what the application *is and does* according to the code. It does not propose UI.

**Read this first — the repo's own docs are stale.** `CLAUDE.md` still describes a "SlideGenerator.Stdio" JSON-RPC/IPC sidecar and a Tauri frontend. **Neither exists in V2.** V2 is a single Avalonia desktop application (`SlideGenerator.Desktop`) that runs the entire backend in-process. Where `CLAUDE.md` and the code disagree, the code wins. `plans/idea.md`, `plans/ui.md`, `plans/status.md` (all outside git) describe the *intended* frontend and are current as design intent, not as a spec of shipped behaviour.

---

## 1. Executive Summary

SlideGenerator automates the production of PowerPoint presentations from tabular data and a template slide.

The user has: (a) an Excel/CSV workbook whose rows are records (people, products, awards, …), and (b) a PowerPoint file containing one slide designed as a template, with text placeholders and picture shapes. The user wants: one output presentation where the template slide is repeated once per data row, with each copy's placeholders filled from that row's cells and its picture shapes filled with images named/linked in that row (downloaded, cropped to fit, face-aware where asked).

In V2 the user builds a reusable **Recipe** that binds template placeholders/shapes to worksheet columns, then starts a **Run** of that recipe against a chosen output folder. Each Run fans out into one **Job** per (template-slide × worksheet) pair; each Job executes a fixed 4-phase pipeline in the background, reporting live progress. Completed and running Runs are browsable on a **Runs** page. Everything domain-related (recipes, run history, settings) is stored locally in SQLite/JSON; the network is used only for downloading row images, checking for app updates, and fetching the About page's contributor/sponsor lists.

Technically: a .NET 10 / Avalonia desktop app, Windows-first, MVVM (CommunityToolkit.Mvvm), 12 modules layered Foundation → Domain → Application → Host. Document I/O is Syncfusion (Excel + PowerPoint); templating is Mustache (Stubble); image work is NetVips + OpenCV YuNet face detection; updates are Velopack against GitHub Releases.

---

## 2. Core Domain

### Plain language

- A **Recipe** is a saved binding configuration. It says: "for template slide *X* in presentation *P*, feed it from worksheet(s) *W*; put column *Name* into placeholder `{{Name}}`; put the image at column *Photo* into the shape called `Picture 1`, cropping around the face."
- A Recipe holds one or more **Mappings**. Each Mapping is: *one template slide* + *its text rules* + *its image rules* + *one or more worksheet data sources*. A Recipe with three Mappings produces three kinds of slide.
- A **Run** (called a *Request* in code) is one execution of a Recipe: "generate now, save the `.pptx` files under this folder." A Run immediately expands into **Jobs** — one Job per (Mapping × worksheet source). Each Job writes exactly one output presentation file.
- A **Job** walks four phases in order: create the output file → add one slide per row → fill text → fill images. It can be paused/resumed/stopped **at the Run level only** (never per-Job), and it resumes automatically after an app crash from wherever it stopped.
- **Settings** are global: appearance (theme/language/motion — UI only), performance (`MaxConcurrentJobs` — affects generation), network (proxy, retry, download size cap — affects generation).
- The **About** page shows version/update status and live GitHub contributor/sponsor lists.

### Technical representation

```
Recipe (SQLite row: id, name, JSON, timestamps)
  └── Mappings : List<Mapping>
        ├── Template : PresentationSource(PresentationIdentifier file, SlideIdentifier 1-based index)
        ├── Sources  : List<WorksheetSource>(WorkbookIdentifier, WorksheetIdentifier, UsedColumns?, RowFilter?)
        ├── TextInstructions  : List<TextInstruction>(Set<placeholderTag>, List<ColumnIdentifier>)
        └── ImageInstructions : List<ImageInstruction>(Set<ShapeIdentifier>, List<ColumnIdentifier>,
                                                       ImageEditInstruction(List<RoiOption>), FallbackImagePath?)

Run:  Request(RecipeId, Name, OutputType, SaveFolder, AllowLocalPaths)
  │   Service.CreateAsync → requestId (GUID string)
  └── Jobs : one JobSpecification per (Mapping × WorksheetSource), fully resolved, id = 0-based ordinal int
        JobSnapshot(RequestId, JobId, JobStatus, JobPhase, CurrentIndex, JobSpecification, Timestamp, TotalRows?)

Persistence (single Data.db): Recipes | Requests (write-once) | Jobs (current-state, ~1s buffered)
```

---

## 3. Domain Concepts

### 3.1 Recipe

- **Type:** `SlideGenerator.Recipe.Models.Recipe(IReadOnlyList<Mapping> Mappings)`. Storage wrapper: `RecipeEntry(int Id, string Name, Recipe Recipe, DateTimeOffset Created, DateTimeOffset Updated)`.
- **Purpose:** a reusable, named binding of template artefacts to data columns. Independent of any Run.
- **Represents:** the user's answer to "how does my spreadsheet fill my template."
- **Properties:** integer `Id` (SQLite autoincrement), `Name` (free text, the only user-editable metadata), `Mappings` (flat list — see 3.2), `CreatedTimestamp`/`UpdatedTimestamp` (UTC).
- **Lifecycle:** created empty (`Recipe([])`) or by import; edited in the Recipe Editor; saved (insert on first save, update thereafter); duplicated; exported to a `.recipe` file; deleted permanently. No soft-delete, no versioning.
- **Can do:** be run any number of times, concurrently; be edited while Runs derived from it are in flight (a Job carries a fully-resolved `JobSpecification`, so editing/deleting the Recipe does not disturb running Jobs); expose `GetReferencedFiles()` (all distinct workbook + presentation paths).
- **Cannot do:** carry run history (that lives on Runs), carry per-recipe UI preferences (e.g. "always open me in Advanced mode" — **not stored**), reference data by anything other than an absolute/relative file path + name/index.
- **Depends on:** file paths to workbooks and presentations that exist on disk at run time (not enforced at save time).
- **Depended on by:** Runs (via `Request.RecipeId`), the Recipe Editor, `.recipe` export/import.
- **Invariants:** `Mappings` is never `null` after load — `SqliteRecipeRepository.DbReadEntry` and the import path both normalise a missing/`null` `mappings` to `[]`; a corrupt JSON blob deserialises to `Recipe([])` rather than throwing.
- **Validation:** essentially none at the repository layer. The Editor requires a non-blank `Name` to enable Save (see 3.13); it does **not** require any Mappings, resolved bindings, or existing files to save. Stronger checks (`HasTemplate`, no unresolved bindings) gate only "Save and run."

### 3.2 Mapping

- **Type:** `Mapping(IReadOnlyList<WorksheetSource> Sources, PresentationSource Template, IReadOnlyList<TextInstruction> TextInstructions, IReadOnlyList<ImageInstruction> ImageInstructions)`.
- **Represents:** one template slide plus the rules and data feeding it. Every `WorksheetSource` in a Mapping is rendered through the *same* template slide and the *same* text/image instructions — the one thing the removed graph model expressed that the flat list still needs (a nested list, no ids).
- **Has no name or id of its own.** The Editor labels a Mapping `"Slide {index}"` (`MappingEditSession.Label`).
- **Lifecycle:** exists only inside a `Recipe`. Added via the template picker, removed, reordered in the Editor's mapping navigator (shown only when ≥ 2 Mappings exist).
- **Fan-out rule:** at Run time each Mapping expands to `Sources.Count` Jobs (`Service.BuildJobs` = `Mappings.SelectMany(m => m.Sources.Select(...))`).

### 3.3 WorksheetSource

- **Type:** `WorksheetSource(WorkbookIdentifier Workbook, WorksheetIdentifier Worksheet, IReadOnlySet<ColumnIdentifier>? UsedColumns = null, RowFilter? RowFilter = null)`.
- **Represents:** one worksheet as a data feed, optionally column- and row-filtered.
- `WorkbookIdentifier(string BookPath, string? BookPassword, string? Separator)` — file path (rooted paths normalised via `Path.GetFullPath`), optional password, optional CSV/TSV separator. `GetBookType()` derives `WorkbookType` (`Xls`, `Xlsx`, `Xltx`, `Ods`, `Csv`, `Tsv`) from the extension.
- `WorksheetIdentifier(string SheetName)` — by name.
- `ColumnIdentifier(string ColumnName)` — by header name (row 1 is the header).
- `UsedColumns == null` means "all columns visible to instructions" and is *forward-looking* — the Editor collapses "every box checked" back to `null` on save so an untouched source keeps tracking future header changes rather than freezing today's list.
- `RowFilter == null` means "all rows."

### 3.4 RowFilter

- **Type:** polymorphic record `RowFilter` (STJ discriminator `"mode"`), enum `RowFilterMode : byte { All, IndexRange, PartitionBlock }`.
  - `AllRowFilter` — every data row.
  - `IndexRangeFilter(int Start, int End)` — 1-based, inclusive. Index 1 = first data row (= worksheet row 2).
  - `PartitionBlockFilter(int PartitionIndex, int PartitionCount)` — divides the data rows into `PartitionCount` equal blocks and takes block `PartitionIndex` (0-based). Used to split one worksheet across several Runs/machines.
- `GetIndices(int dataCount)` returns the selected 1-based data-row indices.
- **Persistence note:** `null` and `AllRowFilter` are treated as identical everywhere; `JobsRepository` never materialises `AllRowFilter` (stores `RowFilterType = null`).

### 3.5 PresentationSource ("the template")

- **Type:** `PresentationSource(PresentationIdentifier Presentation, SlideIdentifier Slide)`.
- `PresentationIdentifier(string PresentationPath, string? PresentationPassword)` — `PresentationType` (`Potx`, `Pptx`, `Ppsx`) from extension.
- `SlideIdentifier(int SlideIndex)` — 1-based, clamped to ≥ 1.
- **There is no "Template" entity.** A template is just this pair: a presentation file on disk + a slide index within it. See §9.

### 3.6 TextInstruction

- **Type:** `TextInstruction(IReadOnlySet<string> Placeholders, IReadOnlyList<ColumnIdentifier> Columns)`.
- **Semantics (`SlideGenerationWorkload.BuildRowTextValues`):** for each instruction, the **first non-empty cell** across `Columns` (in order) is the value; **every** tag in `Placeholders` is set to that value. So `Columns` is a fallback chain, `Placeholders` is a fan-out.
- Placeholder tags are Mustache keys — the bare `Name` from `{{Name}}` (see §10, TextComposer/TemplateEngine).

### 3.7 ImageInstruction

- **Type:** `ImageInstruction(IReadOnlySet<ShapeIdentifier> Shapes, IReadOnlyList<ColumnIdentifier> Columns, ImageEditInstruction ImageEditInstruction, string? FallbackImagePath = null)`.
- `ShapeIdentifier(string ShapeName)` — the PowerPoint shape's name (e.g. `"Picture 1"`).
- `Columns` — fallback chain of cells holding an image URL or local path; first non-empty wins (`Utilities.GetSource`).
- `FallbackImagePath` — normalised absolute path; used when the row's own source is missing/invalid.
- `ImageEditInstruction(IReadOnlyList<RoiOption> RoiOptions)` — an ordered chain of crop strategies tried in turn; first that succeeds is used; if all fail the cropper falls back to a centre crop.

### 3.8 RoiOption (crop strategy)

- **Type:** abstract `RoiOption` (`SlideGenerator.Image.Cropping`), enum `RoiMode : byte { Anchor, Interest }`.
  - `AnchorOption { AnchorType Type; Vector2 Ratio; Vector2 Pivot }` — geometry-based, optionally face-aware.
    `AnchorType : byte { Image, Face, Eyes, Nose, Mouth }`. `Face`/`Eyes`/`Nose`/`Mouth` require a detected face (OpenCV YuNet); return no result if none found, falling through to the next option.
  - `InterestOption { InterestType Type }` — content-aware via libvips. `InterestType : byte { Entropy, Attention, Low, High, All }`.
- Cropping is in-memory end to end; the result is written straight into `IShape.ImageData` as PNG bytes.

### 3.9 Request (the Run)

- **Type:** `Request(int RecipeId, string Name, PresentationType OutputType, string SaveFolder, bool AllowLocalPaths = false)`.
- `SaveFolder` is validated non-blank and normalised at construction (throws otherwise).
- `OutputType` sets the output file extension (`.pptx` / `.potx` / `.ppsx`).
- **`AllowLocalPaths` is inert.** It is a `Request` field, a `Requests`-table column, and a Run-dialog checkbox — but `JobSpecification` has no such field, `Service.BuildJobs` never propagates it, and `SlideGenerationWorkload` therefore cannot read it. Local file paths in image cells are handled **unconditionally** (`if (File.Exists(source))` in `ResolveShapeImageAsync`): loaded and cropped in memory, no hard-link, no copy. The XML doc on `Request.AllowLocalPaths` ("hard-linked or copied") describes behaviour that does not exist in V2. **Flag for design: treat this checkbox as a no-op until the backend wires it.**
- **Persisted as** `RequestRecord(string RequestId, Request Request, string LogPath, DateTimeOffset CreatedAt)` — write-once, never updated.

### 3.10 JobSpecification

- **Type:** `JobSpecification(string WorkbookPath, string WorksheetName, IReadOnlySet<ColumnIdentifier>? UsedColumns, RowFilter? RowFilter, string TemplatePresentationPath, int TemplateSlideIndex, IReadOnlyList<TextInstruction> TextInstructions, IReadOnlyList<ImageInstruction> ImageInstructions, string OutputPath)`.
- **Every value resolved from the Recipe at spawn time.** A Job never re-reads the Recipe to run or resume. This is why a Recipe can be edited/deleted while its Runs execute.
- `OutputPath` = `{Request.SaveFolder}/{workbookFileStem}/{sanitizedWorksheetName}{outputExtension}` (`Service.BuildOutputPath`). **It encodes neither the Mapping nor the slide index** — so two Mappings that share one worksheet in the same Request produce the *same* output path, which is exactly the collision `FindDuplicateOutputPath` rejects at submit. **Design consequence: within one Run, a given worksheet can feed at most one Mapping.**

### 3.11 JobSnapshot

- **Type:** `JobSnapshot(string RequestId, int JobId, JobStatus JobStatus, JobPhase Phase, int CurrentIndex, JobSpecification Specification, DateTimeOffset Timestamp, int? TotalRows = null)`.
- `JobId` is a **plain 0-based ordinal** (position in the Request's Job list), not a GUID.
- `CurrentIndex` = rows completed within the current `Phase`; reset to 0 on every phase transition. Together `(Phase, CurrentIndex)` is the entire resume position.
- `TotalRows` = the worksheet's row count after `RowFilter`, known once the workload starts; `null` for jobs from older builds → Runs' progress bar goes indeterminate.
- Doubles as the **job-scoped progress payload** — there is no separate `JobProgress` DTO.

### 3.12 JobStatus / JobPhase (state enums)

- `JobStatus : byte { Pending, Running, Complete, Paused, Cancelled, Error }`. **All six values are used.** `Pending` = spawned but not yet picked up.
- `JobPhase : byte { Queued, CreatingOutput, CreatingSlides, FillingText, FillingImages, Done }`.
  - **`Queued` is declared but never emitted.** `JobRunner.StartJobAsync` mints the first snapshot at `CreatingOutput`. Live phases are `CreatingOutput → CreatingSlides → FillingText → FillingImages → Done`. Forward-only, never regresses.

### 3.13 Recipe Editor session state (Desktop-only)

- `MappingEditSession(Mapping mapping)` — a mutable wrapper around one Mapping plus two `HashSet<string>` "touched" sets (placeholder names / shape names the user has explicitly confirmed). Provides a stable object identity for the mapping navigator (a `Mapping` record compares by value, so its identity changes on every edit).
- `RecipeEditorViewModel` — the coordinator. Holds `Sessions` (one per Mapping), a `SelectedSession`, `IsDirty`, `IsGuided`, `GuidedStep`, and three child panels (`Canvas`, `TextBindings`, `Sources`). New Recipe → `IsGuided = true`; existing Recipe opened from the list → `IsGuided = false` (Advanced).
- `GuidedStep : { Template = 1, Data, Binding, Review }` — the 4-step wizard. Same panels as Advanced; the enum only chooses which panel(s) show.
- **Dirty tracking** is explicit (child panels raise `Changed`), not record-equality. Merely switching between Mappings to look at them does not mark dirty.

### 3.14 Binding suggestion model (Editor)

- `BindingMatcher.Match(placeholders, columns)` — pure, no I/O. Per name it produces a `BindingCandidate(name, BindingConfidence, column?, candidates)`.
- `BindingConfidence : { Exact, Normalized, Ambiguous, None }` — Exact = literal name match; Normalized = match after loose normalisation; Ambiguous = >1 exact-normalised OR any partial/substring match; None = nothing.
- `BindingDisplayResolver.Resolve` folds a *saved* binding (always wins → `Assigned`) or, failing that, the matcher's suggestion into `BindingDisplayState : { Assigned, Suggested, NeedsSelection, Unassigned }`:
  - saved column, or `Exact` → **Assigned** (auto, silent)
  - `Normalized`, not yet confirmed → **Suggested**; once "touched" → Assigned
  - `Ambiguous` → **NeedsSelection** (user must pick from candidates)
  - `None` → **Unassigned**
- `Summarize` gives the `(Assigned, Suggested, NeedsSelection, Unassigned)` tuple shown as "N ghép · N đề xuất · N cần chọn · N chưa gán."

### 3.15 Settings (`Setting` record tree)

See §12.

### 3.16 Summaries (Editor support data, `SlideGenerator.Summarizer`)

- `WorkbookSummary(FilePath, Name, IReadOnlyList<WorksheetSummary>)`; `WorksheetSummary(WorkbookIdentifier, WorksheetIdentifier, int Count, WorksheetPreview?)`; `WorksheetPreview(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows)` — up to 20 preview rows (`ISummarizationService.MaxPreviewRows`).
- `PresentationSummary(PresentationPath, IReadOnlyList<SlideSummary>)`; `SlideSummary(PresentationIdentifier, SlideIdentifier, IReadOnlyList<string> Placeholders, IReadOnlyList<ShapeSummary> ImageShapes, byte[]? Preview, SizeF SlideSize)`; `ShapeSummary(SlideIdentifier, ShapeIdentifier, RectangleF Bounds)`.
- **`Placeholders`** = distinct Mustache tags scanned from every shape's display text.
- **`ImageShapes`** = only shapes whose `ImageData != null` — i.e. shapes that already hold a picture in the template. An empty rectangle is invisible to the Editor (see §9).
- `Preview` = rendered PNG of the slide/thumbnail.

### 3.17 ContentInfo (Cloud)

`ContentInfo(Uri Uri, string? MimeType, uint? Length, string? Extension)` — result of inspecting a remote image URL (final URI after redirects + cloud resolution, MIME, byte length, file extension). `IsImage()` = MIME starts `image/`.

### 3.18 About-page data

- `Contributor(string Login, string AvatarUrl, string ProfileUrl, int Contributions)` — live from `api.github.com/repos/thnhmai06/SlideGenerator/contributors`, most contributions first. **No role/badge field** — the login→role map (crown/computer/paint icons in `plans/idea.md`) was never supplied. **Unknown / Not determined from the code.**
- `Supporter(string Login, string AvatarUrl, string ProfileUrl)` — from `raw.githubusercontent.com/.../data/sponsors.json` (published by a scheduled GitHub Action). Empty list = no sponsors yet, not an error.
- `UpdateCheckResult : { NotInstalled, UpToDate, UpdateDownloaded, Failed }`.

---

## 4. Domain Relationships

| From | To | Cardinality | Ownership / lifecycle | Independent existence |
|---|---|---|---|---|
| Recipe | Mapping | 1 → 0..N | Recipe **owns** Mappings (value objects in its JSON) | Mapping cannot exist outside a Recipe |
| Mapping | WorksheetSource | 1 → 1..N (Editor requires ≥1 to advance Guided; a raw Recipe may hold 0) | owned | no |
| Mapping | PresentationSource (template) | 1 → 1 (required) | Mapping **references** a file path; does not own the file | the `.pptx` is an external file, fully independent, reusable |
| Mapping | TextInstruction | 1 → 0..N | owned | no |
| Mapping | ImageInstruction | 1 → 0..N | owned | no |
| ImageInstruction | RoiOption | 1 → 0..N (ordered) | owned | no |
| Recipe | workbook file | N Mappings → M files (a Mapping's Sources may span several workbooks) | reference only | file independent, reusable across Recipes |
| Recipe | presentation file | N Mappings → M files | reference only | file independent, reusable across Recipes and Mappings |
| Run (Request) | Recipe | N → 1 (`RecipeId`) | Run captures a **fully-resolved snapshot** at spawn; no live link afterwards | Recipe may be edited/deleted while the Run executes; `RecipeId` on the record is informational only |
| Run (Request) | Job | 1 → 1..N (one per Mapping × WorksheetSource) | Run owns its Jobs; deleting the Run deletes its Job rows and `RequestRecord` | a Job has no meaning without its Request |
| Job | output `.pptx` file | 1 → 1 | Job writes it; nothing deletes it automatically (`PreflightCleanup` only overwrites the Job's *own* prior output on a fresh, non-resumed start) | file persists after the Run; user opens it via "Open folder" |
| Job | per-job download cache | 1 → 1 folder (`%TEMP%\SlideGenerator\{requestId}\{jobId}\`) | created in phase D, **deleted by `GeneratorJobObserver.OnTerminalAsync`** once the Job reaches a terminal (non-Paused) state | transient |
| Run | `.log` file | 1 → 1 (`RequestRecord.LogPath`, shared by every Job of the Run) | written during execution; **not deleted** on Run delete | persists on disk |
| Recipe | `.recipe` package | export/import only | a `.recipe` is a portable zip snapshot; importing creates a **new** Recipe row + copies bundled files into `Imported/` | fully independent artefact |
| Setting | everything | 1 global instance | `ISettingManager.Current`; persisted to `UserSettings.json` | — |

**Not a relationship:** there is no Recipe↔Recipe link, no Mapping id/reference graph (the old `Node`/`Edge` model is gone), no Template registry, no "project" grouping Recipes.

---

## 5. User Workflows

Only workflows the code supports are listed.

### 5.1 Create / edit a Recipe

1. **Start:** Recipes page → "New" (blank Recipe, opens Guided) or a row's "Edit" (opens Advanced). Editor opens **inline inside the Recipes page** (`IsEditorOpen`/`Editor`), not as a separate destination.
2. **Intent:** define how a spreadsheet fills a template.
3. **Objects:** `RecipeEditorViewModel`, `MappingEditSession` per Mapping, child panels `SlideCanvasViewModel` / `TextBindingsViewModel` / `WorksheetSourcesViewModel`, `ISummaryCache` for previews.
4. **Actions (Guided order):**
   - ① **Template** — pick a `.pptx/.potx/.ppsx` file, pick a slide → appends a Mapping.
   - ② **Data** — add one or more workbooks; each contributes its first worksheet by default; per-source column checkboxes and a `RowFilterEditorViewModel` (All / IndexRange / PartitionBlock).
   - ③ **Binding** — confirm text-placeholder → column and image-shape → column bindings (auto-suggested per §3.14). Guided splits this into a Text sub-group and an Image sub-group.
   - ④ **Review** — shows the file count ("sẽ tạo N file" = one per worksheet source in the single Mapping) → Save, or "Save and run," or "Open Advanced mode."
   - **Advanced** exposes all panels at once plus a mapping navigator (add / remove / move up / move down) and a per-shape inspector (reorder / remove `RoiOption`s, pick a fallback image).
5. **State changes:** editor `IsDirty` flips on any child `Changed`; `Id` goes from `null` to a real id on first Save.
6. **Validation:** Save enabled when `IsDirty && Name` non-blank (nothing else). "Save and run" enabled when `HasTemplate && !HasUnresolvedBindings && Name` non-blank.
7. **Success:** `Saved` event → the Recipes list reloads.
8. **Failure:** repository exception surfaces as `ErrorMessage`; leaving a dirty editor prompts a confirm dialog (`recipes.unsavedChanges.*`).
9. **Edge cases:** if a Mapping's template slide no longer exists in the presentation, `LoadMappingAsync` bails silently and the panels keep the previous Mapping's content; the Editor emits a **strictly 1:1** model (one placeholder × one column, one shape × one column) even though `TextInstruction`/`ImageInstruction` support N×N — a fallback chain in an imported Recipe is **lost on re-save**.

### 5.2 Import / export a Recipe

- **Import:** Recipes page → "Import" → pick `*.recipe` → `IRecipePackageService.ImportAsync` reads `Recipe.json`, extracts bundled `Workbooks/*` and `Presentations/*` into `%LOCALAPPDATA%\SlideGenerator\Imported\{Workbooks,Presentations}` (dedup by filename, Zip-Slip guarded, extension allow-list), rewrites the Recipe's paths to the extracted locations, and inserts a **new** Recipe row named after the file. A `null`/missing `mappings` is normalised to `[]`.
- **Export:** a row's "Export" → pick a save path → `ExportAsync` bundles the Recipe JSON (paths rewritten to bare filenames) plus every referenced workbook/presentation that still exists on disk into a `.recipe` zip.

### 5.3 Run a Recipe

1. **Start:** Recipes list row → "Run", or Guided step ④ → "Save and run". Opens the **Run dialog** (`RunDialogViewModel`, a transient instance).
2. **Intent:** generate output presentations now.
3. **Objects:** `Request`, `IService.PreviewAsync` / `CreateAsync`, `PlannedJob`.
4. **Actions:** set `Name` (pre-seeded `"{recipe} {yyyy-MM-dd HH-mm}"`), `OutputType`, `SaveFolder` (folder picker), optional `AllowLocalPaths` (currently a no-op — §3.9). Every field change re-runs `PreviewAsync`, which lists every `PlannedJob(OutputPath, WorkbookPath, WorksheetName, ConflictKind)`.
5. **State changes:** on "Start" → `CreateAsync` mints a `requestId`, persists the `RequestRecord`, publishes `RequestProgress(PreparationStarted)`, announces the expected Job count, then spawns each Job (`JobStatus.Pending`, persisted and flushed immediately).
6. **Validation (`CanStart`):** not previewing, ≥ 1 planned Job, **no conflicts**, non-blank name and folder. `CreateAsync` re-checks: throws if two of the Request's Jobs share an output path (`ConflictKind.DuplicateWithinRequest`), or if a Job's output path is already claimed by another active (Running/Pending/Paused) Request (`ConflictKind.ConflictsWithActiveRequest`).
7. **Success:** dialog closes with the new `requestId`; the shell navigates to Runs and preselects it.
8. **Failure:** any exception → `ErrorMessage` in the dialog; the dialog stays open.
9. **Edge cases:** multiple concurrent Runs of the *same* Recipe are allowed (guarded only at the output-path level, not the recipe level).

### 5.4 Track and control a Run

1. **Start:** Runs page. One list, chip-filtered (`RunStatusFilter`: All / Running / Paused / Done / Cancelled), searchable by name. Master–detail: request row + its Job rows + logs.
2. **Objects:** `RunsViewModel`, `RequestRunViewModel` per Request, `JobRunViewModel` per Job, `IProgressHub`.
3. **Live updates:** after the initial load, `IProgressHub` pushes coalesced `JobSnapshot` and `RowProgress` batches every 250 ms; existing rows are patched in place. A Request key never seen before triggers one full reload (a `JobSnapshot` alone lacks `Request.Name`/`CreatedAt`).
4. **Per-Request commands (there are no per-Job commands):** Pause (when Running), Resume (when Paused), Stop (when Running/Paused/Pending), Delete (confirm dialog; stops first if active), Open folder (opens the first Job's output directory in the OS file browser).
5. **Detail shows:** per-Job status, phase, `CurrentIndex`/`TotalRows` (determinate progress bar, or indeterminate if `TotalRows` is null), output path, a live "activity line" (`RowStage` + free-text `Note`, e.g. the URL being downloaded), and logs read on demand from the `.log` file.
6. **Success/failure:** aggregate Request status via `Service.DeriveStatus` — see §6.3. **There is no Request-level `Error` state** (see §16).

### 5.5 Adjust settings

Settings page → three groups (Appearance / Performance / Network) + a link to About. Every field **persists immediately on change** — no Save button, no debounce. Per-group "reset to defaults" (only the Performance reset command is wired in the ViewModel shown). Theme changes apply live; language changes apply live; `ReducedMotion` toggles the app's motion-duration resources.

### 5.6 Check for updates / view About

About page → "Check for updates" (`UpdateChecker` via Velopack/GitHub Releases; result one of `NotInstalled`/`UpToDate`/`UpdateDownloaded`/`Failed`). Developers and Supporters lists load once per session (24 h disk cache, fail-soft to empty). Repository and sponsor links open in the browser.

---

## 6. State Machines / Lifecycle

### 6.1 Job phase (within one running Job)

```
CreatingOutput ──► CreatingSlides ──► FillingText ──► FillingImages ──► Done
   (copy template,     (append 1        (per row:        (per row:          (terminal
    strip slides;       cloned slide     read cells,       resolve source,    JobPhase
    or reopen on        per data row,    compose text,     download/local,    stamped on
    resume)             Save each)       Save each)        crop, set          the final
                                                           ImageData,         JobSnapshot)
                                                           Save each)
```

- **Transitions:** automatic, forward-only, one direction. Driven by `SlideGenerationWorkload.RunAsync`. Each transition writes a durable `JobSnapshot` (flushed before proceeding) so a crash resumes from the correct phase.
- **Checkpoint:** `context.CheckpointAsync(ct)` + `ct.ThrowIfCancellationRequested()` **before every row**. Pause/cancel granularity is "between rows," never mid-row.
- `Queued` — never entered (§3.12).

### 6.2 Job status

```
                 ┌────────── PauseAsync ─────────┐
                 ▼                               │
Pending ──► Running ◄──────── ResumeAsync ────── Paused
   │           │  │                               │
   │           │  └──────── StopAsync ────────────┤
   │           │                                  │
   │           ├─► Complete   (workload returned) │
   │           ├─► Error      (workload threw)    │
   └───────────┴─► Cancelled  (cancellation observed) ◄─┘
```

- **Pending → Running:** the engine picks the Job up (after acquiring a concurrency slot; a "starting" tick is published *before* the slot is acquired, so a queued Job still shows Running in the UI).
- **Running ⇄ Paused:** user, at the **Request** level (`IService.PauseAsync`/`ResumeAsync` fan out over the Request's Jobs). `GeneratorJobObserver` stamps `Paused`/`Running`. A paused Job blocks at its next checkpoint.
- **→ Cancelled:** user Stop, or app shutdown. The engine cancels the Job's `CancellationTokenSource` and also releases the pause gate so a paused Job unblocks to observe the cancellation.
- **→ Complete:** `RunAsync` returns a terminal `JobSnapshot(Complete, Done, TotalRows, TotalRows)`.
- **→ Error:** `RunAsync` threw (any exception in any phase, including a per-row exception — rows are **not** individually isolated).
- **Terminal states:** `Complete`, `Cancelled`, `Error`. `Paused` is *not* terminal.
- **Crash resume:** at startup `GeneratorResumeSource` reads every `Pending`/`Running`/`Paused` row and reschedules it from `(Phase, CurrentIndex)`, logging to the original Request's `.log` file. A previously `Paused` Job resumes as plain `Running` (no "why paused" is persisted).

### 6.3 Request (Run) aggregate status — `Service.DeriveStatus`

```
any Job Running or Pending      → Running
else any Job Paused             → Paused
else all Jobs Cancelled         → Cancelled
else                            → Complete      ◄── an all-Error Request lands HERE
```

- **There is no `Error` branch.** A Request whose Jobs all ended `Error` reports `Complete`, appears in `ListCompletedAsync`, and `RunStatusFilter` has no "Errored" chip. `RequestRunViewModel.DeriveDisplayStatus` repeats the same omission client-side. Job-level `Error` *is* visible per-Job in the detail panel. **This is a real gap the Runs page must account for.**

### 6.4 Request lifecycle phase — `RequestPhase` (progress only, never persisted)

```
PreparationStarted ──► ProcessingStarted ──► Completed
 (spawn loop begins)   (every announced Job   (every announced Job
                        has left Pending)      has reached a terminal status)
```

Monotonic. Published live by `Service.CreateAsync` (`PreparationStarted`) and derived elsewhere; `Summary.Phase` is recomputed statelessly from current Job statuses on every list call (`DeriveRequestPhase`).

### 6.5 Recipe Editor mode

```
new Recipe  ─► Guided (step Template → Data → Binding → Review)
existing    ─► Advanced
Guided ──"Open Advanced mode"──► Advanced      (one-way within a session; not remembered per recipe)
```

### 6.6 Binding display state

`Unassigned` / `NeedsSelection` / `Suggested` → **`Assigned`** on user pick or confirmation ("touched"). Never regresses once touched. See §3.14.

### 6.7 Theme

`System → Light → Dark → System` cycle (toolbar toggle), or direct set from Settings. Persisted to `Setting.Appearance.Theme`; applied to `Application.RequestedThemeVariant` immediately. The toolbar toggle animates a circular reveal from the click point (falls back to instant when `ReducedMotion` is on or no window).

---

## 7. Use Cases (capabilities catalogue)

`IService` (`SlideGenerator.Generator`) is the single façade the Desktop app calls for generation. `IRecipeRepository` + `IRecipePackageService` for recipes. `ISettingManager` for settings. `ISummarizationService` for editor previews.

| Use case | Input | Preconditions | Domain operation | Side effects | Output | Failure |
|---|---|---|---|---|---|---|
| **List recipes** | — | — | `IRecipeRepository.ListAsync` | SQLite read | `IReadOnlyList<IRecipeMetadata>` (id/name/timestamps), newest-updated first | repo exception → `ErrorMessage` |
| **Get recipe** | `id` | recipe exists | `GetAsync` | read | `RecipeEntry` (full mappings) | throws if not found |
| **Create recipe** | `RecipeInput(Name, Recipe)` | — | `AddAsync` | insert | `IRecipeMetadata` | — |
| **Update recipe** | `id`, `RecipeInput` | recipe exists | `UpdateAsync` | update, bumps `UpdatedTimestamp` | `IRecipeMetadata` | throws if not found |
| **Delete recipe** | `id` | — | `DeleteAsync` | delete | `bool` (found?) | — |
| **Duplicate recipe** | `id` | recipe exists | Get + Add with `" (copy)"` name | insert | new metadata | — |
| **Export recipe** | `id`, `outputPath` | recipe exists | `RecipePackageService.ExportAsync` | writes a `.recipe` zip (+ bundled files) | — | — |
| **Import recipe** | `.recipe` path, target folders | valid archive | `ImportAsync` | extracts files to `Imported/`, inserts a new recipe | `IRecipeMetadata` | `InvalidDataException` on missing/invalid `Recipe.json`, Zip-Slip, etc. |
| **Preview a run** | `Request` | recipe exists | `IService.PreviewAsync` → `BuildJobs` + conflict scan | none | `IReadOnlyList<PlannedJob>` | throws if recipe missing |
| **Create a run** | `Request` | recipe exists; no output-path conflicts | `CreateAsync` → `BuildJobs`, persist `RequestRecord`, spawn N Jobs | inserts `Requests` row + N `Jobs` rows; starts N background tasks; creates the `.log` file; emits progress | `requestId` (string) | throws on duplicate/active-conflict output path, or missing recipe |
| **Pause / Resume / Stop a run** | `requestId` | — | fan out over eligible Jobs | Job status transitions, progress events | `PartialResult(Succeeded, Skipped)` | ineligible Jobs counted skipped, not failed |
| **Pause-all / Stop-all** | — | — | fan out over all active requests | as above | `int` (requests affected) | — |
| **List active / completed runs** | `includeLogs` | — | group `Jobs` by `RequestId`, filter by `DeriveStatus`, join `Requests`, optionally parse `.log` | reads (+ `.log` parse if `includeLogs`) | `IReadOnlyDictionary<string, Summary>` | — |
| **Delete a run** | `requestId` | — | stop if active, then delete Job + Request rows | deletes rows (not the `.log`, not output files) | `bool` (found?) | — |
| **Delete all completed runs** | — | — | loop delete | as above | `int` | — |
| **Initialize** (startup) | — | called once before anything else | `JobRunner.InitializeAsync` → resume non-terminal Jobs | reschedules crash-leftover Jobs | — | — |
| **Shutdown** | — | called at app exit | cancel running Jobs, wait, final flush | — | — | — |
| **Summarize workbook** | `WorkbookIdentifier`, `getPreview` | file exists | open read-only, read headers + up to 20 rows/sheet | Syncfusion read | `WorkbookSummary` | `FileNotFoundException` |
| **Summarize presentation** | `PresentationIdentifier`, `getPreview` | file exists | open read-only, scan placeholders + image shapes + thumbnails | Syncfusion read | `PresentationSummary` | `FileNotFoundException` |
| **Get / Update / Reset settings** | `Setting` | — | `ISettingManager` | writes `UserSettings.json` | `Setting` | write error rethrown |
| **Check for updates** | — | installed as a Velopack app | `UpdateChecker` → GitHub Releases | may download an update package | `UpdateCheckResult` | `Failed` on any network error |
| **Get contributors / supporters** | — | — | `AboutDataService` → GitHub API / raw JSON | 24 h disk cache write | lists (possibly empty) | never throws — empty on failure |

---

## 8. Recipe (dedicated section)

**What it represents in V2:** a persisted, reusable, named configuration that maps template placeholders and picture shapes to worksheet columns, across one or more template slides. It is *pure configuration* — it holds no data, no rendered output, no run history, and no live handle to any file. It is the thing a user authors once and runs many times.

**Structure:** `Recipe = flat list of Mapping`. There is **no graph, no `Node`, no `Edge`, no id-based cross-references** — that model existed in an earlier iteration and was deliberately removed (`plans/status.md`, `CLAUDE.md`). A `Mapping = one template slide + its text rules + its image rules + one-or-more worksheet sources`.

**How created:** (a) "New" in the Recipes page → an empty `Recipe([])` opened in the Guided editor; (b) `IRecipePackageService.ImportAsync` from a `.recipe` file → a new row with bundled files copied locally; (c) "Duplicate" of an existing row.

**How edited:** in the inline Recipe Editor (Guided or Advanced). The Editor loads previews via `ISummarizationService`/`ISummaryCache`, computes binding suggestions (`BindingMatcher`), and on Save projects its panels back into a `Recipe` (`RecipeEditorViewModel.ToRecipe`) written via `IRecipeRepository.Add/Update`.

**How stored:** one row in the `Recipes` table of `%LOCALAPPDATA%\SlideGenerator\Data\Data.db`: `Id` (autoincrement int), `Name` (text), `Recipe` (the whole object as camelCase JSON, `RecipePackageFormat.Data.Recipe.Format` — `JsonStringEnumConverter`, `WhenWritingNull`), `CreatedTimestamp`, `UpdatedTimestamp` (ISO-8601 UTC strings). No normalised child tables.

**What it contains:** `Mappings` only. Each Mapping contains `Sources` (worksheet feeds with optional column subset + row filter), `Template` (`PresentationSource` = file path + 1-based slide index), `TextInstructions` (placeholder-set → column-list), `ImageInstructions` (shape-set → column-list + crop chain + fallback image).

**What it references (never owns):** absolute (or relative) paths to workbook files and presentation files. These files live wherever the user put them (or, for imports, in `Imported/`). The Recipe does not track their existence, hash, or modification.

**Multiple templates per Recipe:** **YES.** A Recipe has N Mappings; each Mapping's `Template` is an independent `(file, slideIndex)` pair. Different Mappings may point at different presentations, the same presentation with different slides, or (legally, though pointless) the same slide. There is **no** "Recipe → exactly one Template" constraint anywhere in the code.

**Template ownership:** templates are **not owned by Recipes** and are **not managed entities**. They are file paths. The same `.pptx` can be referenced by any number of Recipes and any number of Mappings. Selecting a template = the template-picker dialog (pick a presentation file → pick a slide).

**How a Recipe participates in generation:** at Run time `Service.BuildJobs` flattens `Mappings.SelectMany(m => m.Sources.Select(...))` into one fully-resolved `JobSpecification` per (Mapping × Source). From that moment the Recipe is irrelevant to the Run — every value has been copied into the `JobSpecification` and persisted on the Job row.

**What happens when a Recipe changes:** nothing happens to existing Runs (they carry snapshots). Future Runs use the new definition. Editing/deleting a Recipe while its Runs execute is explicitly safe.

**Validation rules:**
- Repository: none beyond "row exists" for Get/Update/Delete.
- Load: `Mappings` coerced non-`null`.
- Editor Save gate: `IsDirty && Name` non-blank.
- Editor "Save and run" gate: `HasTemplate (≥1 Mapping) && no NeedsSelection bindings && Name` non-blank.
- **A Recipe with zero Mappings, unresolved bindings, or dangling file references can be saved.** It just cannot be run cleanly.

---

## 9. Template (dedicated section)

**What a Template represents:** a `PresentationSource(PresentationIdentifier Presentation, SlideIdentifier Slide)` — i.e. *a PowerPoint file on disk plus a 1-based slide index into it*. That is the entire concept. There is no `Template` class, no template library, no template metadata store.

**Lifecycle:** none of its own. The file is external and user-managed. The reference is created when the user picks it in the template picker and lives inside a `Mapping` for as long as that Mapping exists.

**Relationship with Recipes:** a Mapping *references* one template. A Recipe (via its Mappings) references 0..N templates. Many Recipes and many Mappings may reference the same template file; the file has no idea it is being used.

**Relationship with generated slides:** in phase A the Job **copies the template file** to the output path and **removes all of its slides**. In phase B it opens the template read-only, `Clone()`s the requested slide, and `AddSlide()`s one clone per selected data row (the source presentation is kept open the whole time because Syncfusion clones stay coupled to their source's layout/master). So every output slide is a structural clone of the one template slide.

**Configuration:** the template itself is not configured. What *is* configured against it: `TextInstruction`s keyed by the Mustache tags found in its shapes' text, and `ImageInstruction`s keyed by the names of its picture shapes. `TemplateSlideIndex` selects which slide.

**Source / assets:** the `.pptx/.potx/.ppsx` file. Supported types: `PresentationType { Potx, Pptx, Ppsx }`. On `.recipe` export the file is bundled under `Presentations/`; on import it is extracted to `Imported\Presentations\`.

**How it is selected:** `TemplatePickerViewModel` — pick a presentation file (file picker), then pick one of its slides (thumbnail list from `SummarizePresentationAsync`).

**Reusability:** fully reusable. One template file, or one slide within it, can back any number of Mappings across any number of Recipes. Nothing is copied into the Recipe except the path and index.

**What makes a slide usable as a template (from `SummarizationService`):**
- **Text binding targets** = distinct Mustache tags (`{{tag}}`, `{{#tag}}`, `{{{tag}}}`, …) scanned from every shape's display text. A shape with no `{{…}}` contributes no placeholders.
- **Image binding targets** = shapes where `IShape.ImageData != null` — i.e. shapes that **already contain a picture** in the template (a placeholder image the user drops in). An empty rectangle or a content placeholder with no image is **not** offered as an image target in the Editor.

---

## 10. Studio & Generation

### 10.1 "Studio"

**There is no Studio in V2.** `ShellDestination` has exactly four values — `Recipes`, `Runs`, `Settings`, `About` — and none is a "Studio." `plans/status.md` lists "3-tab Studio" explicitly among *deliberately rejected* design options.

The closest thing is the **Recipe Editor**, which is an *inline state of the Recipes page* (`RecipesViewModel.IsEditorOpen` / `.Editor`), not a navigable destination. It is where the user configures a Recipe (§5.1, §8). It is not a slide editor — it never modifies the template file; it edits binding rules, previewed against read-only summaries.

### 10.2 What "Generate" triggers

"Start" in the Run dialog → `IService.CreateAsync(Request)`. Synchronously it:

1. resolves the Recipe, mints `requestId` (`Guid.NewGuid().ToString()`),
2. computes the Job list (`BuildJobs`),
3. rejects duplicate / active-conflicting output paths,
4. writes the `RequestRecord` (`Requests` table), publishes `RequestProgress(PreparationStarted)`, announces the expected Job count,
5. loops: for each Job, persist its initial `Pending` `JobSnapshot` (flushed immediately) and hand it to the Job engine, which fires it on `Task.Run` and returns without waiting.

`CreateAsync` returns the `requestId` almost immediately. Generation runs entirely in the background.

### 10.3 What runs internally per Job (`SlideGenerationWorkload.RunAsync`)

- **Phase A — CreatingOutput:** if fresh, `PreflightCleanup` deletes any prior file at this Job's own output path, `File.Copy` the template → output path, remove every slide. If resuming, reopen the existing output as-is. Open the source workbook read-only, resolve the worksheet, compute `dataRows` from the `RowFilter`, load + clone the template slide.
- **Phase B — CreatingSlides:** append `dataRows.Count` clones of the template slide, `output.Save()` after each; `JobSnapshot(CreatingSlides, i+1)` per slide.
- **Phase C — FillingText:** per row — read the row's cells, `BuildRowTextValues` (per `TextInstruction`: first non-empty column value → all its placeholders), `TextComposer.Compose(shape, values)` for every shape on the row's slide (`ITextComposer` renders Mustache while preserving each text run's formatting via coverage-ratio distribution), `output.Save()`; `RowProgress` + `JobSnapshot` per row.
- **Phase D — FillingImages:** first inspect every distinct image source across all rows once (`ICloudClient.InspectAsync` → `ContentInfo`, deduped, retried with backoff per `Setting.Network.Retry`). Then per row, per `ImageInstruction`, per target shape:
  - resolve the source cell (first non-empty of `Columns`);
  - if it is an existing local file → load + crop in memory;
  - else if it inspected to an image → ensure it is in the per-job download cache (`%TEMP%\SlideGenerator\{requestId}\{jobId}\{hash(uri)}{ext}`), respecting `Setting.Network.MaxDownloadBytes`; then crop;
  - else if `FallbackImagePath` exists → load + crop it;
  - crop = `ISmartCropper.CropAsync(image, shapeBounds, RoiOptions)` — try each `RoiOption` in order, first success wins, else centre crop;
  - assign the PNG bytes to `IShape.ImageData`; `output.Save()` after the row.
- **Completion:** `RunAsync` returns `JobSnapshot(Complete, Done, TotalRows, TotalRows)`.

### 10.4 Progress & errors

- **Progress:** `RequestProgress` (aggregate phase), `JobSnapshot` (per-Job current state), `RowProgress` (per-row: `RowStatus`, `RowStage`, free-text `Note`). Published on the in-process `IEventBus`; the Desktop's `ProgressHub` coalesces and marshals them to the UI thread every 250 ms. Only `Jobs` is persisted (buffered ~1 s); rows are live-only.
- **Errors:** an exception anywhere in `RunAsync` (missing worksheet, missing template slide, Syncfusion failure, an unhandled per-row error) fails the whole Job → `JobStatus.Error`. Rows are **not** individually try/caught to continue. *Image-specific* soft failures do not fail the Job: a failed download returns `null` (→ fallback image, else the shape is left unfilled); a crop exception is caught and logged (`CropToPngAsync`) and the shape is left unfilled.
- **Outputs:** one `.pptx/.potx/.ppsx` per Job at its `OutputPath`. Files are never auto-deleted; the user opens the folder from the Runs detail panel.

### 10.5 Synchronous or asynchronous

**Asynchronous.** `CreateAsync` returns immediately; Jobs run on background tasks bounded by `MaxConcurrentJobs`; the UI observes them through the progress stream and periodic list reloads. There is a genuine Run/Job/execution model — see §11.

---

## 11. Runs / Execution

V2 has a full execution model. Vocabulary:

| Code term | Meaning |
|---|---|
| **Request** (a.k.a. **Run**) | one user-initiated generation of a Recipe. Identified by `requestId` (GUID string). Persisted as `RequestRecord` in the `Requests` table (write-once). Groups N Jobs. |
| **Job** | one unit of generation = one Mapping × one WorksheetSource = one output file. Identified within its Request by a 0-based ordinal `JobId` (int). Persisted as a `JobSnapshot` row in the `Jobs` table (current-state, updated as it progresses). |
| **JobSpecification** | the fully-resolved, self-contained description of what a Job does (workbook, worksheet, columns, row filter, template path + slide index, text/image instructions, output path). |
| **Job Engine** (`SlideGenerator.Jobs`) | a generic, domain-free scheduler: `IJobEngine<TKey,TState>` runs any `IJobWorkload<TState>`, owning the running-job registry, the concurrency semaphore, pause/checkpoint, and crash-resume. Zero knowledge of slides. |
| **Job Workload** (`SlideGenerationWorkload`) | the slide-specific 4-phase pipeline, plugged into the engine. Wrapped in `LoggingWorkload` (per-Job file-log scope). |
| **JobRunner** | thin adapter from `IService`/`Service` vocabulary onto `IJobEngine`. |

**Why it exists:** generation is long-running (per-row, with network I/O), needs bounded concurrency to cap RAM, needs pause/resume, and must survive an app crash mid-generation. The Engine/Workload split makes the concurrency/resume machinery reusable for future non-slide job types.

**Lifecycle & states:** see §6.2 (Job status), §6.1 (Job phase), §6.3–6.4 (Request aggregate).

**What creates a Run:** only `IService.CreateAsync`, from the Run dialog or Guided "Save and run."

**What a Run references:** a `RecipeId` (informational after spawn), a `SaveFolder`, an `OutputType`, a `Name`, one shared `.log` path, and its Jobs.

**Information a Run/Job carries:** everything in §3.9–3.12. Notably a Job carries its **whole** `JobSpecification` on its row — no lookup needed to run or resume.

**Progress tracking:** live via the event bus / `ProgressHub` (§10.4). Persisted state is coarse: `(JobStatus, JobPhase, CurrentIndex, TotalRows)` per Job, buffered to SQLite ~1×/second. **Per-row history is never persisted** — once a row's `RowProgress` event has been shown, it is gone; `JobSummary` has no `Rows` field.

**Completion:** a Job reaching `Complete`/`Cancelled`/`Error`. A Request is "complete" when `DeriveStatus` no longer returns `Running`/`Paused` (note the missing `Error` case, §16). `Summary.CompletedAt` = the latest Job timestamp once the Request is done.

**Failure:** per-Job `Error` (visible in the Job detail). No aggregate error state. A Cancelled Job leaves a partially-written output file on disk.

**Inspecting past executions:** yes — `ListCompletedAsync` returns `Summary` per completed/cancelled Request, browsable on the Runs page (name, status, created/completed times, per-Job phase/index/output path, and logs parsed from the `.log` file on demand). The Recipes page also shows a per-recipe "recent runs" strip (up to 5, immutable snapshots).

**Persistence vs transience:**
- Persistent: `Requests` rows, `Jobs` rows (current state only), the `.log` file, the output `.pptx` files.
- Transient: everything about rows (progress, activity line), the per-job download cache (deleted on terminal), `RequestPhase`, the in-memory running-job registry, `Summary` objects (rebuilt every list call), parsed log entries (never cached).

---

## 12. Settings & Configuration

**Root:** `Setting` (immutable record), exposed via `ISettingProvider.Current` / `ISettingManager`. Persisted to `%LOCALAPPDATA%\SlideGenerator\Data\UserSettings.json` under a top-level `"Application"` key, pretty-printed JSON. Single global instance (no per-recipe, per-run, or per-window scope). On load failure or missing file → defaults are written.

### 12.1 Appearance (`Setting.AppearanceSetting`) — **UI only, does not affect generation**

| Field | Type | Default | Valid values | Effect |
|---|---|---|---|---|
| `Theme` | `ThemeMode` enum | `System` | `System`, `Light`, `Dark` | `Application.RequestedThemeVariant`, applied live |
| `Language` | `string` | `""` | culture name (`"vi"`, `"en"`, …) or `""` = follow OS UI culture | `LocalizationService` reloads strings live (no restart) |
| `ReducedMotion` | `bool` | `false` | — | zeroes the app's `MotionMicro`/`MotionUi`/`MotionBrand` duration resources → animations become instant; theme reveal falls back to instant |

### 12.2 Performance (`Setting.PerformanceSetting`) — **affects generation**

| Field | Type | Default | Effect |
|---|---|---|---|
| `MaxConcurrentJobs` | `uint` | `5` | size of the Job engine's concurrency semaphore. Caps how many Jobs run their workload simultaneously (RAM guard — each holds a Workbook + Presentation in memory). Applies to the **next** Job spawned (read fresh per property access); running Jobs keep their slot. Only throttles execution, never Run acceptance. |

This is the **only** field on `PerformanceSetting` — the old parallel-download/edit/read fields and the hardware/network calibration system were removed.

### 12.3 Network (`Setting.NetworkSetting`) — **affects generation (image download only)**

| Field | Type | Default | Effect |
|---|---|---|---|
| `Proxy.UseProxy` | `bool` | `false` | whether the download `HttpClient` uses a proxy (`Registration` `ConfigurePrimaryHttpMessageHandler`) |
| `Proxy.ProxyAddress` | `string` | `""` | e.g. `http://proxy:8080` |
| `Proxy.Username` / `Password` / `Domain` | `string` | `""` | `NetworkCredential` for the proxy |
| `Retry.MaxRetries` | `int` | `3` | retry count for `ICloudClient.InspectAsync` / `DownloadAsync` (exponential backoff) |
| `Retry.Timeout` | `int` (seconds) | `30` | network timeout |
| `Retry.MaxRetryDelay` | `int` (seconds) | `16` | backoff ceiling |
| `MaxDownloadBytes` | `uint` (bytes) | `52428800` (50 MB) | per-file download cap; `0` = unlimited. A source exceeding it is skipped (logged), the shape gets the fallback image or nothing. Surfaced in the UI as megabytes. |

### 12.4 Behaviour

- **Immediate persistence:** every field writes to disk on change (`SettingsViewModel.Persist` → `ISettingManager.Update` → `Save`). No Save button, no debounce, no dirty state.
- **Reset:** `ISettingManager.ResetToDefaults()` exists (whole-tree). The Settings page exposes a per-group reset; only the **Performance** group's reset command is wired in the ViewModel shown (`ResetPerformanceAsync` → `Performance = new PerformanceSetting()`).
- **Validation:** none in the model or manager. Fields are bound directly (numeric up-downs, text boxes). Out-of-range values are not rejected.
- **UI-preference vs generation-config split:** the code distinguishes them by group. Appearance = UI only. Performance + Network = generation. There is no separate "UI preferences" store — it is all one `Setting` tree.
- **Not stored anywhere:** window size/position, last-opened page, per-recipe "prefer Advanced mode," column widths, recent folders.

---

## 13. File System & Offline Model

**Local-first for domain data; the network is used and load-bearing for three specific things.** Do not describe the app as "offline" — it is not.

### 13.1 What the app reads

| What | From | When |
|---|---|---|
| Source workbooks (`.xls/.xlsx/.xltx/.ods/.csv/.tsv`) | user-picked absolute paths (or `Imported\Workbooks\` for imports) | Editor preview; every Job, phase A/C/D |
| Template presentations (`.pptx/.potx/.ppsx`) | user-picked absolute paths (or `Imported\Presentations\`) | Editor preview; every Job, phase A/B |
| `.recipe` package | user-picked path | on import |
| `Data.db` | `%LOCALAPPDATA%\SlideGenerator\Data\Data.db` | continuously (recipes, requests, jobs) |
| `UserSettings.json` | same folder | at startup |
| `appsettings.json` | executable directory | at startup (log levels) |
| About caches (`about-contributors.json`, `about-sponsors.json`) | `Data\` | About page, 24 h TTL |
| Local image files referenced in data cells | wherever the cell points | phase D (`File.Exists(source)`) |
| Fallback images | `ImageInstruction.FallbackImagePath` | phase D |

### 13.2 What the app writes

| What | To |
|---|---|
| Output presentations | `{Request.SaveFolder}\{workbookStem}\{worksheetName}{ext}` — one per Job, incrementally saved after every row |
| `Data.db` | `%LOCALAPPDATA%\SlideGenerator\Data\` (SQLite, `journal_mode=WAL`) |
| `UserSettings.json` | same folder |
| Per-Run log | `%LOCALAPPDATA%\SlideGenerator\Logs\Workflows\{sanitizedRunName}.log` (one file shared by all Jobs of the Run) |
| System logs | `Logs\System\{timestamp}.log` (+ a `latest.log` hard link) |
| Per-Job download cache | `%TEMP%\SlideGenerator\{requestId}\{jobId}\{hash(uri)}{ext}` — deleted when the Job reaches a terminal (non-Paused) state |
| Imported recipe resources | `Imported\Workbooks\`, `Imported\Presentations\` |
| Update packages | Velopack's own location (on "download update") |
| Single-instance lock | `Instance.pid` under the user data root |

### 13.3 Path resolution

- User data root = `%LOCALAPPDATA%\SlideGenerator`, or the executable directory if built `-p:Portable=true` (`NameAndPaths.Portable`, a compile-time constant).
- All user-supplied paths pass through `Path.GetFullPath` at entry (CodeQL path-injection sanitiser). Rooted paths in identifiers are normalised; relative paths are kept relative.

### 13.4 Networking & external services (all optional to the app's core, required for their feature)

| Feature | Endpoint / mechanism |
|---|---|
| Row image download | arbitrary HTTP(S) URLs; HEAD→GET redirect following; `GoogleDriveResolver` rewrites Drive share links to direct-download URLs (**only** Google Drive is implemented; OneDrive/SharePoint are not) |
| App updates | Velopack against **GitHub Releases** for `thnhmai06/SlideGenerator` |
| About — developers | `https://api.github.com/repos/thnhmai06/SlideGenerator/contributors` |
| About — supporters | `https://raw.githubusercontent.com/thnhmai06/SlideGenerator/data/sponsors.json` |

There is **no local server**, no background service, no telemetry endpoint. If the network is unavailable: recipes/runs/settings/editor all work fully; row images fall back to the fallback image or are left blank; About lists show empty; update check reports `Failed`.

---

## 14. Persistence

### 14.1 The single database — `Data.db`

One SQLite file, `%LOCALAPPDATA%\SlideGenerator\Data\Data.db`, `journal_mode=WAL`. Replaces an older per-purpose split. Schema is created and migrated by **DbUp** at startup (`DatabaseMigrator.Migrate`, called from `Program.Main` before the host is built), from embedded scripts `001_2.0.0.sql` and `002_add-total-rows-to-jobs.sql`, tracked in DbUp's `SchemaVersions` table. Three domain tables:

- **`Recipes`** — `Id` (INTEGER PK AUTOINCREMENT), `Name` (TEXT), `Recipe` (TEXT — the whole `Recipe` object as JSON), `CreatedTimestamp`, `UpdatedTimestamp` (TEXT, ISO-8601 UTC). Changes rarely (only on Editor save / import / duplicate / delete). Short-lived connection per CRUD call.
- **`Requests`** — `RequestId` (TEXT PK), `RecipeId` (INTEGER), `Name`, `OutputType`, `SaveFolder`, `AllowLocalPaths` (INTEGER), `LogPath`, `CreatedAt`. **Write-once** — inserted by `CreateAsync`, never updated. Deleted with its Run.
- **`Jobs`** — composite PK `(RequestId, JobId)`. Columns: `Status`, `Phase`, `CurrentIndex`, `TotalRows` (the mutable current state); `WorkbookPath`, `WorksheetName`, `UsedColumnsJson` (nullable JSON), `RowFilterType` + `RowFilterStart`/`End`/`PartitionIndex`/`PartitionCount` (nullable scalars — a small closed set, no JSON), `TemplatePresentationPath`, `TemplateSlideIndex`, `TextInstructionsJson`, `ImageInstructionsJson`, `OutputPath` (the resolved spec — written once), `Timestamp`. The `UPSERT` only updates `Status`/`Phase`/`CurrentIndex`/`OutputPath`/`Timestamp`/`TotalRows` on conflict; the spec columns are immutable after insert.
- **No `Rows` table** — per-row progress is never persisted.

`Jobs` is the only hot table. `JobsRepository : BufferedRepository<(string,int), JobSnapshot>` — writers `Enqueue` (coalesced, last-write-wins per key); a background `PeriodicTimer` (~1 s) drains the dirty set and upserts the batch in one transaction, then raises `Flushed`. Certain writes force an immediate flush (initial `Pending`, phase transitions, terminal states) so a crash resumes from the right point.

### 14.2 Settings

`UserSettings.json` (see §12) — plain `System.Text.Json`, indented, under an `"Application"` object.

### 14.3 Serialization formats

- Recipe (DB + `.recipe`): camelCase JSON, `JsonStringEnumConverter`, ignore-null, a custom `IReadOnlySet` converter. `RowFilter` polymorphism via a `"mode"` discriminator; `RoiOption` polymorphism via `Mode`.
- Job spec `*Json` columns: a separate small `JsonSerializerOptions` (`JobSpecificationJson`).
- Settings: default STJ, indented.
- Logs: line-oriented text with a parseable scope path (`{requestId}` / `{requestId}/{jobId}` / `{requestId}/{jobId}/{rowIndex}`), read back by a regex `LogFileReader`.

### 14.4 Persistent vs transient objects

| Persistent | Transient (rebuilt/lost) |
|---|---|
| `RecipeEntry` (row) | `Recipe` object graphs held in the Editor |
| `RequestRecord` (row) | `Summary` / `JobSummary` (recomputed every list call) |
| `JobSnapshot` current state (row) | `RowProgress`, activity line, per-row anything |
| Output `.pptx` files | in-memory running-Job registry, `PauseGate` |
| Per-Run `.log` file | per-Job download cache (deleted on terminal) |
| — | `RequestPhase` aggregation, `MappingEditSession` touched-sets |

### 14.5 Migration / versioning

DbUp forward-only migrations, embedded, auto-discovered by wildcard in the `.csproj`, tracked in `SchemaVersions`. No down-migrations. There is no schema version stamped in the Recipe JSON.

---

## 15. Events & Messaging

All in-process (no message broker, no IPC). Two publish-only interfaces in `SlideGenerator.Generator` — `IEventBus`, `ILogNotifier` — implemented in the Desktop host by `GeneratingEventBus` / `LogNotifier`, consumed by `ProgressHub` and ViewModels.

| Event | Payload | Emitted when / by | Consumed by | Domain fact or UI signal? |
|---|---|---|---|---|
| `RequestProgress` | `RequestId`, `RequestPhase` (`PreparationStarted`/`ProcessingStarted`/`Completed`), `Timestamp` | `Service.CreateAsync` (`PreparationStarted`); other phases inferred | `ProgressHub.RequestProgressChanged` → shell/ViewModels | UI signal — never persisted |
| `AnnounceExpectedJobCount(requestId, count)` | request id + N | `Service.CreateAsync` before the spawn loop | phase-aggregation logic | internal timing signal |
| `JobSnapshot` (published as job progress) | the whole job current-state row | `GeneratorJobObserver` on every progress/pause/resume/terminal transition; and each phase transition | `ProgressHub.Jobs` (coalesced by `(requestId,jobId)`, 250 ms) → `RunsViewModel` patches rows; `MainWindowViewModel` counts active jobs for the window title | **Domain fact** — also the persisted row |
| `RowProgress` | `RequestId`, `JobId`, `RowIndex` (1-based), `RowStatus` (`Waiting`/`Processing`/`Done`/`Error` — **only `Processing` and `Done` are ever emitted**), `RowStage` (`None`/`Downloading`/`CroppingImage`/`SavingOutput` — **`CroppingImage` is never emitted**), `Note`, `Timestamp` | `SlideGenerationWorkload.ReportRow` — one call per row per stage | `ProgressHub.Rows` (coalesced by `(requestId,jobId,rowIndex)`) → the Job detail "activity line" | UI signal — never persisted, no history |
| `LogNotification` / `LogEntry` | timestamp, scope path, level, message | every log line written during a Job (`ScopeNotifyingSink`) | `ProgressHub.Logs` (append-only, never dropped) + the per-Run `.log` file | diagnostic — persisted only to the `.log` file |
| `IJobsRepository.Flushed` | the batch just persisted | after each ~1 s SQLite flush | — (was relayed over IPC in V1; now unused by the Desktop UI, which reads the event bus directly) | internal |

**ViewModel-level events** (not a bus): `RecipesViewModel.RunStarted` → shell navigates to Runs; `RecipeListItemViewModel.EditRequested`/`RunStarted`/`Deleted`/`Duplicated`; editor `Saved`/`RunStarted`; child-panel `Changed` → dirty tracking; `RunDialogViewModel.RequestClose`; `TemplatePickerViewModel.RequestClose`; `LocalizationService.PropertyChanged` (language switch).

**Ordering constraint the UI must respect:** `ProgressHub` must be constructed (and its subscriptions attached) **before** `IService.InitializeAsync()`, because crash-resumed Jobs start emitting immediately and a late subscriber would miss their first events (`App.axaml.cs` enforces this).

---

## 16. Validation & Errors

### 16.1 What can fail, where it is checked, how it surfaces

| Failure | Where checked | Representation | Recoverable? | Domain state left behind |
|---|---|---|---|---|
| Recipe not found (Get/Update/Delete/Run) | `SqliteRecipeRepository`, `Service` | `InvalidOperationException` | user retries | none |
| Corrupt Recipe JSON in DB | `DbReadEntry` | swallowed → `Recipe([])` | — | recipe silently reads as empty |
| Invalid / null `mappings` on import | `ReadRecipeFile` | coerced to `[]` (or `InvalidDataException` if `Recipe.json` missing/not JSON) | user re-exports | none |
| `.recipe` Zip-Slip / bad extension / escaping entry | `ExtractEntry` | `InvalidDataException` (escape) or silent skip (extension) | — | partial extraction possible |
| Blank `SaveFolder` | `Request` ctor | `ArgumentException` | user picks a folder | none |
| Output-path duplicate within a Run | `Service.CreateAsync` / `PreviewAsync` | `ConflictKind.DuplicateWithinRequest`; `CreateAsync` throws `InvalidOperationException` | user changes folder / recipe | no Run created |
| Output-path collides with an active Run | same | `ConflictKind.ConflictsWithActiveRequest`; `CreateAsync` throws | user waits / changes folder | no Run created |
| Worksheet / sheet name not found | `SlideGenerationWorkload` phase A | exception → `JobStatus.Error` | resume won't help (config wrong); user fixes recipe + re-runs | partial output file on disk |
| Template slide index out of range | phase A | exception → `JobStatus.Error` | as above | partial output file |
| Syncfusion licence missing | Document layer | exception at first workbook/presentation open → `JobStatus.Error` | set the licence, re-run | none |
| Image URL unreachable / over size cap | phase D | soft — `null` → fallback image → shape left unfilled; logged | — | Job continues, `Complete` |
| Crop failure | `CropToPngAsync` | soft — caught, logged, shape left unfilled | — | Job continues |
| Any other per-row exception | phase B/C/D loop | **hard** — propagates → `JobStatus.Error` | resume from `(Phase, CurrentIndex)` after fixing cause | partial output file |
| App crash mid-Job | — | Job row stays `Pending`/`Running`/`Paused` | **auto-resumed** at next startup from `(Phase, CurrentIndex)` | output file kept and reopened |
| Settings file unreadable | `SettingManager.Load` | logged → defaults written | — | settings reset to defaults |
| Network failure on About / update | `AboutDataService` / `UpdateChecker` | empty lists / `UpdateCheckResult.Failed` — never an exception to the UI | user retries | none |

### 16.2 The gaps a designer must know

1. **No Request-level `Error` status** (§6.3). An all-failed Run displays as `Complete` and sits in the "Done" filter. Job-level `Error` is only visible by expanding the Run's Jobs.
2. **`RowStatus.Error` and `RowStatus.Waiting` are never emitted**; `RowStage.CroppingImage` is never emitted; `JobPhase.Queued` is never entered. Do not design chips/indicators for these four.
3. **Rows are not error-isolated.** One bad row (structurally) kills its whole Job. Only *image* problems degrade gracefully within a row.
4. **`AllowLocalPaths` does nothing** (§3.9).
5. **No pre-run validation that referenced files exist.** A recipe pointing at a moved workbook saves fine and only fails when its Job hits phase A.
6. **The Editor can save an incomplete/invalid recipe** (§8). Only "Save and run" is gated.
7. **Re-saving an imported recipe with N-column fallback chains flattens them to 1** (§5.1).

---

## 17. Architecture & Domain Boundary

```
┌───────────────────────────────────────────────────────────────┐
│ Host / UI  —  SlideGenerator.Desktop  (Avalonia, MVVM,         │
│   CommunityToolkit.Mvvm)                                       │
│   Shell (4 destinations) · Features/{Recipes,Runs,RecipeEditor,│
│   Settings,About} · Services/{Progress,Theme,Localization,     │
│   Dialogs} · Bootstrap/{SingleInstanceLock,UpdateChecker,      │
│   Metadata} · Program.cs (entry, DB migration, Velopack)       │
└───────────────▲───────────────────────────────────────────────┘
                │ builds the generic Host / DI container; wires everything in-process
┌───────────────┴───────────────────────────────────────────────┐
│ Application                                                     │
│   SlideGenerator.Generator  — IService façade, Service,         │
│     JobRunner, SlideGenerationWorkload, GeneratorJobObserver,   │
│     GeneratorResumeSource, JobsRepository/RequestsRepository,   │
│     Progress DTOs, IEventBus/ILogNotifier (publish-only)        │
│   SlideGenerator.Summarizer  — ISummarizationService            │
└───────────────▲───────────────────────────────────────────────┘
┌───────────────┴───────────────────────────────────────────────┐
│ Domain                                                          │
│   SlideGenerator.Settings  — Setting tree, ISettingManager,     │
│     NameAndPaths (all paths), DatabaseMigrator (DbUp)           │
│   SlideGenerator.Recipe  — Recipe/Mapping/Instruction models,   │
│     IRecipeRepository (SQLite), IRecipePackageService (.recipe) │
└───────────────▲───────────────────────────────────────────────┘
┌───────────────┴───────────────────────────────────────────────┐
│ Foundation                                                      │
│   Utilities · Cloud (ICloudClient, Google Drive resolver) ·     │
│   Logging (Serilog, scoped file logging) ·                      │
│   Document (Syncfusion Excel/PPT wrappers + Mustache engine) ·  │
│   Image (NetVips load, SmartCropper, YuNet face detection) ·    │
│   Jobs (generic IJobEngine<TKey,TState> — zero project refs)    │
└───────────────────────────────────────────────────────────────┘
```

**Responsibilities:**
- **Domain layer** owns the *definitions*: what a Recipe is, how it is stored, how a `.recipe` is packaged; what a Setting is and where it lives.
- **Application layer** owns *execution*: turning a Recipe + a Run request into Jobs, running the 4-phase pipeline, tracking/persisting Job state, resuming after a crash, exposing the `IService` façade. `SlideGenerator.Jobs` owns *generic* execution mechanics (concurrency, checkpoint, resume) with no domain knowledge.
- **Foundation** owns *capabilities*: document I/O, templating, image processing, HTTP/cloud, logging.
- **Host/UI** owns *presentation and process lifecycle*: the window, navigation, ViewModels, the in-process event bus implementations, DI wiring, startup (single-instance, DB migration, Velopack, splash), shutdown.
- **Dependencies flow strictly downward.** Each module has a `Registration.cs` DI entry point. `SlideGenerator.Jobs` has no project references at all.

**On disk but not in the solution (orphans — treat as non-existent):** `src\SlideGenerator.Coordinator\`, `tests\SlideGenerator.Acquisition.Tests\`. Neither is in `SlideGenerator.slnx` or `git ls-files`. "Coordinator" is not a live V2 concept.

**Rendering / MVVM specifics for the designer:**
- One `MainWindow`, content-swapped between `SplashViewModel` (only if startup exceeds ~400 ms) and `ShellViewModel`.
- `ShellViewModel` holds exactly four page ViewModels, resolved lazily and cached; `CurrentPage` points at one. No `INavigationService`.
- Recipes/Runs sit in a title-toolbar nav pill; Settings/About are separate icon buttons.
- Localization: every string is an i18n key (`recipes.recipe.name` style); live language switch works via a converter keyed off a `Revision` counter (indexer bindings do not live-refresh in this app's compiled-binding pipeline).

---

## 18. Terminology

| Term | Definition in V2 |
|---|---|
| **Recipe** | A persisted, named, reusable binding configuration: a flat list of Mappings. Holds no data or output. `Recipes` table row. |
| **Mapping** | One element of a Recipe: one template slide + its text rules + its image rules + one-or-more worksheet data sources. Unnamed value object. |
| **WorksheetSource** | One worksheet as a data feed, with an optional column subset and row filter. |
| **RowFilter** | How rows of a worksheet are selected: All, an inclusive 1-based index range, or one block of an N-way partition. |
| **Template** | Not an entity — a `(presentation file, 1-based slide index)` reference (`PresentationSource`). Cloned once per data row at generation. |
| **Placeholder** | A Mustache tag (`{{Name}}`) in a template shape's text; a text binding target. |
| **Image shape** | A template shape that already contains a picture; an image binding target. Identified by shape name. |
| **TextInstruction** | Rule: first non-empty value across a column list → every placeholder in a tag set. |
| **ImageInstruction** | Rule: first non-empty image source across a column list → every shape in a shape set, cropped via an ordered `RoiOption` chain, with a fallback image. |
| **RoiOption** | A crop strategy: anchor-based (`Image`/`Face`/`Eyes`/`Nose`/`Mouth`, geometry + face detection) or interest-based (`Entropy`/`Attention`/`Low`/`High`/`All`, content-aware). Tried in order. |
| **Recipe Editor** | The inline page state (Guided wizard or Advanced layout) for authoring a Recipe. Not a shell destination. Not a slide editor. |
| **Guided / Advanced** | Two layouts of the same Recipe Editor. Guided = a 4-step wizard (Template → Data → Binding → Review). Advanced = everything at once + mapping navigator + shape inspector. |
| **Binding state** | `Assigned` / `Suggested` (auto-matched, unconfirmed) / `NeedsSelection` (ambiguous) / `Unassigned`. |
| **Run** / **Request** | One user-initiated generation of a Recipe. `requestId` (GUID string). `Requests` table row (write-once). |
| **Generate** | Pressing "Start" in the Run dialog → `IService.CreateAsync` → spawns Jobs; returns immediately. |
| **Job** | One unit of generation = one Mapping × one WorksheetSource = one output file. `JobId` = 0-based ordinal int within its Run. `Jobs` table row (current state). |
| **JobSpecification** | The fully-resolved, self-contained description of a Job (no Recipe lookup needed to run or resume). |
| **Phase** | One of `CreatingOutput → CreatingSlides → FillingText → FillingImages → Done`. Forward-only. |
| **CurrentIndex** | Rows completed within the current phase; the resume position with `Phase`. |
| **TotalRows** | Filtered row count of the Job's worksheet; drives the determinate progress bar. |
| **Job Engine** | The generic, domain-free scheduler (`SlideGenerator.Jobs`). |
| **Job Workload** | The slide-specific 4-phase pipeline (`SlideGenerationWorkload`). |
| **Summary** | A per-Run snapshot returned by the list APIs (request-level fields + a dict of `JobSummary`). Rebuilt on every call. |
| **RowProgress** | A live-only, unpersisted per-row activity event (status, stage, note). |
| **Setting** | The single global config tree: Appearance (UI), Performance (generation), Network (generation). `UserSettings.json`. |
| **`.recipe`** | A portable zip package: the Recipe JSON + bundled workbook/presentation files. Import creates a new Recipe. |
| **Data.db** | The one shared SQLite database: `Recipes` + `Requests` + `Jobs`. |
| **`MaxConcurrentJobs`** | The only concurrency control — how many Jobs run their workload at once. Default 5. |

### Misleading V1 terms — do NOT carry into V2

| V1 term | Why it must not appear in V2 UI/IA |
|---|---|
| **Studio** | No such concept. There is no 3-tab studio, no combined generation workspace. Rejected in `plans/status.md`. |
| **Run / Check** (the old paired UI areas) | Removed per `plans/idea.md` §8 — replaced by record/text/image *count* statistics on the recipe detail. |
| **IPC sidecar / JSON-RPC / StreamJsonRpc / `SlideGenerator.Stdio`** | The whole IPC layer is gone. Everything is one in-process app. |
| **Workflow / WorkflowCore / "workflow engine"** | Replaced by plain Task-based Jobs. The `Logs\Workflows\` folder name is a vestigial path, not a concept. |
| **Node / Edge / recipe graph** | The Recipe is a flat list. There is no graph editor (rejected). |
| **Coordinator / concurrency gates / performance calibration** | Removed. Only `MaxConcurrentJobs`. |
| **Acquisition / Collector** | Modules deleted. |
| **Cryptography module** | Folded into Utilities. |

---

## 19. V1 vs V2 Differences

The design agent may have V1 screenshots. These will mislead:

```
V1                                          →   V2
─────────────────────────────────────────────────────────────────────────────
Tauri web frontend + Rust host              →   single Avalonia .NET desktop app
Backend as a JSON-RPC IPC sidecar (Stdio)   →   backend runs in-process; no IPC
WorkflowCore-driven job execution           →   plain Task-based Job Engine + Workload
Recipe = Node/Edge graph                    →   Recipe = flat List<Mapping>
"Studio" (multi-tab generation workspace)   →   no Studio; inline Recipe Editor
                                                inside the Recipes page
3-tab Studio / wizard-separate-from-editor   →  Guided/Advanced are two layouts of ONE editor
"Run" + "Check" panels on a recipe          →   record / text / image count stats
Coordinator: 3 concurrency gates + a         →   one setting: MaxConcurrentJobs (default 5)
  hardware/network calibration system
Per-purpose DBs (Workflows.db, Recipes.db,   →   one Data.db (Recipes | Requests | Jobs)
  Cache.db, Studio.db)
SHA-256 in a Cryptography module             →   Utilities/Sha256.cs
Acquisition + Collector modules              →   deleted
```

**V1 assumption the IA must not reproduce:** "the user works in a Studio to build and run a generation." **V2 reality:** the user manages a flat list of Recipes on one page (with an inline editor), and a flat list of Runs on another. Generation is a modal dialog, not a workspace.

**V1 assumption:** "a Recipe maps to one template / one output." **V2 reality:** a Recipe has N Mappings, each with its own template slide and 1..N worksheet sources; a Run of it produces N × (sources) output files.

**V1 assumption:** "there is a Run view and a Check view." **V2 reality:** replaced by simple counts; see `plans/idea.md` §8.

---

## 20. Domain Diagram

```
                       ┌──────────────────────────────┐
                       │   Recipe   (SQLite row)      │
                       │   Id · Name · timestamps     │
                       └───────────────┬──────────────┘
                                       │ owns 1..N (value objects)
                                       ▼
                       ┌──────────────────────────────┐
                       │   Mapping  (unnamed)         │
                       └───┬───────────┬───────────┬──┘
             references 1  │       owns │ 0..N     │ owns 0..N
                           ▼            ▼           ▼
        ┌──────────────────────┐  ┌───────────┐  ┌──────────────────┐
        │ PresentationSource   │  │   Text    │  │  ImageInstruction│
        │ = template .pptx +   │  │Instruction│  │  (+ RoiOption[]  │
        │   1-based slideIndex  │  └───────────┘  │   + fallback img)│
        └──────────┬───────────┘        │        └──────────────────┘
                   │ owns 1..N          │
                   │                    ▼ keyed by
          ┌────────────────┐     Mustache tags / shape names
          │ WorksheetSource│     scanned from the template slide
          │  workbook +    │
          │  sheet +       │
          │  UsedColumns?  │
          │  RowFilter?    │
          └────────────────┘

  ─────────────────────  R U N   T I M E  ─────────────────────

   User → Run dialog → Request(RecipeId, Name, OutputType, SaveFolder)
                          │  IService.CreateAsync
                          │  Service.BuildJobs = Mappings × Sources
                          ▼
        ┌───────────────────────────────────────────────┐
        │  Request (Run)   requestId (GUID)              │
        │  Requests table row (write-once) + .log file   │
        └───────────────┬───────────────────────────────┘
                        │ owns 1..N
                        ▼
        ┌───────────────────────────────────────────────┐
        │  Job    (RequestId, JobId ordinal)            │
        │  JobSnapshot row: JobStatus · JobPhase ·       │
        │    CurrentIndex · TotalRows · JobSpecification │
        │  (fully resolved — no Recipe lookup)           │
        └───────────────┬───────────────────────────────┘
                        │ runs, forward-only
                        ▼
   CreatingOutput → CreatingSlides → FillingText → FillingImages → Done
   (copy template,  (1 cloned slide  (compose      (download/local,
    strip slides)    per data row)    Mustache)     crop, set image)
                        │ writes                        │ live events
                        ▼                               ▼
              one output .pptx per Job         RequestProgress / JobSnapshot /
              under {SaveFolder}\{book}\        RowProgress  →  ProgressHub  →
              {sheet}{ext}                      Runs page (250 ms coalesced)

   Job status:  Pending → Running ⇄ Paused → { Complete | Cancelled | Error }
   Control:     Pause/Resume/Stop  — at the REQUEST level only, never per-Job
   Crash:       non-terminal Jobs auto-resume from (Phase, CurrentIndex) at startup
```

---

## 21. Design Constraints

Things the UI/UX design **must** respect, each backed by the code:

**Recipe & Template**
- A Recipe is **not** tied to a single template. It holds N Mappings, each with its own `(presentation file, slide index)`. Design the editor and the recipe detail for the multi-Mapping case.
- Templates are file references, not managed entities. There is no template library to browse. Selecting a template = pick a file, then pick a slide.
- The same template file / slide is freely reusable across Recipes and Mappings.
- Only two kinds of thing in a template slide are bindable: Mustache `{{tags}}` in text, and shapes that already contain a picture. An empty box is invisible to the editor — the design should tell the user their template shape needs a placeholder image.
- Within one Run, a given worksheet can feed at most one Mapping (output path collision). The editor / run preview must surface this.

**Recipe Editor**
- One editor, two layouts (`IsGuided`). Guided is a linear 4-step wizard reusing the same panels. New recipe → Guided; existing → Advanced. "Open Advanced" is one-way and **not remembered per recipe** (no storage for that preference — **Unknown / Not determined from the code** whether it ever will be).
- Binding has four states (`Assigned` / `Suggested` / `NeedsSelection` / `Unassigned`) shared between text and image panels; the summary count ("N ghép · N đề xuất · N cần chọn · N chưa gán") is a first-class UI element.
- The editor persists a strictly 1:1 model (one placeholder ↔ one column). N-column fallback chains from imports are silently flattened on re-save — the design should at least not pretend the editor round-trips them.
- Save is nearly unvalidated (dirty + name). Only "Save and run" blocks on missing template / unresolved bindings.
- The editor lives **inside the Recipes page**, not as a separate nav destination.

**Runs & Execution**
- Generation is **asynchronous**. "Start" returns instantly; work happens in the background and is observed via a live progress stream + periodic list reloads.
- A Run expands into N Jobs, one output file each. The design must show a Run→Jobs hierarchy.
- Control (Pause / Resume / Stop) is **Request-level only**. There is no per-Job pause/stop. Job rows in the UI are read-only.
- Pause/stop granularity is "between rows" — a paused Job finishes its current row first.
- Jobs **auto-resume after an app crash** from `(Phase, CurrentIndex)`. The Runs page must handle Jobs that were mid-flight last session and are now running again.
- Determinate progress needs `TotalRows` (nullable) — fall back to indeterminate when null.
- **There is no Request-level error state.** An all-failed Run shows as `Complete` in the "Done" filter; the only error signal is per-Job `Error` inside the detail. Design the Runs page to make Job-level failure discoverable despite the missing aggregate.
- `RunStatusFilter` = All / Running / Paused / Done / Cancelled — no "Errored" chip exists.
- Do **not** design chips/states for `JobPhase.Queued`, `RowStatus.Waiting`, `RowStatus.Error`, or `RowStage.CroppingImage` — declared but never emitted.
- Row-level detail (activity line: which URL is downloading, etc.) is **live-only, never persisted**. A completed Run has no per-row history — only Job phase/index and the `.log` file.
- Output files and `.log` files are never auto-deleted, even when a Run is deleted. Per-job download caches are deleted on Job completion.
- The Run dialog must live-preview the exact Job fan-out and every output-path conflict (`PreviewAsync`/`PlannedJob`/`ConflictKind`) and disable "Start" on any conflict.
- The `AllowLocalPaths` checkbox is currently a **no-op** — local paths always work regardless.

**Settings**
- Three groups: Appearance (**UI only**), Performance (**affects generation** — `MaxConcurrentJobs`), Network (**affects generation** — proxy, retry, download cap).
- Every change persists **immediately** — no Save button anywhere. Design accordingly (no dirty/save affordance).
- Reset is **per-group** ("restore defaults"), not whole-app.
- No validation on numeric fields — the design's input controls are the only guardrail.
- Theme (System/Light/Dark), language, and reduced-motion all apply **live, no restart**.
- Nothing about window state, last page, or recent folders is persisted.

**System model**
- Local-first, **not offline**. Recipes/Runs/Settings are fully local (SQLite/JSON). The network is required for: row image downloads (HTTP + Google Drive only), the About page's contributor/sponsor lists, and app updates (Velopack/GitHub Releases). Degrade gracefully when offline; do not present the app as network-free.
- Single-instance app (second launch exits).
- Windows-first (`explorer.exe` for "open folder"; `%LOCALAPPDATA%` / `%TEMP%` layout; a portable build variant exists).
- Syncfusion licence is required at runtime for any generation or template/workbook preview.
- About page must never show an error state for a failed network fetch — empty lists instead.
- Developer role badges (crown / computer / paint from `plans/idea.md`): the login→role map was **never supplied** — **Unknown / Not determined from the code**. The `Contributor` model has no role field.

**Vocabulary**
- Use: Recipe, Mapping, Run, Job, Template, Placeholder, Binding, Phase, Settings, Import/Export.
- Never use: Studio, Workflow, Node/Edge, Coordinator, IPC/sidecar, the paired "Run/Check" panels.
