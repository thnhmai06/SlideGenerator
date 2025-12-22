# Hướng dẫn sử dụng

## Mục lục

1. [Yêu cầu](#yêu-cầu)
2. [Kết nối backend](#kết-nối-backend)
3. [Tạo task](#tạo-task)
4. [Theo dõi xử lý](#theo-dõi-xử-lý)
5. [Kết quả](#kết-quả)
6. [Xuất và nhập cấu hình](#xuất-và-nhập-cấu-hình)

## Yêu cầu

- Backend phải đang chạy (Electron có thể tự khởi chạy như subprocess).
- File template: `.pptx` hoặc `.potx`
- File dữ liệu: `.xlsx` hoặc `.xlsm`

## Kết nối backend

1. Mở **Cài đặt**.
2. Thiết lập URL backend nếu cần (mặc định là local).
3. Lưu cấu hình.

## Tạo task

1. Trong **Tạo task**, chọn file template và file dữ liệu.
2. Chờ tải headers và placeholders.
3. Thêm cấu hình thay thế text và ảnh:
   - Placeholder phải trùng placeholder đọc từ template.
   - Cột dữ liệu phải trùng header trong sheet.
4. Chọn thư mục output và nhấn **Tạo task**.

## Theo dõi xử lý

Mở **Xử lý** để:

- Pause/Resume group hoặc sheet.
- Dừng job (hủy và xóa khỏi backend, đồng thời xóa file output).
- Xem log theo từng dòng.

## Kết quả

Mở **Kết quả** để:

- Xem group đã hoàn thành/thất bại/đã hủy.
- Mở thư mục output hoặc file output.
- Clear dữ liệu kết quả (xóa khỏi backend).

## Xuất và nhập cấu hình

- **Tạo task** cho phép xuất/nhập file JSON.
- **Xử lý/Kết quả** có nút **Xuất cấu hình** dạng icon cho từng group.
  File JSON tương thích với thao tác nhập trong Create Task.
