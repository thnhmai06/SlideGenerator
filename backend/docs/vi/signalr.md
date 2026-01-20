# SignalR API

[🇺🇸 English Version](../en/signalr.md)

Backend cung cấp một API thời gian thực thông qua SignalR hubs. Mọi giao tiếp đều tuân theo mẫu request/response kèm theo các thông báo (notification) bất đồng bộ.

## Các Hub Endpoint

| Endpoint | Mô tả |
| :--- | :--- |
| `/hubs/job` | Endpoint chính để tạo, điều khiển và truy vấn job. |
| `/hubs/sheet` | Tiện ích để kiểm tra Excel workbook (tiêu đề, dòng dữ liệu). |
| `/hubs/config` | Đọc và ghi cấu hình backend. |

> **Lưu ý:** `/hubs/task` là alias cũ (legacy) của `/hubs/job`.

## Giao thức

### Mẫu Request
Client gửi yêu cầu bằng cách gọi phương thức `ProcessRequest` trên Hub với payload JSON.

- **Trường bắt buộc:** `type` (chuỗi ký tự, không phân biệt hoa thường).
- **Phản hồi:** Được gửi lại qua sự kiện `ReceiveResponse`.
- **Lỗi:** Trả về message với type là `error`.

## Job Hub Messages (`/hubs/job`)

### 1. Tạo Job (`JobCreate`)

Tạo một tác vụ tạo slide mới.

**Group Job (Workbook + Template):**
```json
{
  "type": "JobCreate",
  "jobType": "Group",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output",
  "sheetNames": ["Sheet1", "Sheet2"],
  "textConfigs": [
    { "pattern": "{{Name}}", "columns": ["FullName"] }
  ],
  "imageConfigs": [
    {
      "shapeId": 4,
      "columns": ["Photo"],
      "roiType": "RuleOfThirds",
      "cropType": "Fit"
    }
  ],
  "autoStart": true
}
```

**Sheet Job (Single Sheet):**
```json
{
  "type": "JobCreate",
  "jobType": "Sheet",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output\\Sheet1.pptx",
  "sheetName": "Sheet1"
}
```

### 2. Điều khiển Job (`JobControl`)

Quản lý trạng thái của các job đang chạy.

- **Hành động:** `Pause`, `Resume`, `Cancel`, `Stop` (giống Cancel), `Remove` (xóa khỏi lịch sử).

```json
{
  "type": "JobControl",
  "jobId": "GUID-ID-HERE",
  "jobType": "Group",
  "action": "Pause"
}
```

### 3. Truy vấn Job (`JobQuery`)

Lấy chi tiết job.

- **Phạm vi (Scope):** `Active`, `Completed`, `All`.
- **includePayload:** Trả về JSON payload gốc (được tái tạo từ DB).

```json
{
  "type": "JobQuery",
  "jobId": "GUID-ID-HERE",
  "jobType": "Group",
  "includeSheets": true
}
```

### 4. Quét Template (Scan Template)
Các tiện ích để kiểm tra file PPTX.
- **Hành động:** `ScanShapes`, `ScanPlaceholders`, `ScanTemplate`.

```json
{
  "type": "ScanShapes",
  "filePath": "C:\\slides\\template.pptx"
}
```

## Thông báo (Notifications)

Client phải lắng nghe sự kiện `ReceiveNotification` để nhận cập nhật thời gian thực.

**Loại sự kiện:**
- `GroupProgress`: Tiến độ tổng thể của một group.
- `SheetProgress`: Tiến độ của một sheet đơn lẻ.
- `JobStatus`: Thay đổi trạng thái (ví dụ: Pending -> Processing).
- `LogEvent`: Log message có cấu trúc từ backend.

## Đăng ký (Subscriptions)

Để nhận cập nhật chi tiết cho các job cụ thể, client cần đăng ký:

- `SubscribeGroup(groupId)`
- `SubscribeSheet(sheetId)`

