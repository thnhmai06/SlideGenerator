# Hệ thống job

English version: [English](../en/job-system.md)

## Khái niệm

- Loai job:
  - Group job: một workbook + một template + thư mục output.
  - Sheet job: một sheet xuất ra một file.
- Mô hình nội bộ:
  - `JobGroup` (gốc)
  - `JobSheet` (lá)
- Trang thai job:
  - Pending → Processing → Done
  - Paused, Cancelled, Error

## Collection

- Active: lưu Pending/Processing/Paused bằng ConcurrentDictionary.
- Completed: lưu Done/Failed/Cancelled.
- JobManager truy vấn cả hai.

## Vòng đời

- Tạo: `JobCreate` → `IActiveJobCollection.CreateGroup`.
- Start: auto-start mặc định; có thể Pause/Resume.
- Dieu khien: Pause/Resume/Cancel (Stop duoc hieu la Cancel), Remove xoa state backend.
- Hoàn tất: khi mọi sheet xong, group chuyển sang Completed.

## Song song

- `job.maxConcurrentJobs` giới hạn số sheet chạy đồng thời.
- Resume ưu tiên slot trống; còn lại giữ Paused/Pending.

## Lưu state và khôi phục

- State lưu bằng Hangfire SQLite (`HangfireJobStateStore`).
- Lưu path, status, progress, text/image configs.
- Khi khởi động lại: Pending/Processing bị chuyển về Paused.
- Payload JSON không lưu; hệ thống dựng lại từ state khi cần.

Tiếp theo: [SignalR API](signalr.md)

