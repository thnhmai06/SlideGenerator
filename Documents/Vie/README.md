# SlideGenerator Backend

Chào mừng bạn đến với tài liệu kỹ thuật của **SlideGenerator**, một hệ thống tự động hóa tạo PowerPoint từ Excel mạnh mẽ.

## Tài liệu chi tiết

Vui lòng tham khảo các tệp sau để hiểu rõ hơn về hệ thống:

1.  **[Kiến trúc hệ thống (Architecture)](./ARCHITECTURE.md)**: Tổng quan về mô hình Modular Monolith, IPC Sidecar và luồng dữ liệu.
2.  **[Tài liệu API & Workflow](./API_WORKFLOW.md)**: Danh sách đầy đủ các Endpoint JSON-RPC và cơ chế vận hành của WorkflowCore.
3.  **[Công nghệ & Quy chuẩn (Standards)](./STANDARDS.md)**: Tech stack sử dụng và các quy tắc lập trình bắt buộc trong dự án.
4.  **[Hướng dẫn Cài đặt & Thiết lập (Setup)](./SETUP.md)**: Các bước chi tiết để cấu hình biến môi trường và build dự án.

---

## Bắt đầu nhanh (Quick Start)

> **⚠️ Quan trọng:** Bạn cần cấu hình `SYNCFUSION_LICENSE_KEY` trong tệp `.env` trước khi chạy ứng dụng. Xem chi tiết tại [Hướng dẫn Thiết lập](./SETUP.md).

```bash
# 1. Cấu hình bản quyền
cp .env.example .env
# Chỉnh sửa .env và điền SYNCFUSION_LICENSE_KEY

# 2. Build dự án
dotnet restore
dotnet build SlideGenerator.sln
```

### Các tính năng chính
- Giao tiếp IPC siêu tốc qua StreamJsonRpc.
- Điều phối luồng công việc bền bỉ với WorkflowCore.
- Xử lý hình ảnh thông minh (ROI & Face Detection).
- Hỗ trợ đa đám mây (Google Drive, OneDrive, SharePoint).
