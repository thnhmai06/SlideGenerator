# Tài liệu API & Workflow

## Điểm cuối IPC (IPC Endpoints)
Hệ thống hoạt động dưới dạng IPC Sidecar, cung cấp **9 phương thức JSON-RPC 2.0** có thể gọi từ Frontend (Tauri/Electron) qua `stdin`. Các phương thức này được đăng ký thông qua `AddLocalRpcMethod` trong `SlideGenerator.Ipc`.

| Tên Phương thức | Trình xử lý (Handler) | Chức năng (Mô tả) |
|---|---|---|
| `workflow.start` | `WorkflowHandler.StartAsync` | Bắt đầu quá trình tạo Slide. Yêu cầu tham số truyền vào là cấu hình/công việc (DTO parameter). |
| `workflow.cancel` | `WorkflowHandler.CancelAsync` | Hủy bỏ an toàn luồng công việc (workflow) đang chạy. |
| `workflow.pause` | `WorkflowHandler.PauseAsync` | Tạm dừng luồng công việc đang chạy. |
| `workflow.resume` | `WorkflowHandler.ResumeAsync` | Tiếp tục luồng công việc đã bị tạm dừng. |
| `scanning.scanWorkbook` | `ScanningHandler.ScanWorkbookAsync` | Quét tệp Excel, trả về cấu trúc (`WorkbookSummary`) và MetaData để Frontend hiển thị bản xem trước. |
| `scanning.scanPresentation`| `ScanningHandler.ScanPresentationAsync`| Quét tệp mẫu PowerPoint, trả về cấu trúc (`PresentationSummary`) và các biến (Mustache Variables). |
| `settings.get` | `SettingsHandler.GetAsync` | Lấy toàn bộ các cấu hình hệ thống hiện tại. Không yêu cầu tham số. |
| `settings.update` | `SettingsHandler.UpdateAsync` | Cập nhật cấu hình hệ thống mới. Yêu cầu tham số truyền vào là cấu hình cập nhật. |
| `settings.resetToDefaults` | `SettingsHandler.ResetToDefaultsAsync` | Khôi phục toàn bộ cấu hình hệ thống về trạng thái mặc định ban đầu. |

## Thông báo từ hệ thống (Notifications)
Frontend không chỉ gửi yêu cầu mà còn nhận luồng dữ liệu liên tục từ Backend qua `stdout` mà không cần gọi hàm (Push model):

- **`workflow/progress`**: Phương thức thông báo được Backend đẩy qua `WorkflowProgressObserver` mỗi khi có thay đổi trong tiến độ xử lý ảnh, tạo slide (vd: % hoàn thành, tên file đang xử lý).

## Cơ chế hoạt động của Workflow (Workflow System)
Quá trình tạo Slide được thực thi qua **WorkflowCore** và chia làm 3 Giai đoạn (Phases) theo tuần tự, nhưng nội bộ mỗi bước có thể chạy song song (Phase-Sequential, Item-Parallel):

1. **Giai đoạn A (Xác thực & Thiết lập):** Đọc metadata và chuẩn bị `GeneratingTask` state.
2. **Giai đoạn B (Chuẩn bị Tài nguyên):** Khởi tạo vòng lặp (sử dụng `.ForEach()` của WorkflowCore, tuyệt đối không dùng `foreach` C#). Tải hình ảnh (`DownloadImage`), chỉnh sửa hình ảnh (`EditImage`).
3. **Giai đoạn C (Lắp ráp & Dọn dẹp):** Thay thế dữ liệu chữ (`TextInstruction`) và hình (`ImageInstruction`) vào bản mẫu PowerPoint (`ReplaceSlideData`), sau đó đóng tất cả file handles.
