# Công nghệ & Quy chuẩn (Tech Stack & Standards)

## Công nghệ sử dụng

| Công nghệ | Thành phần | Lý do sử dụng / Trách nhiệm |
|---|---|---|
| **.NET 10** | Core SDK | Đảm bảo hiệu suất tối đa, tính năng hiện đại (pinned trong `global.json`). |
| **StreamJsonRpc** | Giao tiếp (IPC) | Cung cấp giao thức JSON-RPC 2.0 siêu tốc và ổn định qua `stdin/stdout`. |
| **WorkflowCore** | Điều phối (Orchestration) | Quản lý logic các bước dài hạn, hỗ trợ chạy song song, chia giai đoạn, dễ dàng theo dõi tiến trình và lỗi. |
| **Syncfusion** | Tài liệu (.xlsx, .pptx) | Thao tác an toàn, nhanh chóng với tệp tin Office mà không cần cài đặt MS Office trên máy chủ. |
| **Magick.NET** | Xử lý ảnh | `MagickImage` được dùng làm kiểu mặc định để cắt, thu phóng hình ảnh nhanh gọn. |
| **OpenCVSharp** | Computer Vision | (Mô hình YuNet) Xử lý nhận diện khuôn mặt giúp cắt ảnh vào đúng trọng tâm (ROI). |
| **Serilog & EF Core** | Ghi Log & DB | Quản lý nhật ký lỗi bền bỉ vào tập tin dạng rolling và cơ sở dữ liệu. |

## Quy chuẩn mã nguồn (Coding Standards)

Dự án áp dụng các tiêu chuẩn mã hóa khắt khe để duy trì tính nhất quán và độ tinh gọn của kiến trúc:

- **Bảo toàn ranh giới Mô-đun:** Không có phụ thuộc vòng (No Circular Dependencies). Tất cả các dịch vụ được đăng ký qua tệp `Registration.cs` trong mỗi mô-đun.
- **Tiêu chuẩn Lớp dữ liệu:**
  - Sử dụng `record` hoặc `sealed record` cho các đối tượng trao đổi dữ liệu (DTOs, Value Objects).
  - Sử dụng `sealed class` cho các lớp nghiệp vụ, tiêm phụ thuộc qua Constructor (DI-Based Composition).
- **Quy tắc Async/Await:** Bắt buộc sử dụng `ConfigureAwait(false)` trên tất cả các lệnh gọi bất đồng bộ trong các mô-đun thư viện và phải truyền xuyên suốt `CancellationToken`.
- **Quản lý Hình ảnh:** Bắt buộc sử dụng `MagickImage` cho các thao tác hình ảnh. Kiểu mảng `byte[]` chỉ được phép sử dụng ở vùng biên hệ thống.
- **Xử lý ngoại lệ:** Không dừng quy trình khi gặp một lỗi đơn lẻ. Mỗi đối tượng trạng thái lưu trữ một danh sách lỗi dạng `ConcurrentDictionary<string, Exception> Errors`.
- **Tài liệu XML:** Tất cả các phương thức public, class và properties bắt buộc phải có thẻ XML comments.
