# Cấu hình

English version: [English](../en/configuration.md)

## Vị trí file cấu hình

- Backend đọc `backend.config.yaml` tại thư mục làm việc.
- Nếu chưa có, hệ thống sẽ tạo file mặc định khi chạy lần đầu.
- File mẫu: `backend.config.sample.yaml`.

## Các thiết lập chính

- `server`: host, port, debug.
- `job`: `maxConcurrentJobs` giới hạn số sheet chạy song song.
- `image`: độ tin cậy nhận diện khuôn mặt, kích thước tối đa (mặc định 1280, 0 = không giới hạn) và padding saliency.
- `download`: giới hạn băng thông và retry (khi tải ảnh).

## Hành vi runtime

- Hangfire lưu trạng thái vào SQLite (`jobs.db` mặc định).
- Số worker = `job.maxConcurrentJobs`.

## Quy tắc an toàn

- Không cho phép cập nhật config khi còn group ở trạng thái Pending hoặc Running.
- Task đang Paused không chặn cập nhật.

Tiếp theo: [Hệ thống job](job-system.md)
