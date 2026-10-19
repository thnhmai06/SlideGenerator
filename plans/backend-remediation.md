# Kế hoạch sửa backend

Nguồn: review backend (10 phát hiện chính + một số phát hiện phụ) và rà soát kiến trúc. Chưa có thay đổi code nào được
thực hiện theo kế hoạch này. Các phát hiện đã được đối chiếu bằng cách đọc code; chưa chạy được code (không có `dotnet`
trong phiên lập kế hoạch).

## Quyết định đã chốt

1. Workflow đang pause **giữ** slot đồng thời (`MaxConcurrentWorkflows` là cơ chế kiểm soát RAM; workflow pause vẫn giữ file pptx trong bộ nhớ).
2. Ghi file đầu ra qua **file tạm + đổi tên nguyên tử**.
3. **Gộp** 3 phase (slides / text / images) thành một pipeline cho mỗi hàng.
4. Ảnh dự phòng được **đóng gói vào `Images/`** trong gói `.recipe`.
5. Thêm trạng thái request **`CompletedWithError`**.
6. **Khử trùng ảnh là bắt buộc**: giữ dict `ContentInfo` (khoá khử trùng là URI sau redirect/resolve, chỉ biết được sau
   khi inspect). Ý tưởng bỏ dict bằng cách chuẩn hoá URI offline đã bị loại.
7. Tránh HEAD lặp lại bằng dict inspect trong RAM theo chuỗi nguồn đã chuẩn hoá (xem 5.3). Không dùng alias/hard link
   trên đĩa; sau crash chỉ tốn một lượt HEAD cho mỗi URL.
8. Bỏ `latest.log` (hard link) và xoá `Utilities/HardLink.cs` — không còn nơi nào cần hard link.

**Ngoài phạm vi:** zip bomb và dọn file giải nén dở khi import lỗi, mã hoá mật khẩu đang lưu (proxy, `BookPassword`,
`PresentationPassword`), bộ nhớ của `ProgressHub` (thuộc Desktop).

**Quy trình cho mọi mục:** viết test tái hiện lỗi trước, rồi sửa, rồi test xanh (Basic Rules §4 trong `AGENTS.md`).
Thứ tự phụ thuộc: 1 → 2 → 3 → (4 song song với 5) → 6; 5.2 phải sau 3.1.

---

## Đánh giá kiến trúc: giữ khung, sửa ba trục

Quy mô: ứng dụng desktop một người dùng, một instance, một loại workflow. **Giữ nguyên** 11 module và quy tắc phụ thuộc một
chiều; không thêm module hay framework. Ba trục cần sửa:

1. **Một nơi duy nhất sở hữu vòng đời workflow.** Hiện nhiều nơi cùng ghi `WorkflowStatus` (observer luôn ép `Running`, pause ghi từ
   luồng khác, terminal ghi từ luồng workflow). Đây là gốc của #2, #3, #7.
2. **Một luồng ghi DB duy nhất, theo thứ tự.** Flush hiện chạy song song không thứ tự và mất batch khi lỗi.
3. **Ghi file đầu ra an toàn.** Hiện ghi đè thẳng `OutputPath`, xoá file cũ ngay khi bắt đầu, `Save()` khoảng 3N lần.

---

## Giai đoạn 0: chuẩn bị

- Cài .NET 10 SDK; đặt `GITHUB_USERNAME`/`GITHUB_TOKEN` để restore gói OpenCvSharp (thiếu thì chỉ chạy được test của các
  project không phụ thuộc `Image`, phần còn lại dựa vào CI).
- Chạy `dotnet test SlideGenerator.slnx` lấy mốc (`plans/status.md` ghi 548 test, 1 skip).

## Giai đoạn 1: lỗi làm hỏng app

**1.1 Logger factory** (`Logging/FileLogging/FileLoggerFactory.cs`, `Desktop/App.axaml.cs:137`)
- Lỗi: `SerilogFileLoggerFactory` giữ một `LoggerConfiguration` (Transient bị capture bởi singleton `WorkflowController`), nên
  `CreateFile` lần hai gọi `CreateLogger()` trên config đã build và ném lỗi → mọi workflow sau workflow đầu đều `Error`.
- Sửa: mỗi `CreateFile` tạo `new LoggerConfiguration()`, minimum level lấy từ options/`IConfiguration`
  (`Logging:Workflow:MinimumLevel`). Bỏ đăng ký Transient `LoggerConfiguration`; factory thành singleton.
- Test: gọi `CreateFile` hai lần trên cùng một instance, cả hai ghi được file.

**1.2 Đường dẫn log** (`Service.ResolveWorkflowLogPath`)
- Đổi thành `Logs/Workflows/{requestId}.log` (tên request vẫn nằm trong nội dung log), sink `shared: true`, xoá file log
  khi xoá request.
- Test: hai workflow cùng request ghi đủ dòng; hai request trùng tên ghi vào hai file; xoá request thì mất file log.

## Giai đoạn 2: vòng đời workflow và lưu trữ

**2.1 Máy trạng thái trong engine** (`Workflows/Engine/`, vẫn generic)
- Thêm `WorkflowLifecycle { Queued, Running, Paused, Completed, Cancelled, Faulted }`. `RunningWorkflow` giữ lifecycle, chuyển
  trạng thái dưới lock riêng của workflow; trạng thái kết thúc là cuối cùng.
- `PauseGate` báo engine khi workflow thật sự đỗ ở checkpoint; chỉ khi đó mới báo `Paused`. `IWorkflowObserver` nhận lifecycle;
  `GeneratorWorkflowObserver` ánh xạ 1-1 sang `WorkflowStatus`, **không còn ép `Running`** (sửa #2).
- Pause/stop được cả workflow `Queued`. Thêm `StopAsync(key, waitForExit)`.
- `SettingConcurrencyProvider` trả `Math.Max(1, value)`.
- Test (`WorkflowEngineTests`): pause khi row đang chạy + `ReportAsync` đến muộn không đè `Paused`, resume được; pause workflow
  `Queued`; terminal không bị hạ; `MaxConcurrentWorkflows = 0` không ném lỗi.

**2.2 `BufferedRepository`** (`Generator/Persistence/`)
- `lock` + `Dictionary` thay hoán đổi `ConcurrentDictionary` (hết mất ghi khi `Enqueue` rơi vào sau `Exchange`).
- `FlushAsync` tuần tự hoá bằng `SemaphoreSlim(1)`; flush lỗi thì trộn batch trở lại bằng `TryAdd` (bản ghi mới hơn thắng).
- SQL upsert thêm `WHERE Workflows.Status NOT IN ('Complete','Cancelled','Error')`.
- Test: hai flush đồng thời giữ thứ tự; flush lỗi rồi tick sau ghi đủ; upsert `Running` lên hàng `Complete` không đổi.

**2.3 Service** (`Generator/Service.cs`)
- `DeleteAsync` stop với `waitForExit: true` rồi mới xoá (hết hàng `Workflows` mồ côi).
- Tuần tự hoá `CreateAsync` bằng `SemaphoreSlim(1)` riêng (sửa race kiểm tra trùng output path).
- `RequestStatus` tách khỏi `WorkflowStatus`: `Running | Paused | Complete | CompletedWithError | Error | Cancelled`.
  Quy tắc: có Running/Pending → `Running`; else có Paused → `Paused`; else tất cả Cancelled → `Cancelled`; else tất cả
  Error → `Error`; else có Error → `CompletedWithError`; else `Complete`. `ListCompletedAsync` trả cả `Error` và
  `CompletedWithError`.
- `Summary.WorkflowStatus` đổi kiểu thành `RequestStatus`. **Ảnh hưởng Desktop**: `RunsViewModel`, `RunStatusFilter`,
  `RequestRunViewModel`, chuỗi `.resx` (en/vi).
- Test (`ServiceTests`): bảng `DeriveStatus` đủ tổ hợp; delete request đang chạy không để lại hàng nào.

## Giai đoạn 3: đúng dữ liệu vào/ra

**3.1 File tạm và lưu theo mốc** (`SlideGenerationRunner`, `PreflightCleanup`)
- Làm việc trên `{OutputPath}.partial`: copy template, xoá slide, **`Save()` ngay**; xong thì
  `File.Move(partial, OutputPath, overwrite: true)`. `PreflightCleanup` chỉ xoá `.partial`; file kết quả cũ chỉ bị thay
  khi workflow thành công. Resume mở `.partial`.
- Lưu mỗi K hàng hoặc T giây (hằng số, không đưa vào settings) và luôn lưu khi kết thúc; báo `CurrentIndex` với
  `durable` chỉ sau `Save()`. **Bất biến: DB không bao giờ đi trước file.**

**3.2 Hợp đồng worksheet** (`Document/Workbooks/Components/Worksheet.cs`, `Recipe/Models/RowFilter.cs`)
- `RowCount` và `GetRow` cùng tính tương đối từ đầu used range (hàng 1 là header); sheet rỗng → 0 hàng dữ liệu.
- `IndexRangeFilter` kẹp theo `[1, dataCount]`, `Start > End` cho tập rỗng; `PartitionBlockFilter` kiểm tra
  `0 ≤ PartitionIndex < PartitionCount`, `PartitionCount > 0`.
- Test: used range bắt đầu từ A3; range vượt số hàng; sheet rỗng.

**3.3 Kiểm tra đầu vào** (`PreviewAsync`/`CreateAsync`)
- Kiểm tra workbook/template/sheet tồn tại, chỉ số slide, row filter; lỗi trả theo từng workflow (mở rộng `PlannedWorkflow`);
  `CreateAsync` từ chối nếu có lỗi. Thêm `ConflictKind.OverwritesExistingFile` (chỉ cảnh báo).

## Giai đoạn 4: bảo mật, mạng, recipe

**4.1 `AllowLocalPaths`**
- Migration `004_add-allow-local-paths-to-workflows.sql` (`003` đã dùng để đổi tên bảng `Jobs` → `Workflows`); đưa trường này vào `WorkflowSpecification` và `BuildWorkflows`.
- `ImageSourcePolicy` duy nhất: chỉ `http`/`https`; đường dẫn local chỉ dùng khi cờ bật; chuỗi không phải URL hợp lệ bị
  bỏ qua (không gửi request mạng). `FallbackImagePath` thuộc recipe nên luôn được phép. Chạy **trước** inspect.

**4.2 `CloudClient`**
- Inspect: HEAD, lỗi bất kỳ (kể cả 403/404/501) thì GET với `ResponseHeadersRead`; kiểm tra status code. Dispose
  `HttpClient` khi dùng `DefaultClient()`.
- Thêm `DownloadToFileAsync(uri, path, maxBytes, ct)`: stream có giới hạn byte (vượt `MaxDownloadBytes` thì huỷ), ghi
  `{path}.tmp`. Thay `GetByteArrayAsync`.
- Bỏ phần mở rộng khỏi tên cache (libvips nhận dạng qua header) → xoá `ContentInfo.Extension`/`ExtractExtension`. Xoá
  `ExceedsMaxDownloadBytes` và test của nó; `IsImage()` giữ để lọc MIME.

**4.3 Đóng gói ảnh dự phòng**
- Thêm `RecipePackageFormat.Data.Images` (`Images/`) và `NameAndPaths.ImportedFolder.ImagesPath`.
- Export gom các `FallbackImagePath` tồn tại vào `Images/` và viết lại đường dẫn; import giải nén vào `Imported/Images/`
  (trùng tên thì thêm hậu tố như workbook).

**4.4 Null → rỗng** cho `Mappings`, `Sources`, `TextInstructions`, `ImageInstructions`, `Placeholders`, `Columns`,
`Shapes`, `RoiOptions`, khi import và khi đọc từ DB (sửa `RecipeRepository.cs:178`, hiện không có tác dụng).

## Giai đoạn 5: hiệu năng và cache ảnh

**5.1** Đọc các hàng cần dùng **một lần** vào bộ nhớ khi workflow bắt đầu, thu gọn theo `UsedColumns`.

**5.2 Pipeline mỗi hàng:** `CreatingOutput`, rồi với mỗi hàng: thêm slide → điền chữ → điền ảnh; lưu theo mốc (3.1).
Workflow cũ còn dở vẫn resume được (chỉ số lấy từ `SlidesCount`, các bước làm lại đều idempotent). Giữ nguyên giá trị enum
`WorkflowPhase` để đọc dữ liệu cũ.

**5.3 `ImageCache`** (`Generator/Workflows/ImageCache.cs`, thay `WorkflowTempFolder` và các helper tải trong runner)

Phạm vi theo **request**: `{TempRoot}/{requestId}/`. Các workflow cùng request dùng chung ảnh.

Tên file cache: `{SHA256(ContentInfo.Uri)}` (không đuôi) — **khoá đích**, tức URI sau redirect/resolve; đây là khoá
khử trùng thật. **Không có alias theo chuỗi nguồn và không dùng hard link** (đã cân nhắc và bỏ: trong một lần chạy,
dict trong RAM đã tránh HEAD lặp; sau crash chỉ tốn một lượt HEAD cho mỗi URL).

Luồng cho mỗi nguồn:

```
1. Chuẩn hoá nhẹ chuỗi nguồn (trim, thêm https:// nếu thiếu scheme, dạng chuẩn của Uri.AbsoluteUri,
   bỏ fragment, GIỮ NGUYÊN query)
2. Tra dict inspect trong RAM theo chuỗi đã chuẩn hoá → có: dùng ContentInfo đã có (không HEAD)
3. Không có: HEAD (dự phòng GET) → ContentInfo → khoá đích
4. Tra file theo khoá đích → có: dùng luôn
5. Không có: tải → .tmp → kiểm tra header ảnh → Move thành file khoá đích
```

- **Trong RAM** (cùng phạm vi request): `ConcurrentDictionary<chuỗi nguồn đã chuẩn hoá, Lazy<Task<ContentInfo?>>>` để HEAD
  chỉ chạy một lần cho mỗi nguồn trong lúc workflow đang chạy; `ConcurrentDictionary<khoá đích, Lazy<Task<string?>>>` để hai
  workflow/hàng cùng khoá chỉ tải một lần; `HashSet` các khoá đích đã thất bại để URL hỏng ở 500 hàng không bị tải lại 500
  lần (không ghi lỗi lên đĩa vì sau crash nên thử lại).
- **Kiểm tra trước khi vào cache:** tải về `{hash}.tmp` → mở bằng NetVips để đọc header (lỗi thì xoá `.tmp`, ghi vào
  tập lỗi) → `File.Move`. "Có file" luôn nghĩa là có ảnh hợp lệ; `.tmp` không bao giờ được coi là đã có.
- **Lọc MIME nới lỏng:** chỉ bỏ qua khi MIME khẳng định không phải ảnh (vd `text/html`); `application/octet-stream` hoặc
  không rõ thì vẫn tải và để bước kiểm tra header quyết định.
- **Inspect theo yêu cầu** (khi hàng cần ảnh), thay `InspectSourcesAsync` duyệt tuần tự trước mọi hàng. Tải trước song
  song có giới hạn (~4 mỗi request) cho vài hàng phía trước.
- **Dọn dẹp:** xoá thư mục request khi workflow cuối cùng của request kết thúc (cần bộ đếm workflow chưa kết thúc theo request, lấy
  từ `IWorkflowsRepository`); khi khởi động, xoá các thư mục không còn workflow chưa kết thúc. Workflow pause giữ nguyên cache.
- `GeneratorWorkflowObserver.CleanupWorkflowTempFolder` đổi thành dọn theo request qua `ImageCache`.

Test: bảng chuẩn hoá khoá nguồn (hoa/thường, cổng mặc định, fragment, thiếu scheme; query giữ nguyên); lần tra cứu thứ
hai theo cùng nguồn trong cùng lần chạy không gọi HEAD; hai nguồn khác nhau cùng ContentInfo.Uri chỉ tải một lần và dùng chung dữ liệu;
`.tmp` dở không được dùng; file không phải ảnh bị loại; URL lỗi chỉ thử một lần mỗi request; HEAD trả 403/501 thì GET
dự phòng vẫn lấy được ảnh; `octet-stream` nhưng nội dung là ảnh vẫn dùng được; vượt `MaxDownloadBytes` thì huỷ và không
để lại file; dọn thư mục khi workflow cuối kết thúc và dọn mồ côi khi khởi động.

## Giai đoạn 6: dọn dẹp

- Xoá `AnnounceExpectedWorkflowCount`, `RequestProgress` live, `RequestProgressChanged` (không ai subscribe). Sửa doc của
  `IEventBus` còn nhắc `Stdio`.
- Cập nhật `AGENTS.md` (máy trạng thái, file tạm, `RequestStatus`, `ImageSourcePolicy`, `ImageCache`, `Images/`), xoá mục
  "Known gaps" đã sửa; sửa task `run` trong `Taskfile.yml` (còn trỏ tới `SlideGenerator.Stdio`).
