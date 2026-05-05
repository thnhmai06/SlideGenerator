# Module: Dịch vụ Tải về

## The Hook (Q&A)

**Q: Làm sao xử lý khi kết nối internet không ổn định?**  
`DownloadService` triển khai các **Chính sách Thử lại (Retry Policies)** và sử dụng gate `Network` từ Coordinator để tránh làm quá tải đường truyền. Nó phối hợp chặt chẽ với các Cloud Resolver để đảm bảo URL trực tiếp ổn định nhất được sử dụng.

---

## 1. Các tính năng chính

- **Tải về nguyên tử (Atomic)**: Các file được tải về một vị trí tạm thời trước để ngăn việc sử dụng các file bị hỏng hoặc tải chưa xong trong pipeline.
- **Dựa trên Stream**: Đẩy dữ liệu vào đĩa một cách hiệu quả để giữ mức sử dụng bộ nhớ thấp ngay cả với các file ảnh lớn.

---

## 2. Tích hợp

Được tích hợp trực tiếp vào bước `DownloadImage` của workflow sinh slide. Nó dựa vào một instance `HttpClient` dùng chung để tối ưu hóa việc quản lý kết nối (connection pooling).