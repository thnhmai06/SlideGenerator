# Module: Luồng xử lý (Pipelines)

## The Hook (Q&A)

**Q: Quy trình sinh slide được cấu trúc như thế nào?**  
`GeneratingWorkflow` được chia thành ba giai đoạn logic: **Xác thực (Phase A)**, **Chuẩn bị (Phase B)**, và **Lắp ráp (Phase C)**. Mỗi giai đoạn đảm bảo hệ thống chỉ tiếp tục với dữ liệu đã được xác minh và xử lý trước, giảm thiểu lỗi runtime trong quá trình sinh PowerPoint cuối cùng.

**Q: Nguyên tắc "Tính toàn vẹn thông minh" là gì?**  
Các bước như `DownloadImage` và `EditImage` sẽ kiểm tra xem thành phẩm cuối cùng đã tồn tại trong thư mục tạm chưa trước khi thực hiện. Điều này ngăn chặn các cuộc gọi mạng dư thừa và xử lý ảnh tốn CPU nếu workflow được khởi động lại hoặc nếu nhiều slide dùng chung một tài nguyên.

---

## 1. Các giai đoạn Workflow

### Giai đoạn A: Xác thực & Tạo khuôn
- **ValidateRequest**: Kiểm tra sheet Excel, slide và khung ảnh có tồn tại không. Các yêu cầu không hợp lệ sẽ bị loại bỏ.
- **CreateTemplate**: Copy template gốc và chuẩn bị "khuôn đúc" (file PPTX chỉ có 1 slide duy nhất).

### Giai đoạn B: Chuẩn bị tài nguyên
- **ExtractData**: Phân tích các dòng Excel thành các tác vụ sinh slide riêng biệt.
- **DownloadImage**: Tải ảnh thô từ Cloud/Web.
- **EditImage**: Cắt và đổi kích thước ảnh để vừa khít với kích thước của khung đích.

### Giai đoạn C: Lắp ráp & Hoàn thiện
- **ReplaceSlideData**: Nhân bản khuôn, điền chữ (mã Mustache) và chèn ảnh.
- **CloseAllHandles**: Giải phóng file và xóa slide khuôn ban đầu.

---

## 2. Cấu trúc thư mục (Tạm)

Ảnh được lưu trữ có hệ thống để hỗ trợ tính toàn vẹn:
- `Temp/{Workbook}/{Sheet}/{Column}/Download/`: File thô.
- `Temp/{Workbook}/{Sheet}/{Column}/Edit/`: File đã cắt/đổi kích thước sẵn sàng để chèn.