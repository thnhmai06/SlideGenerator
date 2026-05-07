# Module: Cài đặt & Cấu hình

## The Hook (Q&A)

**Q: Cài đặt được lưu ở đâu?**  
Cài đặt được lưu trữ dưới dạng file **YAML**. Lựa chọn này mang lại sự cân bằng giữa khả năng đọc của con người và khả năng phân tích của máy tính, giúp người dùng dễ dàng điều chỉnh cấu hình thủ công nếu cần.

**Q: Cấu hình được quản lý như thế nào khi ứng dụng chạy?**  
`SettingManager` đóng vai trò là trung tâm. Nó tải cài đặt khi khởi động, cung cấp quyền truy cập thread-safe vào các đối tượng cấu hình và xử lý việc lưu file an toàn vào đĩa.

---

## 1. Các loại cấu hình

- **JobConfig**: Định nghĩa đường dẫn đầu ra, pattern đặt tên file và các quy tắc sinh slide toàn cục.
- **DownloadConfig**: Quản lý giới hạn tải đồng thời và cài đặt timeout.
- **ImageConfig**: Điều khiển loại ROI mặc định và chất lượng xử lý ảnh.

---

## 2. Tuần tự hóa

Chúng tôi sử dụng **YamlDotNet** để tuần tự hóa. Điều này cho phép chúng tôi hỗ trợ các tính năng nâng cao như ghi chú và định dạng sạch đẹp trong các file cấu hình.

---

## 3. Tích hợp IPC

Cài đặt có thể được truy vấn và cập nhật theo thời gian thực thông qua các phương thức JSON-RPC `settings.*`, cho phép frontend cung cấp giao diện cấu hình phong phú.