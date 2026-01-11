# Hướng dẫn sử dụng

[English](../en/usage.md)

## Điều kiện

- Backend đang chạy (Electron có thể tự khởi động).
- Template: `.pptx` hoặc `.potx`.
- Spreadsheet: `.xlsx` hoặc `.xlsm`.

## Kết nối backend

1. Mở **Settings**.
2. Kiểm tra host/port (mặc định là local).
3. Lưu và restart backend nếu được yêu cầu.

## Tạo task (group job)

1. Chọn template PowerPoint.
2. Chọn file Excel và chờ tải cột/sheet.
3. Thiết lập text/image replacements.
4. Chọn sheet cần xử lý (tuỳ chọn).
5. Chọn thư mục output.
6. Nhấn **Create Task**.

Ghi chú:

- Group job đại diện một template + một workbook + một thư mục output.
- Sheet job là từng sheet nằm trong group.
- Progress và thống kê dựa trên số row/slide, không dựa trên số job.

## Xử lý

Trong **Processing** bạn có thể:

- Pause/Resume group hoặc sheet.
- Cancel job.
- Xem log theo từng row.

## Kết quả

Trong **Results** bạn có thể:

- Xem nhóm hoàn thành/lỗi/huỷ.
- Mở thư mục hoặc file output.
- Xoá group/sheet (đồng thời xoá state trên backend).

## Export/import config

- **Create Task** hỗ trợ export/import JSON.
- Mỗi group có chức năng export config nhanh.
