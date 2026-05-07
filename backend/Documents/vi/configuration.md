# Cấu hình

[🇺🇸 English Version](../en/configuration.md)

Backend được cấu hình thông qua một file YAML có tên `backend.config.yaml` nằm trong thư mục làm việc.

## File Cấu hình

Trong lần chạy đầu tiên, nếu file này bị thiếu, ứng dụng sẽ tự động sinh ra một file `backend.config.yaml` mặc định. Bạn cũng có thể tham khảo file `backend.config.sample.yaml`.

### Cấu trúc & Các cài đặt chính

```yaml
server:
  host: "localhost"
  port: 5000
  debug: false   # Bật log debug chi tiết

job:
  # Số lượng sheet job tối đa chạy song song trên tất cả các group.
  maxConcurrentJobs: 4 

image:
  # Ngưỡng tin cậy khi nhận diện khuôn mặt (0.0 - 1.0)
  faceConfidence: 0.7
  # Kích thước tối đa để resize ảnh (0 = không giới hạn)
  maxDimension: 1280
  # Phần đệm (padding) thêm vào vùng ROI được phát hiện
  saliencyPadding: 0.1

download:
  # Giới hạn băng thông mạng khi tải ảnh (0 = không giới hạn)
  maxBandwidth: 0
  retryCount: 3
```

## Hành vi Runtime

### Bền vững (Persistence)
- **Trạng thái Job:** Được lưu trong cơ sở dữ liệu SQLite (`jobs.db` mặc định). Điều này cho phép ứng dụng tiếp tục các tác vụ sau khi khởi động lại.
- **Worker Pool:** Số lượng luồng xử lý nền được tự động điều chỉnh dựa trên `job.maxConcurrentJobs`.

### Cơ chế An toàn

Để đảm bảo tính toàn vẹn dữ liệu, hệ thống áp dụng các quy tắc sau đối với việc thay đổi cấu hình:

1.  **Chặn cập nhật:** Bạn không thể thay đổi cấu hình khi có bất kỳ job group nào đang ở trạng thái `Pending` hoặc `Running`.
2.  **Cho phép cập nhật:** Cấu hình có thể được cập nhật an toàn khi tất cả các job đang `Paused` hoặc khi không có job nào đang hoạt động.

Tiếp theo: [Hệ thống Job](job-system.md)
