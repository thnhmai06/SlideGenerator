# Module: Hình ảnh & Vùng trọng tâm

## The Hook (Q&A)

**Q: Làm sao để tránh việc mặt bị cắt mất khi crop ảnh?**  
`RoiResolver` sử dụng **AI nhận diện khuôn mặt** (qua YuNet/OpenCV). Trước khi cắt, hệ thống xác định tọa độ khuôn mặt và điều chỉnh Vùng trọng tâm (ROI) để đảm bảo con người luôn là tâm điểm của slide.

**Q: Chuyện gì xảy ra nếu không tìm thấy khuôn mặt nào?**  
Hệ thống sẽ quay về các thuật toán hình học tiêu chuẩn như **Center Crop** (Cắt giữa) hoặc **Rule of Thirds** (Quy tắc 1/3), tùy thuộc vào cấu hình.

---

## 1. Các thuật toán ROI

- **Center**: Đơn giản và nhanh chóng. Tập trung vào giữa ảnh.
- **Rule of Thirds**: Đặt các điểm nhấn dọc theo các đường lưới để có giao diện "chuyên nghiệp" hơn.
- **Face-Aware**: Chế độ ưu tiên. Căn giữa phần cắt xung quanh các khuôn mặt được phát hiện.

---

## 2. Động cơ xử lý

Chúng tôi sử dụng **ImageMagick (Magick.NET)** để thao tác hình ảnh hiệu năng cao. Tất cả các thao tác (đổi kích thước, cắt, chuyển đổi định dạng) đều diễn ra trên bộ nhớ trước khi được lưu vào thư mục tạm.

---

## 3. Nhận diện khuôn mặt

Sử dụng model `YuNet.onnx` để suy luận nhẹ nhàng và nhanh chóng, đảm bảo sidecar vẫn chạy nhanh ngay cả trên các máy không có GPU rời.