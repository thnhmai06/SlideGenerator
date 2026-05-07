# Implementation Plan: Reliability, Security, Logging Refactor, and Hashing Module

## 1. Objective
Giải quyết các vấn đề về bảo mật (Path Traversal, mã hóa mật khẩu), tăng độ tin cậy của tiến trình (Idempotency tải ảnh, xử lý cột trùng lặp), tái cấu trúc hệ thống Logging chuyển từ Database sang lưu trữ File, và thêm module Hashing chuyên biệt. Đảm bảo toàn bộ dự án tuân thủ tiêu chuẩn "Thuần .NET, không phụ thuộc ASP.NET Core".

## 2. Các thay đổi chính (Scope & Impact)

### Phase 1: Module `SlideGenerator.Hash` (Mới)
*   Tạo mới project `SlideGenerator.Hash` (Class Library, thuần .NET).
*   **`FileHasher`**: Expose API `ComputeSha256(string filePath)` sử dụng `System.Security.Cryptography.SHA256` để băm tệp tin.
*   **`HashRegistry`**: Dịch vụ Singleton (Thread-safe) cung cấp lưu trữ dạng Dictionary để map/lưu lại các hash của đường dẫn file phục vụ sử dụng sau này.
*   Đăng ký module thông qua `Registration.cs`.

### Phase 2: Cấu hình và Bảo mật (Settings & Security)
*   **Bảo mật mật khẩu (`Proxy.Password`)**:
    *   Tạo `CryptoHelper` sử dụng `System.Security.Cryptography.Aes` (thuần .NET). Khóa mã hóa sẽ được dẫn xuất (derive) từ một định danh cố định của máy (ví dụ: `Environment.MachineName`) hoặc một app-specific salt để đảm bảo chạy cross-platform mà không phụ thuộc vào `DataProtection` của ASP.NET.
    *   Canh thiệp vào quá trình Load/Save của `SettingManager` để giải mã/mã hóa `Proxy.Password` trước khi lưu xuống YAML.
*   **Path Traversal Validation**:
    *   Trong `SettingManager.Update()`, thực hiện kiểm tra quyền ghi (Write-Access) bằng cách thử tạo và xóa một dummy temp file tại đường dẫn `Temp.FolderPath`.
    *   Nếu không có quyền truy cập (`UnauthorizedAccessException`), từ chối lưu cấu hình. Các đường dẫn hợp lệ được người dùng chỉ định (kể cả ổ đĩa khác) vẫn sẽ qua nếu có quyền.
*   **Định dạng thư mục Temp**:
    *   Loại bỏ logic `Directory.CreateDirectory` bên trong DTO `TempSetting`. Việc này sẽ được chuyển sang các class thực hiện thao tác I/O (`DownloadService`, `ExtractData`).
    *   Sửa `GetDownloadDir` và `GetEditDir`: Nhận tham số `bookPath` thay vì `bookName`. Gọi module Hash để băm `bookPath`, lấy 7 ký tự đầu. Định dạng kết quả thành: `{normalizedBookName}_{7charHash}/Download`.

### Phase 3: Độ tin cậy của Pipeline (Reliability)
*   **Xử lý trùng lặp cột Excel (`ExtractData.cs`)**:
    *   Thay thế `ToDictionary` bằng vòng lặp. Nếu cột đã tồn tại trong Map, hệ thống sẽ bỏ qua và chỉ lấy dữ liệu của cột đầu tiên tìm thấy.
*   **Kiểm tra toàn vẹn tệp tin (`DownloadImage.cs`)**:
    *   Cải thiện Idempotency: Khi khôi phục phiên (file ảnh đã tồn tại), hệ thống sẽ đọc byte và thử dùng `Utilities.Decode()` để chuyển sang `MagickImage`.
    *   Nếu ảnh bị hỏng (ném exception) hoặc rỗng, file sẽ bị xóa và bắt buộc tải lại. (Sẽ đảm bảo `using` để giải phóng memory ngay).

### Phase 4: Tái cấu trúc Logging
*   **Cập nhật Mô hình Dữ liệu**:
    *   Bổ sung `string Name` vào `Recipe` (hoặc `GeneratingRequest`) để dùng cho việc đặt tên log file.
    *   Sửa `LogEntry` trong DB: Xóa các cột `Message`, `Level`, `Error`. Giữ lại `Id`, `TaskId` (mã của Job), `Timestamp`, và `LogFilePath` (đường dẫn tới file log).
*   **Log ra tệp vật lý (`WorkflowDatabaseSink.cs`)**:
    *   Thay vì insert từng dòng vào CSDL, Sink sẽ append các dòng log ra tệp vật lý: `{TempFolder}/TaskLogs/{Recipe.Name}_{Timestamp yyyy-MM-dd HH-mm-ss}.log`.
    *   Chỉ tạo **một** record trong bảng `LogEntries` cho mỗi `TaskId` lưu lại đường dẫn `LogFilePath` này.
*   **API Xóa Log (Clear Log)**:
    *   Tạo hàm `DeleteTaskLog(string taskId)` trong tầng Logging.
    *   Hàm này sẽ xóa file `.log` vật lý và xóa record `LogEntry` tương ứng trong CSDL.
    *   **Lưu ý**: Hành động này độc lập và KHÔNG xóa Task/Job gốc (chỉ xóa dấu vết log). Khi API bên ngoài xóa Job, nó sẽ tự động gọi hàm này.

### Phase 5: Cập nhật tài liệu
*   Sửa đổi file `INSTRUCTIONS.MD`: Loại bỏ quy tắc "Domain Purity" (Application must NOT reference Infrastructure implementations), cho phép sử dụng trực tiếp các thư viện Engine (Syncfusion) bên trong Workflow/Activities.

## 3. Testing & Verification
*   **Unit Tests**: Bổ sung test cho bộ mã hóa AES (CryptoHelper) để đảm bảo chuỗi ban đầu và sau giải mã khớp nhau.
*   **Integration Tests**:
    *   Chạy Job tạo log và kiểm tra xem file log vật lý có được tạo đúng thư mục với tên định dạng `{Recipe.Name}_{Timestamp}.log` hay không.
    *   Gọi `DeleteTaskLog` và xác minh cả file lẫn record trong DB đều biến mất.
    *   Cố tình đặt hai cột Excel trùng tên và kiểm tra xem pipeline có lấy cột đầu tiên thành công mà không văng lỗi.