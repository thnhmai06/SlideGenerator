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
| A.3 integration tests | **chờ review, CHƯA commit** | — | minimax-m3 | Generator 64 → 69 (agent báo, 3 lần chạy ổn định) | 5 file mới chưa theo dõi trong `tests/SlideGenerator.Generator.Tests/Integration/` (Concurrency, Control, Failure, Resume, WorkflowTestHarness). Việc còn lại: review từng dòng, Rider reformat+lint (agent báo `get_file_problems` trả null), build, commit. Harness tự dựng `LoggingWorkflowRunner` để né lỗi logger singleton — lỗi thật, sửa ở mục 1.1; sau 1.1 nên bỏ workaround đó. Test pause chỉ kiểm bất biến sau pause, không kiểm "xong hàng đang dở". |
| Quy ước | — | — | — | — | Model mặc định `opencode-go/deepseek-v4.1-flash` (đã bật Global). Scenario (e) logger request thứ hai chuyển thành test repro của 1.1. Cron nhắc 1:37am + mỗi 5h (session-only). |
