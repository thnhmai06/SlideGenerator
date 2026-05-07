# Module: Điều phối (Đồng thời)

## The Hook (Q&A)

**Q: Tại sao chúng ta cần một bộ khóa (Locker) riêng?**  
Các file Microsoft Office (Excel, PowerPoint) và việc tải dữ liệu mạng rất nhạy cảm với việc truy cập đồng thời. Mở cùng một file hai lần hoặc gửi quá nhiều request cùng lúc tới server có thể dẫn đến `IOException` hoặc bị chặn IP. `GateLocker` tập trung quyền kiểm soát truy cập để đảm bảo sự ổn định của hệ thống.

**Q: "Gate" là gì?**  
Một Gate là một biến đếm (semaphore) logic với sức chứa nhất định. Ví dụ, gate `Excel` có thể có sức chứa là 1 (truy cập tuần tự nghiêm ngặt), trong khi gate `Network` có thể cho phép 4 lượt tải đồng thời.

---

## 1. Các loại Gate

- **`Excel`**: Bảo vệ file `.xlsx`. Ngăn lỗi "File in use".
- **`PowerPoint`**: Bảo vệ file `.pptx` trong quá trình nhân bản và lắp ráp.
- **`Network`**: Giới hạn lượt tải đồng thời để tránh bị bóp băng thông.
- **`CPU`**: Giới hạn các tác vụ xử lý ảnh nặng để hệ thống luôn phản hồi mượt mà.

---

## 2. Cách sử dụng

Các Service bao bọc logic của mình trong một lời gọi `GateLocker.LockAsync`. Điều này đảm bảo semaphore luôn được giải phóng (thông qua `IDisposable`) ngay cả khi xảy ra ngoại lệ.

```csharp
using (await gateLocker.LockAsync(GateType.Excel, ct))
{
    // Thực hiện thao tác file an toàn tại đây
}
```

---

## 3. Lợi ích

- **Hiệu năng có thể dự đoán**: Không bị nghẽn CPU do hàng trăm thread cùng lúc cắt ảnh.
- **Không treo file**: Quản lý tập trung đảm bảo các file luôn được đóng đúng cách.
- **Hàng đợi văn minh**: Các bước tự động đợi đến lượt thay vì báo lỗi ngay lập tức.