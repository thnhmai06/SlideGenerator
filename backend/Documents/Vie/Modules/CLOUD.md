# Module: Phân giải Cloud

## The Hook (Q&A)

**Q: Tại sao chúng ta không tải trực tiếp từ link luôn?**  
Hầu hết các link chia sẻ cloud (Google Drive, OneDrive) đều trỏ đến trang "Xem trước", không phải file thô. `MultiCloudResolver` nhận diện nhà cung cấp và chuyển đổi link người dùng cung cấp thành URL dòng nhị phân trực tiếp bằng các pattern đặc biệt và thủ thuật API.

**Q: Những nhà cung cấp nào được hỗ trợ?**  
Hiện tại, hệ thống hỗ trợ **Google Drive**, **Google Photos**, **OneDrive**, và **SharePoint**. Nếu một link không được nhận diện, nó sẽ được xử lý như một URL trực tiếp tiêu chuẩn.

---

## 1. Các bộ phân giải hỗ trợ

- **Google Drive**: Chuyển đổi `/file/d/{id}/view` thành endpoint tải trực tiếp.
- **Google Photos**: Phân tích link album hoặc ảnh trực tiếp thành URL nguồn chất lượng cao.
- **OneDrive/SharePoint**: Xử lý link chia sẻ cá nhân và doanh nghiệp để trích xuất nội dung ID bên dưới.

---

## 2. Tích hợp

`MultiCloudResolver` hoạt động như một facade. Nó được gọi trong bước `DownloadImage` của pipeline.

```csharp
// Ví dụ sử dụng
var directUri = await multiCloudResolver.ResolveUriAsync(userUri, httpClient);
```