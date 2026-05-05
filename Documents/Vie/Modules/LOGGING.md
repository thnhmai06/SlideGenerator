# Module: Ghi log

## The Hook (Q&A)

**Q: Làm sao theo dõi lỗi trong một sidecar không giao diện?**  
Chúng tôi sử dụng **Serilog** với nhiều đầu ra (sinks). Log được ghi đồng thời vào các file cục bộ để debug chuyên sâu và vào một **SQLite/Workflow Database** cho theo dõi có cấu trúc.

**Q: Frontend có thể xem được log không?**  
Các lỗi và sự kiện quan trọng được đẩy qua thông báo JSON-RPC `workflow/progress` và cũng được lưu lại trong database, frontend có thể truy vấn để xem lịch sử.

---

## 1. Chiến lược ghi log

- **File Sink**: Các file log cuốn chiếu được lưu trong thư mục dữ liệu ứng dụng cục bộ.
- **Workflow Sink**: Một sink Serilog tùy chỉnh (`WorkflowDatabaseSink`) ghi lại kết quả sinh slide và lỗi vào database để lưu trữ qua các phiên làm việc.

---

## 2. Dữ liệu có cấu trúc

Log bao gồm các dữ liệu giàu ngữ cảnh như `WorkflowInstanceId`, `StepName`, và chi tiết `Exception` giúp việc khắc phục các lỗi sinh slide cụ thể trở nên dễ dàng.