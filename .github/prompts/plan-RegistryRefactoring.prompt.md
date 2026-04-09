## Kế hoạch 02: Registry theo cơ chế giữ instance, rồi chuyển sang abstract class quản lý chung

### Mục tiêu
1. Khi vẫn còn instance đang cần dùng thì giữ nguyên object trên registry, không giải phóng sớm.
2. Chỉ giải phóng resource khi không còn bất kỳ instance nào cần nữa để tối ưu bộ nhớ.
3. Chuyển registry từ interface sang abstract class để gom toàn bộ logic quản lý chung vào một nơi.
4. Các lớp ở Infrastructure chỉ triển khai cách mở và đóng instance cụ thể.
5. Luôn bổ sung XML documentation bằng tiếng Anh cho mọi public API mới hoặc thay đổi hành vi.

### Cách tiếp cận
1. Thiết kế registry theo hướng reference count hoặc lease để theo dõi số consumer đang giữ resource.
2. Mỗi lần mở lại cùng một key thì tăng số lượng tham chiếu thay vì tạo instance mới.
3. Mỗi lần release thì giảm reference count.
4. Chỉ khi reference count về 0 mới dispose và remove khỏi registry.
5. Đổi abstraction trung tâm ở Application thành abstract base class.
6. Đưa các logic dùng chung vào base class:
- Chuẩn hóa key.
- Caching và get-or-add thread-safe.
- TryGet.
- Close và dispose lifecycle.
7. Cho các lớp Infrastructure kế thừa base class và chỉ override các hook chuyên biệt:
- OpenResource.
- CloseResource.
8. Giữ tương thích với các call-site hiện có trong Application và Infrastructure trong quá trình migration.
9. Nếu cần tối ưu thêm bộ nhớ, chỉ áp dụng idle eviction cho các entry đã về trạng thái không active.

### Tệp cần tác động
1. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Application/Common/IRegistry.cs
2. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Infrastructure/Sheet/Services/XlWorkbookRegistry.cs
3. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Infrastructure/Settings/Services/TextFileRegistry.cs
4. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Application/Settings/Services/SettingManager.cs
5. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Application/Tasks/Scanning/ScanningService.cs

### Xác minh
1. Kiểm thử cùng một key được mở nhiều lần thì registry giữ nguyên instance và tăng ref count.
2. Kiểm thử chỉ khi ref count về 0 thì resource mới được dispose.
3. Kiểm thử các call-site load/save và scanning không bị giải phóng sớm.
4. Kiểm thử concurrency để bảo đảm không double-dispose và không rò rỉ instance.
5. Kiểm thử build toàn bộ solution sau migration abstraction.
6. Chạy unit test cho base class và 3 implementation.
