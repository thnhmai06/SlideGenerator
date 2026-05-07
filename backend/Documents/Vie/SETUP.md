# Hướng dẫn cài đặt & Triển khai (Installation & Setup)

Tài liệu này hướng dẫn chi tiết các bước để thiết lập môi trường và chạy dự án SlideGenerator.

## Yêu cầu hệ thống (Prerequisites)
- **.NET 10 SDK**: Dự án được xây dựng trên nền tảng .NET mới nhất.
- **Git**: Để quản lý mã nguồn.

## Các bước thiết lập

### 1. Cấu hình biến môi trường (Environment Variables)

Đây là bước quan trọng nhất trước khi vận hành ứng dụng. Dự án sử dụng Syncfusion để xử lý tài liệu, yêu cầu phải có License Key hợp lệ.

1.  Sao chép tệp mẫu cấu hình:
    ```bash
    cp .env.example .env
    ```
2.  Mở tệp `.env` và điền khóa bản quyền của bạn:
    ```env
    SYNCFUSION_LICENSE_KEY=your_license_key_here
    ```

> **Lưu ý:** Nếu không có khóa bản quyền, các tính năng liên quan đến Excel và PowerPoint sẽ bị giới hạn hoặc hiển thị thông báo bản quyền của Syncfusion.

### 2. Phục hồi và Biên dịch

Sử dụng terminal tại thư mục gốc của dự án:

```bash
# Phục hồi các gói NuGet
dotnet restore

# Biên dịch toàn bộ Solution
dotnet build SlideGenerator.sln
```

### 3. Chạy ứng dụng

Vì dự án hoạt động như một IPC Sidecar, bạn có thể chạy tệp thực thi trực tiếp từ thư mục `bin` sau khi build:

```bash
dotnet run --project SlideGenerator.Ipc/SlideGenerator.Ipc.csproj
```

## Cấu hình bổ sung
Ngoài biến môi trường, các cấu hình chi tiết về giới hạn tốc độ (Throttling), đường dẫn lưu trữ và log có thể được tìm thấy trong `appsettings.json` hoặc cấu hình YAML thông qua mô-đun `Settings`.
