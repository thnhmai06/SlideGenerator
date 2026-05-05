# SlideGenerator Backend

## The Hook (Q&A)

**Q: Dự án giải quyết vấn đề gì?**  
SlideGenerator tự động hóa việc sản xuất hàng loạt các bài thuyết trình PowerPoint bằng cách ánh xạ dữ liệu từ Excel trực tiếp vào các mẫu (template) PowerPoint. Dự án loại bỏ hoàn toàn thao tác copy/paste văn bản và cắt cúp ảnh thủ công, biến hàng giờ làm việc thành vài giây.

**Q: Tại sao kiến trúc tinh gọn này lại tối ưu cho dự án?**  
Chúng tôi sử dụng kiến trúc .NET sidecar giao tiếp qua JSON-RPC 2.0 thông qua `stdin/stdout`. Thay vì "làm quá" (over-engineering) với các lớp Repository phức tạp hay REST API nặng nề, hệ thống trực tiếp ánh xạ IPC request vào các pipeline của `WorkflowCore`. Sự thực dụng này đảm bảo hiệu năng cao, tốn ít bộ nhớ và dễ dàng tích hợp với các frontend framework hiện đại (như Tauri, Electron).

**Q: Luồng xử lý (Data Flow) hoạt động ra sao?**  
Frontend gửi một yêu cầu JSON-RPC thông qua standard input. Bộ xử lý IPC giải mã và kích hoạt một pipeline trong `WorkflowCore`. Pipeline này lần lượt xác thực file, tải/chỉnh sửa ảnh, và lắp ráp slide. Tiến độ (progress) được trả về liên tục qua standard output.

---

## 1. Tổng quan dự án

SlideGenerator là một ứng dụng console .NET được tối ưu hóa cao độ, hoạt động dưới dạng IPC sidecar. Nó nhận lệnh để đọc file `.xlsx`, tải tài nguyên, và sinh ra các file `.pptx` một cách hiệu quả.

### Tính năng cốt lõi:
- **Giao tiếp IPC trực tiếp**: Sử dụng StreamJsonRpc cho tốc độ nhắn tin siêu tốc và ổn định.
- **Điều phối luồng**: Được trang bị WorkflowCore để thực thi từng bước một cách bền bỉ.
- **Tính toàn vẹn thông minh**: "Làm rồi thì không làm lại." Tái sử dụng ảnh đã tải và cắt cúp hiệu quả.
- **Kiểm soát luồng đồng thời**: Sử dụng GateLocker mạnh mẽ để tránh tình trạng hệ điều hành khóa file và nghẽn CPU.

---

## 2. Cấu trúc Solution

Solution được chia thành các module với phạm vi hẹp, mang tính thực dụng nhằm ưu tiên việc thực thi trực tiếp thay vì trừu tượng hóa quá sâu.

- **`SlideGenerator.Ipc`**: Điểm vào của ứng dụng. Xử lý `stdin/stdout` và điều phối JSON-RPC.
- **`SlideGenerator.Pipelines`**: Động cơ cốt lõi. Định nghĩa các giai đoạn và bước xử lý bằng WorkflowCore.
- **`SlideGenerator.Coordinator`**: Quản lý đồng thời và khóa file (`GateLocker`).
- **`SlideGenerator.Documents`**: Bọc các thư viện bên ngoài (như Syncfusion) để thao tác với Excel và PowerPoint.
- **`SlideGenerator.Images`**: Xử lý cắt ảnh, thay đổi kích thước và nhận diện vùng trọng tâm (ROI).
- **`SlideGenerator.Cloud`**: Phân giải link chia sẻ (Google Drive, OneDrive) thành link tải trực tiếp.

---

## 3. Bắt đầu

Để phát triển trên máy cá nhân, chỉ cần phục hồi các thư viện phụ thuộc và build solution bằng .NET 8 CLI tiêu chuẩn.

```bash
dotnet restore
dotnet build SlideGenerator.sln
```

Xem `CONTRIBUTING.md` để biết các tiêu chuẩn lập trình và `ARCHITECTURE.md` để xem các sơ đồ hệ thống.