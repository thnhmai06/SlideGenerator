# Hướng dẫn sử dụng

English version: [English](../en/usage.md)

## Điều kiện

- Backend đang chạy (Electron có thể tự start).
- Template: `.pptx` hoặc `.potx`.
- Spreadsheet: `.xlsx` hoặc `.xlsm`.

## Kết nối backend

1. Mở **Settings**.
2. Kiểm tra host/port (mặc định local).
3. Lưu thay đổi và restart backend nếu được yêu cầu.

## Tạo task

1. Chọn file template và file spreadsheet.
2. Chờ load shapes/placeholders và headers.
3. Thêm mapping text và image.
4. Chọn output path.
5. Bấm **Create Task**.

Ghi chú:

- Group task = một workbook + một template + thư mục output.
- Sheet task = một sheet → một file output.

## Theo dõi xử lý

Mở **Process** để:

- Pause/Resume group hoặc sheet.
- Cancel task.
- Xem progress và log theo dòng.
- Tiến độ nhóm hiển thị slide hoàn thành/tổng slide (job hoàn thành/tổng job) và % theo số slide.

## Kết quả

Mở **Result** để:

- Xem nhóm đã hoàn thành/lỗi/hủy.
- Mở thư mục hoặc file output.
- Clear kết quả (xóa state phía backend).
- Remove group/sheet cũng xóa state backend.

## Export/Import cấu hình

- **Create Task** hỗ trợ export/import JSON.
- Mỗi group có nút export nhanh.
