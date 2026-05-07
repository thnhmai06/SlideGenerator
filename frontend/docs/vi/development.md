# Hướng dẫn Phát triển

[🇺🇸 English Version](../en/development.md)

## Thiết lập Môi trường

### Yêu cầu tiên quyết
- **Node.js** (Khuyên dùng bản LTS)
- **npm** (đi kèm với Node.js)
- **.NET 10 SDK** (Cần thiết nếu bạn định chạy/debug backend cục bộ)

### Cài đặt
```bash
cd frontend
npm install
```

### Chạy trong môi trường Dev

Lệnh này khởi động Vite dev server và desktop host của Tauri.

```bash
npm run dev
```

**Lưu ý:** Runtime desktop đang được migration sang Tauri v2.
- **Tắt khởi chạy Backend:** `SLIDEGEN_DISABLE_BACKEND=1` (Hữu ích khi bạn đang chạy backend riêng trong Visual Studio).
- **Đường dẫn Backend tùy chỉnh:** `SLIDEGEN_BACKEND_PATH=/path/to/executable`.

## Cấu trúc Dự án

Chúng tôi tuân theo kiến trúc **Feature-First** (Ưu tiên tính năng).

```
src/
├── app/                  # App shell, Layouts, Providers
├── features/             # Feature modules
│   ├── create-task/      # Wizard tạo task
│   ├── process/          # Dashboard giám sát job
│   ├── results/          # Danh sách job hoàn thành
│   └── settings/         # Cấu hình ứng dụng
├── shared/               # Tiện ích chia sẻ
│   ├── components/       # UI components nguyên tử (Buttons, Inputs)
│   ├── contexts/         # React Contexts (JobContext, AppContext)
│   ├── hooks/            # Custom React Hooks
│   ├── services/         # API & RPC clients
│   └── styles/           # Global SCSS & Variables
└── assets/               # Tài nguyên tĩnh (Images, Fonts)
```

## Tiêu chuẩn Coding

### TypeScript
- **Strict Mode:** Đã bật. Không được dùng `any` trừ khi thực sự cần thiết (và phải có comment giải thích).
- **Interfaces:** Ưu tiên dùng `interface` hơn `type` cho các định nghĩa object.
- **Đặt tên:** PascalCase cho components/interfaces, camelCase cho functions/vars.

### React
- **Functional Components:** Sử dụng FC với Hooks.
- **Props:** Luôn định nghĩa interface Props có kiểu.
- **Hiệu năng:**
    - Sử dụng `React.memo` cho các item trong danh sách hoặc component nặng.
    - Sử dụng `useCallback` cho các event handler được truyền xuống component con.

### Styling
- **CSS Modules:** Sử dụng cho style riêng của component (`Component.module.scss`).
- **Global Styles:** Nằm trong `src/shared/styles/`. Sử dụng biến CSS cho theming.

## Testing

Chúng tôi sử dụng **Vitest** + **React Testing Library**.

### Chạy Test
```bash
npm test
```

### Viết Test
- **Unit Tests:** Tập trung vào các hàm tiện ích (utility functions) và hooks.
- **Component Tests:** Tập trung vào tương tác người dùng và khả năng truy cập (accessibility).
- **Mocking:** Sử dụng MSW (Mock Service Worker) cho các request mạng. Handlers nằm trong `test/mocks/handlers.ts`.

## Debugging

- **Renderer Process:** Sử dụng Chrome DevTools tiêu chuẩn (Ctrl+Shift+I).
- **Desktop Host (Rust):** Debug qua Rust tooling / VS Code Rust extension.
- **Backend:** Debug qua Visual Studio hoặc extension C# của VS Code.

Tiếp theo: [Build & Đóng gói](build-and-packaging.md)
