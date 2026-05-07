# Module: Thao tác Office

## The Hook (Q&A)

**Q: Làm sao hệ thống xử lý PPTX/XLSX chất lượng cao mà không cần cài đặt Office?**  
Chúng tôi sử dụng thư viện **Syncfusion File Formats**. Điều này cho phép tạo file phía server tương thích 100% với Microsoft PowerPoint và Excel mà không cần phụ thuộc vào COM Interop nặng nề và thiếu ổn định.

**Q: Văn bản và hình ảnh thực sự được thay thế như thế nào?**  
- **Văn bản**: `TextComposer` quét các thẻ `{{Mustache}}` trong các khung hình và thay thế chúng bằng dữ liệu từ Excel.
- **Hình ảnh**: `ImageComposer` nhắm vào các khung hình ảnh cụ thể, xóa nội dung cũ và áp dụng `BlipFill` mới với hình ảnh đã qua xử lý.

---

## 1. Các bộ soạn thảo cốt lõi

- **TextComposer**: Xử lý thay thế văn bản phức tạp bao gồm văn bản nhiều dòng và giữ nguyên định dạng font gốc.
- **ImageComposer**: Đảm bảo hình ảnh được chèn chính xác vào các khung PowerPoint trong khi vẫn giữ đúng tỷ lệ khung hình (đã được xử lý trước khi chèn bởi module Image).

---

## 2. Các lớp trừu tượng

- **SfWorkbook / SfPresentation**: Các lớp bọc (wrapper) quản lý vòng đời của các instance Syncfusion và cung cấp API gọn gàng hơn cho các bước của pipeline.