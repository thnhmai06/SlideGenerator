# Triển khai

English version: [English](../en/deployment.md)

## Tóm tắt

Backend là ứng dụng ASP.NET Core chạy từ `SlideGenerator.Presentation`.

## Các bước

1. Chuẩn bị `backend.config.yaml` (host, port, maxConcurrentJobs).
2. Đảm bảo quyền ghi cho:
   - vị trí file config,
   - file SQLite của Hangfire,
   - thư mục output.
3. Chạy server (local hoặc bản publish).

## Ghi chú

- Health check: `/health`.
- Hangfire dashboard: `/hangfire` (read-only).
- Thiết kế ưu tiên chạy local/offline.
