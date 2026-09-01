# Tiến độ overhaul Avalonia frontend

Cập nhật: 2026-09-01. Kế hoạch đầy đủ: `C:\Users\haith\.claude\plans\snoopy-tinkering-river.md` (ngoài repo, không commit được).

## Trạng thái theo phase

| Phase | Trạng thái | Ghi chú |
|---|---|---|
| P0 | Xong | 2 spike (custom titlebar, theme reveal) — kết luận đã áp dụng ở P2 |
| P1 | Xong | Foundation: token, icon, style class, `DesignSystemTests` gate + 2 bug crash/theme-runtime |
| P2 | Xong | Shell mới (toolbar-titlebar) + motion + theme reveal hình tròn |
| P3 | Xong | i18n dot-key migration toàn bộ |
| P4 (a+b+c) | Xong | Recipes/RecipeEditor/RunDialog/TemplatePicker polish |
| P5 | Xong | `TotalRows` backend contract + Runs live UI |
| P6 | Xong | About page + Settings rebuild + sponsors CI |
| P7 | Phần lõi xong, 3 mục treo chờ quyết định | xem dưới |

## P7 — đã làm (commit `39e12589`, `b4604c3a`, `3f073309`)

1. **Headless test activation**: `Avalonia.Headless.XUnit` 12.1.1 lệch version với `xunit.v3` 4.0.0 (`MissingMethodException` lúc discovery) → đổi sang gọi thẳng `HeadlessUnitTestSession` (bỏ package `.XUnit`). 4 test view-construct (Recipes/Runs/Settings/About) chạy được. Sau đó phát hiện + sửa 1 race condition: mỗi test class tự `StartNew` session riêng đụng độ `Application.Current` (process-wide singleton) khi xUnit chạy song song → gộp về `HeadlessTestSession.Instance` dùng chung cho cả assembly.
2. **Dọn `PlaceholderPageView`**: mồ côi (4/4 destination đã có trang thật) — xoá hẳn, `default:` case đổi thành `throw UnreachableException` thay vì ẩn lỗi tương lai.
3. **Sửa CLAUDE.md drift đã xác minh cụ thể**: tên/số lượng migration script (`0001/0002/0003` cũ → thực tế chỉ `001_2.0.0.sql` + `002_add-total-rows-to-jobs.sql`), đường dẫn `NameAndPaths.cs` (`Rules/` cũ → thực tế `Immutable/`), version test package (`xunit.v3`/`xunit.runner.visualstudio`/`NSubstitute` doc ghi cũ hơn thực tế), `SlideGenerator.Stdio/Program.cs` → `SlideGenerator.Desktop/Program.cs` (Stdio đã xoá khỏi solution).
4. **11 test VM/service mới**: stats aggregation (`RecipesViewModel`), `TotalRows` mapping (`JobRunViewModel`), `AboutViewModel` (mock HTTP), `ThemeService` reduced-motion branch.
5. **Bug thật bắt được khi mở rộng contrast gate**: nav-pill active tab + avatar initials (About page) hiện chữ dưới ngưỡng đọc được ở dark mode (2.57:1, cần ≥4.5:1) — do `AccentBrush` trên `AccentMutedBrush` chưa từng được `DesignSystemTests` gate check. Sửa: đậm `AccentMutedBrush` dark từ `#2A4E85` → `#152742` (giữ hue, chỉ đậm hơn) → 4.63:1.

**Kết quả**: build solution 0 lỗi; **548 test, 547 xanh, 1 skip** (Syncfusion license, có từ trước, không phải regression).

## 3 mục còn treo — cần chủ project quyết

1. **CLAUDE.md's section "IPC Layer (SlideGenerator.Stdio)"** mô tả kiến trúc JSON-RPC/StreamJsonRpc **không còn tồn tại trong code** (module Stdio đã xoá hẳn khỏi solution, `grep StreamJsonRpc` rỗng). Đây là drift quy mô LỚN (cả 1 section + nhiều bảng liên quan trong CLAUDE.md), vượt xa phạm vi "sửa 4 câu drift nhỏ" đã làm — chưa tự ý viết lại, cần xác nhận có muốn dành riêng 1 việc cho việc này không.
2. **Focus ring bàn phím**: hiện chỉ có ở `Button` (từ P1) — `ToggleButton`/`ListBoxItem`/`TextBox`/`NumericUpDown` chưa có ring tuỳ biến, chỉ dựa Semi mặc định. Đã xác nhận rõ (không mơ hồ), cố ý chưa mở rộng để tránh vỡ ảnh đã duyệt ở các phase trước mà không có bằng chứng cụ thể đang hỏng.
3. **Ma trận 20 screenshot §7.0.B** (blueprint) chưa chạy chính thức theo checklist — bị chặn bởi thiếu `SYNCFUSION_LICENSE_KEY` lúc runtime cho Desktop app (không tìm thấy code nào tự load `.env`), nên không chạy được 1 lượt generation thật để chụp job-đang-chạy có tiến độ live. Đã hỏi từ P5 (câu hỏi mở Q8 trong plan gốc), chưa có câu trả lời.

## Ràng buộc đang giữ

- `plans/` KHÔNG BAO GIỜ commit vào git — file này (`status.md`) và toàn bộ `plans/` vẫn nằm ngoài git (`git status` xác nhận `?? plans/` trước mỗi commit).
- Không sửa file trong danh sách cấm chạm (ViewModels, `TrExtension.cs`, `ProgressHub.cs`, `MappingEditSession`, 10 module domain) mà không hỏi trước — chưa vi phạm lần nào trong P7.
