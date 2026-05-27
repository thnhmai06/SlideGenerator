import type { LogEntry } from "@/types/workflow";

export const mockLogEntries: LogEntry[] = [
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:00.000Z",
    message: "Workflow bắt đầu: Giấy khen học sinh - Đợt 1",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:01.120Z",
    message: "LoadRecipeSummary: Đọc recipe ID=1",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:01.540Z",
    message: "LoadRecipeSummary hoàn thành. 1 MapNode đã được tải.",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:02.010Z",
    message: "PreflightCleanup: Khởi tạo 1 Worksheet hợp lệ, 3 ValidationItem.",
  },
  { level: "INFO", timestamp: "2026-05-20T08:00:02.550Z", message: "PreflightCleanup hoàn thành." },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:03.210Z",
    message: "ValidateRequest: Kiểm tra C:\\Data\\DanhSachHocSinh.xlsx...",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:03.890Z",
    message: "ValidateRequest hoàn thành. File Excel hợp lệ.",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:04.310Z",
    message: "CreateTemplate: Sao chép C:\\Templates\\GiayKhenHocSinh.potx...",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:05.120Z",
    message: "CreateTemplate hoàn thành. Output: C:\\Output\\GiayKhen_20260520_080005.pptx",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:05.990Z",
    message: "ExtractData: Đọc 42 dòng từ sheet 'Lớp 10A1'...",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:06.880Z",
    message: "ExtractData: 42 SlideContext, 42 ImageContext đã tạo.",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:07.230Z",
    message: "AcquireImage [Row 1]: Tải ảnh từ https://example.com/an.jpg",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:08.100Z",
    message: "AcquireImage [Row 1]: Đã lưu vào C:\\Assets\\Download\\an.jpg (128KB)",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:08.540Z",
    message: "EditImage [Row 1]: Crop Center (pivot 0.5, 0.5), Face detection ON",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:09.210Z",
    message: "EditImage [Row 1]: Phát hiện 1 khuôn mặt, điều chỉnh pivot.",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:09.880Z",
    message: "EditImage [Row 1]: Hoàn thành → C:\\Assets\\Edit\\an_edited.jpg",
  },
  {
    level: "WARNING",
    timestamp: "2026-05-20T08:00:12.100Z",
    message: "AcquireImage [Row 3]: URL không trả về (timeout 5s), dùng fallback.",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:12.550Z",
    message: "AcquireImage [Row 3]: Sử dụng ảnh dự phòng C:\\Assets\\default_avatar.jpg",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:15.210Z",
    message: "ReplaceSlideData [Row 1]: Điền 1 text, 1 ảnh vào slide...",
  },
  {
    level: "DEBUG",
    timestamp: "2026-05-20T08:00:15.990Z",
    message: "ReplaceSlideData [Row 1]: TenHocSinh='Nguyễn Văn An', AnhHocSinh=OK",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:16.440Z",
    message: "ReplaceSlideData [Row 1]: Hoàn thành.",
  },
  {
    level: "ERROR",
    timestamp: "2026-05-20T08:00:22.100Z",
    message: "AcquireImage [Row 9]: Kết nối bị từ chối (403 Forbidden)",
    details: "URL: https://example.com/ich.jpg\nHTTP 403\nDùng fallback nếu đã cấu hình.",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:00:22.550Z",
    message: "AcquireImage [Row 9]: Sử dụng ảnh dự phòng (fallbackImagePath).",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:01:45.000Z",
    message: "CloseAllHandles: Đóng tất cả file handle.",
  },
  {
    level: "INFO",
    timestamp: "2026-05-20T08:01:45.500Z",
    message:
      "Workflow hoàn thành. 42 slide đã tạo. Xem output tại C:\\Output\\GiayKhen_20260520_080005.pptx",
  },
];
