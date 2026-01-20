# Build & Đóng gói

[🇺🇸 English Version](../en/build-and-packaging.md)

Hướng dẫn này bao gồm cách build ứng dụng SlideGenerator để phân phối sản phẩm (production).

## Tổng quan Quy trình Build

Quy trình build bao gồm hai giai đoạn chính:
1.  **Backend Build:** Biên dịch ứng dụng .NET thành file thực thi khép kín (self-contained executable).
2.  **Frontend Build:** Đóng gói ứng dụng React và Electron, bao gồm cả binary backend.

## 1. Build với Task (Khuyên dùng)

Cách dễ nhất để build dự án là sử dụng [Task](https://taskfile.dev/).

**Build Toàn bộ:**
```bash
task build
```

**Build cho Linux:**
```bash
task build RUNTIME=linux-x64
```

Lệnh này tự động hóa quy trình build backend, copy vào resource frontend, và đóng gói ứng dụng Electron.

## 2. Quy trình Build Thủ công

Nếu bạn muốn chạy lệnh thủ công mà không dùng Task:

### Bước 1: Build Backend

Backend phải được build trước để có thể copy vào thư mục resource của frontend.

Khi backend đã sẵn sàng, bạn có thể build ứng dụng Electron.

**Lệnh:**
```bash
# Chạy từ thư mục frontend/
npm run build:full
```

Script này thực hiện các hành động sau:
1.  `build:backend`: Copy các file backend đã publish vào `frontend/backend`.
2.  `build`: Chạy Vite để đóng gói ứng dụng React.
3.  `electron-builder`: Đóng gói mọi thứ thành bộ cài đặt (NSIS cho Windows, AppImage cho Linux).

## Phân phối

### Artifact đầu ra
Các bộ cài đặt cuối cùng nằm tại `frontend/release/`.

- **Windows:** `SlideGenerator Setup <version>.exe`
- **Linux:** `SlideGenerator-<version>.AppImage`

### Signing (Tùy chọn)
Để ký ứng dụng (bắt buộc cho auto-update và tránh cảnh báo SmartScreen):
1.  Thiết lập biến môi trường `CSC_LINK` và `CSC_KEY_PASSWORD`.
2.  Tham khảo [tài liệu electron-builder](https://www.electron.build/code-signing) để biết chi tiết.

## Khắc phục sự cố

- **Thiếu Backend:** Nếu ứng dụng chạy nhưng không làm gì cả, hãy đảm bảo binary backend đã được copy chính xác vào `resources/backend` bên trong ứng dụng đã cài đặt.
- **Lỗi Runtime:** Kiểm tra xem máy đích có đáp ứng yêu cầu hệ điều hành không (mặc dù .NET runtime là khép kín, một số dependency hệ điều hành có thể cần thiết trên Linux).
