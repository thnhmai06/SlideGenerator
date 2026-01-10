# Kiến trúc

English version: [English](../en/architecture.md)

## Tổng quan

Backend theo Clean Architecture và chia theo feature. SignalR cung cấp API tối giản cho task, còn xử lý job chạy nền và được lưu trạng thái để khôi phục khi crash.

## Các lớp chính

- Presentation: host ASP.NET Core và các SignalR hub (Task/Sheet/Config).
- Application: contract, DTO, và điều phối feature.
- Domain: entity, trạng thái, và quy tắc nghiệp vụ cốt lõi.
- Infrastructure: Hangfire + SQLite, IO, logging, và xử lý nền.

## Thành phần runtime

- TaskHub: nhận request và điều khiển task.
- JobManager: quản lý Active và Completed collections.
- ActiveJobCollection: lưu task đang chạy bằng ConcurrentDictionary.
- JobExecutor: xử lý từng dòng và lưu checkpoint.
- HangfireJobStateStore: lưu state vào SQLite.
- JobNotifier: gửi notification theo group/sheet.

## Luồng dữ liệu

1. Client gửi JSON vào TaskHub (`ProcessRequest`).
2. TaskHub tạo group/sheet task qua JobManager.Active.
3. ActiveJobCollection lưu state và enqueue Hangfire job (nếu auto-start).
4. JobExecutor xử lý, cập nhật state, gửi notification.
5. Khi hoàn tất, group được chuyển sang Completed.

Tiếp theo: [SignalR API](signalr.md)
