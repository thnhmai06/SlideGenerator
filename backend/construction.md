Bạn là coding agent làm việc trên backend hiện tại của SlideGenerator.  
Mục tiêu là **mở rộng/chỉnh sửa an toàn** theo kiến trúc đang có, không rewrite toàn bộ solution.

## 1) Solution hiện tại (source of truth)
Các project backend đang dùng:
- `SlideGenerator.Framework`: thư viện core logic dùng lại (slide/sheet/image/cloud services) - độc lập, có thể publish NuGet
- `SlideGenerator.Features`: domain features (Configs, Jobs orchestration, Slides/Sheets models)
- `SlideGenerator.Services`: application services (ScanService, GenerateService, DownloadService, validation/model manager)
- `SlideGenerator.Ipc`: stdio JSON-RPC host + endpoint layer

## 2) Kiến trúc và nguyên tắc bắt buộc
- Ưu tiên tái sử dụng logic đã có trong `Framework` (không duplicate nếu đã có service phù hợp).
- `Ipc` chỉ là adapter transport (JSON-RPC), không chứa business logic nặng.
- `Features` chứa domain entities/models và Jobs orchestration.
- `Services` chứa các service runtime (generate, scan, download, validation).
- Không đưa logic IO/domain vào sai layer.

## 3) Config & DI
- Chỉ `Program.cs` của IPC tạo singleton `ConfigManager`.
- Mapping config read-only hiện dùng `IConfigProvider` từ `ConfigManager` (trong `Features.Configs`).
- Không inject trực tiếp model `Config` làm dependency root trừ trường hợp read snapshot trong runtime service.
- Dùng `Microsoft.Extensions.DependencyInjection`, tránh wire service thủ công bằng `new` trong constructors.

## 4) Face detection contract (đang áp dụng)
- Model lifecycle do `FaceDetectorModelManager` ở `Services.Generating` quản lý.
- `Framework` (`YuNetModel`/`FaceDetectorModel`) không auto-init trong `DetectAsync`.
- Nếu detect khi model chưa init: throw exception.
- `Framework` trả toàn bộ detections; lọc score ở caller/business layer.

## 5) Download contract (đang áp dụng)
- Download ảnh remote trong generate pipeline phải đi qua `DownloadService` (trong `Services.Generating`, dùng thư viện `Downloader`).
- Không thêm logic tải remote ad-hoc bằng `HttpClient` trong pipeline generate.

## 6) JSON-RPC endpoint scope (hiện có)
- `system.*` (health)
- `slide.*` / `sheet.*` (scan)
- `jobs.*` (create/get/list/pause/resume/cancel + notifications)
- `configs.*` (get/reload/save/reset)

Endpoint organization hiện tại:
- Endpoint chia file partial trong `SlideGenerator.Ipc/Endpoints`.
- Request DTO đặt trong `SlideGenerator.Ipc/Contracts/Requests` hoặc `Services.Generating.Models`.
- Endpoint chỉ validate input và gọi `BackendService` (trong `Features.Jobs`).

## 7) Scanning contract
- `ScanService` (trong `Services`) phải ưu tiên gọi service trong `Framework` (`PresentationDocumentService`, `WorkbookService`, `WorksheetService`, `ShapeService`) thay vì tự parse trùng lặp.
- Kết quả scan trả model mỏng trong `Features.Slides.Models` / `Features.Sheets.Models`.

## 8) Coding style
- C# hiện đại, rõ nghĩa, thread-safe.
- Public API có XML docs trong file chạm tới.
- Không đổi tên/structure lớn nếu không cần.
- Sửa đúng phạm vi yêu cầu, tránh kéo theo refactor ngoài lề.

## 9) Validation
- Sau thay đổi, luôn build project bị ảnh hưởng trước, rồi build toàn backend nếu khả thi.
- Nếu có lỗi nền không liên quan (pre-existing), nêu rõ và tách khỏi phần thay đổi mới.
