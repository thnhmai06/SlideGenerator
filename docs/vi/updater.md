# Trình cập nhật (Updater)

[English](../en/updater.md)

Ứng dụng sử dụng `electron-updater` để cung cấp khả năng cập nhật tự động cho các bản dựng Windows (không bao gồm bản portable).

## Tính năng

- **Kiểm tra tự động**: Tự động kiểm tra bản cập nhật khi khởi động ứng dụng.
- **Kiểm tra thủ công**: Người dùng có thể kích hoạt kiểm tra từ màn hình Giới thiệu (About).
- **Tải xuống vi sai**: Chỉ tải xuống những phần thay đổi (hành vi mặc định của `electron-updater`).
- **Bảo vệ an toàn**: Ngăn chặn việc cài đặt nếu đang có các tiến trình tạo slide đang chạy.
- **Phát hiện bản Portable**: Tự động vô hiệu hóa tính năng cập nhật khi chạy từ tệp thực thi portable.

## Kiến trúc

### Tiến trình chính (Main Process - `electron/main/updater.ts`)

- Quản lý thực thể `autoUpdater`.
- Xử lý các cuộc gọi IPC để kiểm tra, tải xuống và cài đặt bản cập nhật.
- Phát tín hiệu trạng thái tới tất cả các cửa sổ renderer thông qua `updater:status`.
- Lưu giữ trạng thái "đã tải xuống" để xử lý khi ứng dụng khởi động lại.

### Preload (`electron/preload/api.ts`)

- Cung cấp các phương thức đã được định nghĩa kiểu cho renderer thông qua `window.electronAPI`:
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
5. **Cài đặt**: Người dùng nhấn cài đặt. Ứng dụng gọi `quitAndInstall()`.
   - _Lưu ý_: Giao diện sẽ vô hiệu hóa nút Cài đặt nếu `hasActiveJobs` là true.

## Cấu hình

Cấu hình của trình cập nhật được đọc từ `package.json` trong mục `build.publish`.

```json
"build": {
  "publish": {
    "provider": "github",
    "owner": "your-username",
    "repo": "your-repo"
  }
}
```

## Kiểm thử trong môi trường phát triển

Trình cập nhật được cấu hình để cho phép kiểm thử trong chế độ phát triển:

- `autoUpdater.forceDevUpdateConfig = true` được thiết lập khi `app.isPackaged` là false.
- Có thể cần tệp `dev-app-update.yml` ở thư mục gốc để kiểm thử cục bộ.
