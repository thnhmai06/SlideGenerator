# Hướng dẫn Đóng góp

## The Hook (Q&A)

**Q: Làm thế nào để build và test trên máy cá nhân?**  
Bạn cần .NET 8 SDK. Vì đây là ứng dụng IPC, để test bạn cần chạy một frontend giả lập đẩy dữ liệu vào `stdin`, hoặc dùng các bản bọc (wrappers) của unit/integration test. Lệnh `dotnet build` tiêu chuẩn hoạt động ngay lập tức.

**Q: Tiêu chuẩn lập trình của dự án này là gì?**  
Thực dụng, trực tiếp, và rõ ràng. Tránh các tầng trừu tượng chung chung trừ khi thực sự cần thiết. Giữ các bước (steps) trong workflow nhỏ và tập trung. Luôn dùng dependency injection và tôn trọng `GateLocker` đối với giới hạn tài nguyên.

---

## 1. Nguyên tắc Phát triển

1. **Không code "Phòng hờ":** Không xây dựng abstract factories hay interface trừ khi có ít nhất 2 bản triển khai thực tế.
2. **Lỗi nhanh & Ghi log mọi thứ:** Sử dụng Serilog. Nếu trạng thái hệ thống bị vi phạm, throw Exception ngay lập tức kèm theo bối cảnh.
3. **Tính toàn vẹn (Idempotency):** Bất kỳ bước pipeline nào cũng phải an toàn khi chạy lại. Không bao giờ mặc định trạng thái hoàn hảo. Luôn kiểm tra trước khi tạo/tải.
4. **Code Sạch:** Tuân thủ chuẩn C# (PascalCase cho Properties, camelCase cho biến cục bộ). Luôn dùng `sealed` cho class mặc định.

## 2. Tiêu chuẩn Commit

Sử dụng commit message mô tả rõ ràng, tập trung vào chữ *Tại sao* thay vì *Cái gì*. Vd: "Sửa lỗi race condition của GateLocker để ngăn tình trạng treo file Excel."