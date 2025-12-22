# Kiến trúc

## Mục lục

1. [Tổng quan](#tổng-quan)
2. [Dự án và trách nhiệm](#dự-án-và-trách-nhiệm)
3. [Thành phần runtime chính](#thành-phần-runtime-chính)
4. [Luồng dữ liệu](#luồng-dữ-liệu)

## Tổng quan

Backend dùng kiến trúc phân lớp:

- `SlideGenerator.Presentation`: host ASP.NET Core, SignalR hubs.
- `SlideGenerator.Infrastructure`: hiện thực (Hangfire, IO, thông báo).
- `SlideGenerator.Application`: hợp đồng công khai (services, DTOs, requests/responses).
- `SlideGenerator.Domain`: thực thể cốt lõi và interface miền nghiệp vụ.

## Dự án và trách nhiệm

### `SlideGenerator.Presentation`

- Mở các endpoint SignalR.
- Xác thực và điều phối request vào.
- Trả phản hồi thành công/lỗi có kiểu.

Xem thêm: [SignalR API](../en/signalr.md)

### `SlideGenerator.Infrastructure`

- Thực thi job bằng Hangfire.
- Cung cấp job manager và các collection.
- Phát thông báo qua SignalR.

Xem thêm: [Job system](../en/job-system.md)

### `SlideGenerator.Application`

- Định nghĩa contract dùng chung cho presentation/infrastructure.
- Định nghĩa DTOs cho request/response/notification.

### `SlideGenerator.Domain`

- Mô hình job dạng composite: group là gốc, sheet là lá.
- Trạng thái, cách tính tiến độ, và bất biến của thực thể.

## Thành phần runtime chính

- SignalR hubs: xử lý traffic UI và đăng ký theo group/sheet.
- Hangfire server + HangfireSQLite: lập lịch chạy sheet và lưu trạng thái.
- Job manager: theo dõi active/completed và khôi phục job chưa xong.

## Luồng dữ liệu

1. Client gửi request tới `SlideHub`.
2. Hub dùng `IJobManager.Active` tạo group và start.
3. Hangfire xếp hàng một job cho mỗi sheet (ID ổn định).
4. `IJobExecutor` xử lý từng dòng, checkpoint pause/resume, lưu state.
5. `IJobNotifier` chỉ phát cập nhật tới subscriber đúng group/sheet.
6. Khi xong, group được chuyển từ Active sang Completed.
