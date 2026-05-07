# Module: Giao diện IPC (JSON-RPC 2.0)

## The Hook (Q&A)

**Q: Sidecar giao tiếp như thế nào?**  
Ứng dụng sử dụng **JSON-RPC 2.0** thông qua standard input (`stdin`) và standard output (`stdout`). Điều này cho phép bất kỳ frontend nào (Tauri, Electron, Python) điều khiển backend .NET như một tiến trình tách biệt mà không tốn tài nguyên cho HTTP/TCP sockets.

**Q: Tiến độ được báo cáo lại như thế nào?**  
Chúng tôi sử dụng mô hình "Đẩy" (Push). Backend không đợi frontend hỏi; thay vào đó, nó tự động gửi các thông báo JSON-RPC (`workflow/progress`) tới `stdout` bất cứ khi nào một bước workflow hoàn thành hoặc thay đổi trạng thái.

---

## 1. Các phương thức RPC

Các phương thức được tổ chức theo namespace chức năng.

### Workflow (`workflow.*`)
- **`workflow.start`**: Bắt đầu một tác vụ sinh slide mới. Trả về `instanceId`.
- **`workflow.cancel`**: Chấm dứt một tác vụ đang chạy.
- **`workflow.pause` / `workflow.resume`**: Điều khiển trạng thái thực thi của workflow.

### Scanning (`scanning.*`)
- **`scanning.scanWorkbook`**: Đọc file Excel để lấy danh sách sheet và cột.
- **`scanning.scanPresentation`**: Đọc file PowerPoint để lấy danh sách slide và khung hình.

### Settings (`settings.*`)
- **`settings.get`**: Lấy cấu hình hiện tại của ứng dụng.
- **`settings.update`**: Lưu cấu hình mới vào đĩa.
- **`settings.resetToDefaults`**: Khôi phục cài đặt về trạng thái ban đầu.

---

## 2. Giao thức Giao tiếp

- **Mã hóa**: UTF-8.
- **Định dạng**: New-Line Delimited JSON (NDJSON). Mỗi request/response phải nằm trên một dòng duy nhất.
- **Tuần tự hóa**: Tuân thủ quy tắc đặt tên thuộc tính CamelCase.

---

## 3. Ví dụ Thông báo Tiến độ

```json
{
  "jsonrpc": "2.0",
  "method": "workflow/progress",
  "params": {
    "workflowInstanceId": "...",
    "event": "StepCompleted",
    "stepName": "DownloadImage",
    "status": "Running",
    "timestamp": "2026-05-05T..."
  }
}
```