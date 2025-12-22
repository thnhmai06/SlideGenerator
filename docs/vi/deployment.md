# Triển khai

## Mục lục

1. [Tổng quan](#tổng-quan)
2. [Cấu hình](#cấu-hình)
3. [Lưu ý](#lưu-ý)

## Tổng quan

Backend là ứng dụng ASP.NET Core chạy từ `SlideGenerator.Presentation`.

## Cấu hình

Trước khi deploy, cấu hình:

- Host/port server
- Đường dẫn database SQLite của Hangfire
- Số worker của Hangfire (tối đa job song song)

Xem: [Configuration](../en/configuration.md)

## Lưu ý

- Đảm bảo thư mục output tồn tại và có quyền ghi.
- Đảm bảo thư mục chứa database Hangfire có quyền ghi.
