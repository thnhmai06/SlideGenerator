# Tạo Slide Tốt Nghiệp - Electron App

Ứng dụng desktop được xây dựng bằng **Electron + React + TypeScript** để tự động tạo slide PowerPoint từ dữ liệu CSV.

## 🚀 Chuyển đổi từ PySide6

Dự án này là phiên bản Electron của ứng dụng PySide6 ban đầu, với các cải tiến:

- ✅ Cross-platform desktop app (Windows, macOS, Linux)
- ✅ Modern UI với React và TypeScript
- ✅ Hot reload trong development
- ✅ Dễ dàng triển khai và cập nhật

## 📋 Yêu cầu

- **Node.js** >= 18.x
- **npm** >= 9.x

## 🔧 Cài đặt

```bash
# Di chuyển vào thư mục ElectronApp
cd ElectronApp

# Cài đặt dependencies
npm install
```

## 🎯 Chạy ứng dụng

### Development mode
```bash
npm run electron:dev
```

Lệnh này sẽ:
1. Khởi động Vite dev server (React hot reload)
2. Mở Electron window với DevTools

### Build production
```bash
npm run electron:build
```

File build sẽ được tạo trong thư mục `release/`

## 📁 Cấu trúc dự án

```
ElectronApp/
├── electron/              # Electron main process
│   ├── main.ts           # Main process (window, IPC handlers)
│   └── preload.ts        # Preload script (IPC bridge)
├── src/                  # React app
│   ├── components/       # React components
│   │   ├── Sidebar.tsx
│   │   ├── InputMenu.tsx
│   │   ├── ProcessMenu.tsx
│   │   ├── ProgressBar.tsx
│   │   ├── LogWindow.tsx
│   │   ├── SettingMenu.tsx
│   │   ├── DownloadMenu.tsx
│   │   └── AboutMenu.tsx
│   ├── styles/          # CSS files
│   ├── App.tsx          # Main app component
│   ├── main.tsx         # React entry point
│   └── global.d.ts      # TypeScript declarations
├── index.html           # HTML template
├── package.json         # Dependencies & scripts
├── tsconfig.json        # TypeScript config
└── vite.config.ts       # Vite config
```

## 🎨 Tính năng chính

### 1. **Input Menu**
- Chọn file CSV input
- Chọn file PPTX template
- Chọn folder lưu output
- File dialogs native của OS

### 2. **Process Menu**
- Hiển thị multiple progress bars
- Theo dõi tiến trình real-time
- Xem log chi tiết cho từng task
- Demo với 3 progress bars mẫu

### 3. **Settings Menu**
- Cấu hình theme (Dark/Light)
- Chọn ngôn ngữ (Tiếng Việt/English)
- Tùy chọn auto-save và notifications

### 4. **Download Menu**
- Xem danh sách outputs đã tạo
- Mở file trực tiếp
- Export tất cả thành ZIP

### 5. **About Menu**
- Thông tin phiên bản
- Link đến GitHub repository
- Mở README documentation

## 🔌 Electron IPC API

Ứng dụng sử dụng IPC (Inter-Process Communication) để giao tiếp giữa React và Electron:

### File Dialogs
```typescript
// Mở file dialog
const filePath = await window.electronAPI.openFile([
  { name: 'CSV Files', extensions: ['csv'] }
])

// Mở folder dialog
const folderPath = await window.electronAPI.openFolder()

// Mở URL trong browser
await window.electronAPI.openUrl('https://github.com/...')

// Mở file với app mặc định
await window.electronAPI.openPath('/path/to/file')
```

## 🛠️ Development

### Hot Reload
Trong dev mode, React app sẽ tự động reload khi bạn chỉnh sửa code.

### DevTools
Electron DevTools tự động mở trong dev mode để debug.

### TypeScript
Dự án sử dụng TypeScript strict mode với type checking đầy đủ.

## 📦 Build & Distribution

### Build cho Windows
```bash
npm run electron:build
```

Tạo file `.exe` installer trong `release/`

### Build cho nhiều platform
Chỉnh sửa `package.json`:
```json
"build": {
  "win": { "target": "nsis" },
  "mac": { "target": "dmg" },
  "linux": { "target": "AppImage" }
}
```

## 🎯 Sử dụng

1. **Khởi động app**: `npm run electron:dev`
2. **Chọn Input**: Vào menu Input, chọn file CSV và PPTX template
3. **Start Processing**: Click "Start Processing" để bắt đầu
4. **Theo dõi tiến trình**: Xem progress bars và logs trong Process menu
5. **Download**: Mở outputs đã tạo từ Download menu

## 🆚 So sánh với PySide6

| Feature | PySide6 | Electron |
|---------|---------|----------|
| UI Framework | Qt6 | React |
| Language | Python | TypeScript |
| Bundle Size | ~100MB | ~150MB |
| Startup Time | Fast | Medium |
| Development | Qt Designer | Hot Reload |
| Cross-platform | ✅ | ✅ |
| Web Technologies | ❌ | ✅ |
| Native Look | ✅ | Custom |

## 🤝 Contributing

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Mở Pull Request

## 📄 License

MIT License - xem file [LICENSE](../LICENSE)

## 👤 Author

**thnhmai06**
- GitHub: [@thnhmai06](https://github.com/thnhmai06)
- Project: [tao-slide-tot-nghiep](https://github.com/thnhmai06/tao-slide-tot-nghiep)

## 🐛 Known Issues

- TypeScript errors trong dev mode (không ảnh hưởng chức năng)
- Cần cài đặt Python backend riêng để xử lý CSV → PPTX
- Chưa implement backend processing logic

## 🔮 Roadmap

- [ ] Integrate Python backend qua child process
- [ ] Implement real-time progress tracking
- [ ] Add unit tests
- [ ] Improve error handling
- [ ] Add i18n support
- [ ] Theme customization
- [ ] Auto-update functionality

---

**Note**: Đây là phiên bản Electron của dự án PySide6 ban đầu. Folder `FrontEnd/` chứa code PySide6 gốc để tham khảo.
