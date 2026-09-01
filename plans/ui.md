# SlideGenerator V2 Desktop Frontend — Status Summary

Bàn giao cho agent khác. Đây là bản tóm tắt của plan gốc (dài ~1400 dòng) tại
`C:\Users\haith\.claude\plans\h-y-l-p-plan-cho-pure-puzzle.md` trên máy tác giả — nếu cần đào sâu một quyết định
cụ thể (lý do, phương án đã bỏ, benchmark ảnh render_q1...), đọc file đó. File này đủ để tiếp tục làm việc mà
không cần đọc lại toàn bộ plan.

## Bối cảnh

Backend V2 (`SlideGenerator.Generator`/`.Recipe`/`.Settings`/... — 10 module domain, xem `CLAUDE.md` ở root repo)
đã hoàn chỉnh, chạy in-process (không IPC sidecar nữa). `SlideGenerator.Desktop` là frontend Avalonia — lúc bắt
đầu plan này chỉ là khung rỗng (`MainWindow` là `TextBlock` placeholder). Plan này xây toàn bộ frontend đó.

Mô hình domain: **Recipe** = danh sách phẳng các **Mapping** (không phải graph/Node/Edge — model đó đã bị xoá
khỏi backend có chủ đích). Một Mapping = 1 template slide + N `WorksheetSource` (dữ liệu) + text/image
instructions. `Service.BuildJobs` fan-out `Mappings × Sources` thành job list.

## Trạng thái: TẤT CẢ ĐÃ XONG (P-1 → P5, task 1-38)

Không còn task nào treo trong plan. Build xanh, 523/524 test toàn solution qua (1 skip do thiếu secret
`SYNCFUSION_LICENSE_KEY`, pre-existing, không liên quan frontend).

### Theo phase

- **P-1** — Cổng validate UX: xác nhận `ProgressBar` ngang thắng `PhaseRing` (ring thua ở cả 16px/40px, xem
  benchmark trong plan gốc §4.1), Guided bước ③ phải chia 2 nhóm con Văn bản/Ảnh, Advanced giữ nguyên §5.2.b.
- **P0** — 5 thay đổi backend nhỏ (đã duyệt trước khi làm UI, xem "Backend contracts" bên dưới).
- **P1** — Shell: design tokens (`Resources/Primitives.axaml`/`Semantic.axaml`/`Tokens.axaml`/`Controls.axaml`),
  font (Inter + JetBrains Mono NL), `LocalizationService`+`TrExtension`, `ThemeService`, `MainWindow`+
  `ShellViewModel` (3 đích: Recipes/Runs/Settings), `SplashView` (animation lockup, không chặn UI thread nữa),
  `ViewLocator`, `IDialogService`/`IFilePicker`.
- **P2** — Runs (đọc trước, ít rủi ro nhất): `ProgressHub` (coalesce event bus + log, `DispatcherTimer` 250ms,
  subscribe **trước** `service.InitializeAsync()` — thứ tự bắt buộc, xem Threading bên dưới), `RunsViewModel`/
  `RunsView` master-detail.
- **P3** — Recipes list + Run dialog: CRUD qua `IRecipeRepository`, export/import `.recipe`, `RunDialogView` +
  `IService.PreviewAsync` (preview job list + conflict).
- **P4** — Recipe editor (phần khó nhất, chia 6 lượt con P4.1-P4.6): coordinator (`RecipeEditorViewModel` +
  `MappingEditSession`), canvas overlay + auto-bind 4 mức (Exact/Normalized/Ambiguous/None), text bindings,
  worksheet sources, mapping navigator (thêm/xoá/đổi thứ tự), inspector (ROI reorder + fallback image),
  double-click canvas để gán cột nhanh, Save/dirty-tracking/validation (chặn "Lưu và chạy" khi còn Ambiguous,
  KHÔNG chặn "Lưu" thường).
- **P4.5** — Guided mode: `IsGuided` flag (không phải ViewModel thứ hai) + `GuidedStep` enum (Template→Data→
  Binding→Review), recipe mới mặc định Guided, recipe có sẵn mặc định Advanced.
- **P5** — Settings page (Giao diện/Hiệu năng/Mạng + Giới thiệu), bàn phím (Ctrl+S/Ctrl+F/Delete/Esc), audit
  accessibility + pre-flight §10 (xem "Gap còn lại" bên dưới).

## Kiến trúc & quy ước quan trọng

- **MVVM feature-folder** dưới `src/SlideGenerator.Desktop/Features/{Recipes,Runs,RecipeEditor,Settings}/`
  (`Views/`, `ViewModels/`, `Models/`) — khác quy ước 10 module domain (xem `CLAUDE.md`), vì đây là UI code.
- **`ShellViewModel`** giữ 3 page ViewModel làm property, `CurrentPage` trỏ 1 trong 3 — không
  `INavigationService` (3 đích cố định).
- **`ProgressHub`** (`Services/Progress/`) là điểm gom duy nhất giữa `GeneratingEventBus`/`LogNotifier` (bắn từ
  thread nền, không throttle) và UI. ViewModel không bao giờ chạm event bus thô.
- **`LocalizationService`+`TrExtension`** (`Services/Localization/`): đổi ngôn ngữ live không cần khởi động lại.
  **Lưu ý kỹ thuật quan trọng** — binding indexer (`Binding("[key]")`) KHÔNG live-refresh được trong pipeline
  compiled-binding của app này dù `PropertyChanged` bắn đúng convention; phải bind qua một named property thường
  (`Revision`, int) + `IValueConverter` (`Converters/LocalizedTextConverter.cs`) tra lại theo `ConverterParameter`.
  Xem doc comment đầy đủ trong `TrExtension.cs` nếu định sửa cơ chế này.
- **`ThemeService`** (`Services/Theme/`) áp `Setting.Appearance.Theme` → `Application.RequestedThemeVariant`, và
  từ P5 audit cũng áp `ReducedMotion` → zero/restore 2 resource `MotionUi`/`MotionBrand` (`Application.Current
  .Resources["MotionUi"]`, DynamicResource nên mọi nơi dùng token này tự cập nhật).
- **Recipe editor**: `RecipeEditorViewModel` là coordinator thật, dựng 3 VM con (`SlideCanvasViewModel`/
  `TextBindingsViewModel`/`WorksheetSourcesViewModel`). `MappingEditSession` bọc 1 `Mapping` + touched-set
  (HashSet shape/placeholder đã được user xác nhận) — danh tính ổn định qua lại giữa các mapping, khác `Mapping`
  record (so sánh theo giá trị). Dirty-tracking dùng event `Changed` tường minh bắn tại đúng điểm sửa (không
  dùng record-equality — `IReadOnlyList`/`IReadOnlySet` so theo tham chiếu, sai).
- **`IJobEngine`/`IJobRunner`**: điều khiển chỉ ở cấp request (`Stop/Pause/Resume(requestId)`), không có API
  cấp job — hàng job trong UI luôn read-only.

## Backend contracts frontend phụ thuộc (đã làm ở P0, đọc để hiểu vì sao UI như vậy)

| Method/field | Ở đâu | Vì sao UI cần |
|---|---|---|
| `IReadOnlySlide.SlideSize` | `SlideGenerator.Document/Presentations/Components/Slide.cs` | Tính scale overlay canvas từ px @96 DPI |
| `Service.FindDuplicateOutputPath` (static, thuần) | `SlideGenerator.Generator/Service.cs` | Run dialog + `CreateAsync` dùng chung logic phát hiện trùng path nội bộ 1 request |
| `Setting.AppearanceSetting` (Theme/Language/ReducedMotion) | `SlideGenerator.Settings/Mutable/` | Chỗ lưu duy nhất cho theme/ngôn ngữ/reduced-motion |
| `includeLogs` param trên `ListActiveAsync`/`ListCompletedAsync` | `SlideGenerator.Generator/Service.cs` | Runs list không đọc/parse toàn bộ `.log` mọi request chỉ để vẽ list |
| `IService.PreviewAsync` + `PlannedJob`/`ConflictKind` | `SlideGenerator.Generator/Service.cs` | Run dialog hiện trước N file sẽ tạo + xung đột, không tự chế logic riêng |

## Gap còn lại (không phải bug ẩn — đều đã ghi rõ, không chặn dùng)

1. **`ShellView`'s `CrossFade` page-transition** không tôn trọng Reduced motion — `CrossFade.Duration` là CLR
   property thường, không bind được qua `DynamicResource`. Sửa đúng cần viết page-transition tuỳ biến.
2. **`ItemsControl` (log lines trong `RunsView`, `PlannedJobs` trong `RunDialogView`)** không ảo hoá — `ListBox`
   (Recipes/Runs master list) thì có (mặc định Avalonia). Job chạy lâu có thể sinh vài nghìn dòng log không lag
   ngay nhưng không optimal. Sửa cần đổi scroll ownership (không phải 1 dòng).
2b. **Guided mode không nhớ theo từng recipe** — bấm "Mở chế độ nâng cao" rồi đóng, mở lại vẫn theo rule mặc
   định (có id → Advanced). Bỏ có chủ đích (domain model không có chỗ lưu preference này); thêm khi có người
   thực sự cần.
3. **4 mục pre-flight §10 không tự động verify được trong môi trường dev hiện tại** (thiếu fixture, không phải
   app lỗi):
   - Recipe thật end-to-end với `.pptx`/`.xlsx` thật (overlay khớp pixel) — không có fixture trong
     `tests/fixtures/data/`.
   - Multi-job progress đồng thời, và crash-resume (kill app giữa chừng) — cần fixture + chạy thật.
   - Đo tương phản màu 4.5:1 — không có tool đo màu sẵn trong môi trường.
   - Test đa độ phân giải (1024/1440/1920px) bằng ảnh chụp thật — chưa làm, ngoài phạm vi thời gian audit gần
     nhất; code responsive theo token hệ thống nên rủi ro thấp nhưng chưa xác nhận bằng mắt.

## Việc còn có thể làm tiếp (không phải nợ kỹ thuật, chỉ là mở rộng tương lai — xem §12 plan gốc "Đã cân nhắc và bỏ")

Node/edge graph editor, wizard tách khỏi editor, 3-tab Studio, undo/redo stack, `INavigationService`, Rx/
DynamicData, Svg.Skia, command palette, headless UI test, điều khiển cấp job, lịch sử row-level — tất cả đã cân
nhắc và bỏ có lý do rõ ràng trong plan gốc §12, không phải thiếu sót. Chỉ làm khi có nhu cầu thật xuất hiện.

## Test & build

```
dotnet build SlideGenerator.slnx           # phải xanh
dotnet test SlideGenerator.slnx            # 523/524 (1 skip = thiếu SYNCFUSION_LICENSE_KEY)
dotnet test tests/SlideGenerator.Desktop.Tests/SlideGenerator.Desktop.Tests.csproj  # 95/95
```

Smoke test UI qua windows-mcp: xem git log các commit gần đây (`git log --oneline -20` trên branch `develop`)
để biết đúng những gì đã test bằng tay qua app thật (mở app, click qua các trang, gõ liệu thật) — mỗi commit
feat/fix của phase này đều có ghi chú smoke test chi tiết trong message hoặc trong plan gốc.
