# Tiến độ backend-remediation (ledger)

Plan điều phối: `C:\Users\haith\.claude\plans\c-c-c-file-plans-twinkly-sedgewick.md`. Resume: đọc plan, `git log`, file này.

## Môi trường / baseline

- Restore chạy offline từ cache `V:\.cache\nuget` (không cần GITHUB_TOKEN).
- `Data.db` dev cũ đã đổi tên thành `Data.db.bak-<timestamp>` (không xoá) trong `%LOCALAPPDATA%\SlideGenerator\Data\`.
- Integration test đọc `SYNCFUSION_LICENSE_KEY` từ **biến môi trường**, không đọc `.env`; key cũng được bake lúc build `Document`.
  Chạy test: nạp key từ `.env` vào `$env:SYNCFUSION_LICENSE_KEY` rồi build lại `Document` trước khi chạy.
- OpenCode: `deepseek-v4.1-flash` bị chặn ("requires Global regions") — dùng `mimo-v2.6-flash` cho việc cơ học/vừa.
- Baseline sau A.2 (555 test): Cloud 48, Desktop 118, Document 75, Generator 64, Image 142, Logging 15, Recipe 26,
  Settings 7, Utilities 53, Workflows 7. Tất cả xanh (Generator có 1 skip nếu thiếu license key).

## Mục

| mục | trạng thái | commit | model | test trước → sau | ghi chú |
|---|---|---|---|---|---|
| 0 restore + backup DB | xong | — | — | — | restore offline OK |
| A.1/A.2 build + test | xong | (xem git log) | orchestrator (XML doc) + mimo-v2.6-flash (JobRow) | 7 fail → 0 fail (555 total) | JobRow: Dapper khớp ctor theo vị trí cột, `TotalRows` phải ở vị trí 6 |
| A.3 integration tests | xong | (xem git log) | minimax-m3 (viết) + deepseek-v4.1-flash (sửa theo review) + orchestrator (6 sửa vi mô sau lint) | Generator 64 → 69, 3 lần chạy liên tiếp 69/69 | 5 file trong `tests/SlideGenerator.Generator.Tests/Integration/`. Phiên deepseek bị dừng vì thiếu RAM (client shell), nhưng agent phía server vẫn chạy xong; đã tự xác minh lại diff + test. **Harness tự dựng `LoggingWorkflowRunner` để né lỗi logger singleton (lỗi thật, sửa ở mục 1.1; sau 1.1 đổi sang `Controller.StartWorkflowAsync` và bỏ workaround).** Test pause (b): cổng `JobGate` không đảm bảo job nào thắng slot nên test chỉ kiểm "không có snapshot mới khi paused" + job còn lại không đổi trạng thái; "job đang chờ slot không chạy khi pause" chỉ được kiểm lỏng. Test (d) mô phỏng crash bằng stop + rewind trạng thái về Running (không phải kill -9). |
| Quy ước | — | — | — | — | Model mặc định `opencode-go/deepseek-v4.1-flash` (đã bật Global). Scenario (e) logger request thứ hai chuyển thành test repro của 1.1. Cron nhắc 1:37am + mỗi 5h (session-only). |
| 1.1 file logger factory | xong | (xem git log) | deepseek-v4.1-flash (spec do Claude thiết kế) | Logging 15 → 17, Generator 69 → 69 | Đỏ chứng minh: trên src cũ test 2-workflow qua controller thật lỗi `CreateLogger() was previously called`. `SerilogFileLoggerFactory` nhận `Func<LoggerConfiguration>` (mỗi `CreateFile` một config mới, cũng hết cộng dồn sink), đăng ký Singleton. Đã bỏ workaround ở `WorkflowTestHarness` (giờ gọi `Controller.StartWorkflowAsync`). `AddLoggingServices` yêu cầu container có `LoggerConfiguration` Transient (đã ghi trong XML doc; Desktop đã có sẵn). |
| 1.2 đường dẫn log theo workflowId + xoá log | xong | (xem git log) | deepseek-v4.1-flash (spec do Claude thiết kế) | Generator 69 → 73 | Đỏ chứng minh: 3/4 test mới fail trên Service.cs cũ (test "log đã mất" vốn xanh). Log = `Logs/Workflows/{workflowId}.log`; `DeleteAsync`/`DeleteAllCompletedAsync` xoá file log (best-effort, IOException/UnauthorizedAccess chỉ ghi cảnh báo). Không thêm `shared: true`: mỗi file chỉ có đúng một sink của workflow của nó. Xoá log có thể thất bại nếu workflow đang dừng dở vì `DeleteAsync` chưa chờ workflow thoát — mục 2.3 sửa bằng `waitForExit`. Cảnh báo cho phiên sau: `lint_files` trên file lớn trả rất nhiều cảnh báo "allocation" — dùng cho file nhỏ hoặc bỏ qua. |
