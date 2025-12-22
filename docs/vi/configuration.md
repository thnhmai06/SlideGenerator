# Cấu hình

## Mục lục

1. [Nguồn cấu hình](#nguồn-cấu-hình)
2. [Hành vi runtime](#hành-vi-runtime)
3. [Quy tắc an toàn](#quy-tắc-an-toàn)
4. [Cấu hình ảnh](#cấu-hình-ảnh)

## Nguồn cấu hình

Cấu hình được nạp khi khởi động và lưu xuống đĩa.

Code liên quan:

- `SlideGenerator.Application/Configs/ConfigHolder.cs`
- `SlideGenerator.Infrastructure/Configs/ConfigLoader.cs`
- `SlideGenerator.Presentation/Hubs/ConfigHub.cs`

## Hành vi runtime

- Host/port và debug mode đọc từ config.
- Hangfire dùng SQLite tại `jobs.db` cạnh file chạy.
- Số worker theo `MaxConcurrentJobs`.
- Mặc định xử lý ảnh (padding face/saliency) đọc từ config.

## Quy tắc an toàn

Không cho đổi config khi job đang chạy hoặc pending:

- `ConfigHub` kiểm tra `GroupStatus.Pending` hoặc `GroupStatus.Running`.

Job ở trạng thái paused vẫn cho phép chỉnh.

Xem thêm: [Job system](../en/job-system.md)

## Cấu hình ảnh

`image` chia thành `face` và `saliency`:

- `face.confidence`: ngưỡng độ tin cậy (0-1).
- `face.padding_*`: tỉ lệ padding quanh khuôn mặt (0-1).
- `face.union_all`: gộp tất cả khuôn mặt thành một ROI.
- `saliency.padding_*`: tỉ lệ padding quanh vùng saliency (0-1).
