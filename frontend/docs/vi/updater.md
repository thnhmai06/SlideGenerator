# Trình cập nhật (Updater)

[English](../en/updater.md)

Trình cập nhật của ứng dụng hiện dùng updater plugin của Tauri v2.

## Tính năng

- **Kiểm tra tự động**: Tự động kiểm tra bản cập nhật khi khởi động ứng dụng.
- **Kiểm tra thủ công**: Người dùng có thể kích hoạt kiểm tra từ màn hình Giới thiệu (About).
- **Tải xuống vi sai**: Được quản lý theo cấu hình artifact/signature của updater Tauri.
- **Bảo vệ an toàn**: Ngăn chặn việc cài đặt nếu đang có các tiến trình tạo slide đang chạy.
- **Phát hiện bản Portable**: Tự động vô hiệu hóa tính năng cập nhật khi chạy từ tệp thực thi portable.

## Kiến trúc

### Desktop Host (`src-tauri/src/main.rs` và cấu hình updater plugin)

- Dùng vòng đời updater plugin của Tauri.
- Cung cấp luồng cập nhật cho frontend thông qua adapter desktop API.

### Frontend Bridge (`desktopApi` adapter)

- Cung cấp các phương thức updater định kiểu cho renderer thông qua adapter Tauri:
  - `checkForUpdates()`
  - `downloadUpdate()`
  - `installUpdate()`
  - `onUpdateStatus(callback)`
  - `isPortable()`

### React Context (`src/shared/contexts/UpdaterContext.tsx`)

- Cung cấp hook `useUpdater()`.
- Đồng bộ hóa trạng thái cục bộ với các sự kiện IPC.
- Theo dõi `hasActiveJobs` để kiểm soát quá trình cài đặt.

## Quy trình cập nhật

1. **Kiểm tra**: Ứng dụng gọi `checkForUpdates`. Trạng thái chuyển sang `checking`.
2. **Có bản mới**: Nếu tìm thấy phiên bản mới hơn, trạng thái trở thành `available`.
3. **Tải xuống**: Người dùng nhấn tải xuống. Trạng thái trở thành `downloading` kèm theo phần trăm tiến độ.
4. **Sẵn sàng**: Sau khi tải xong, trạng thái trở thành `downloaded`.
5. **Cài đặt**: Người dùng nhấn cài đặt. Ứng dụng áp dụng cập nhật và relaunch.
   - _Lưu ý_: Giao diện sẽ vô hiệu hóa nút Cài đặt nếu `hasActiveJobs` là true.

## Cấu hình

Cấu hình updater được đọc từ `src-tauri/tauri.conf.json` trong mục `plugins.updater`.

```json
"plugins": {
  "updater": {
    "active": true,
    "pubkey": "PUBLIC_KEY_CONTENT",
    "endpoints": ["https://example.com/latest.json"]
  }
}
```

## Kiểm thử trong môi trường phát triển

Trình cập nhật được cấu hình để cho phép kiểm thử trong chế độ phát triển:

- Hành vi updater ở môi trường dev tuân theo cấu hình plugin updater của Tauri.
