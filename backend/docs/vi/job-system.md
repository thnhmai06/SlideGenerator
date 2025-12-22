# Hệ thống Job

## Mục lục

1. [Khái niệm](#khái-niệm)
2. [Mô hình composite](#mô-hình-composite)
3. [Active và completed](#active-và-completed)
4. [Vòng đời](#vòng-đời)
5. [Pause/Resume](#pause-resume)
6. [Lưu trạng thái và resume](#lưu-trạng-thái-và-resume)
7. [Cancel và clear](#cancel-và-clear)

## Khái niệm

- **Group**: một lô tạo từ 1 workbook + 1 template.
- **Sheet**: job cho một worksheet, sinh một file output.
- **Active jobs**: pending/running/paused.
- **Completed jobs**: completed/failed/cancelled.

## Mô hình composite

Domain dùng composite:

- `IJobGroup` là composite root.
- `IJobSheet` là leaf.
- `IJobGroup.Sheets` trả về danh sách con.

## Active và completed

`IJobManager` chia quản lý thành hai collection:

- `IJobManager.Active` (`IActiveJobCollection`)
  - Tạo group.
  - Điều khiển job đang chạy (start/pause/resume/cancel).
- `IJobManager.Completed` (`ICompletedJobCollection`)
  - Lưu nhóm đã xong.
  - Hỗ trợ remove/clear.

### Tự động chuyển

Khi toàn bộ sheet trong group ở trạng thái Completed/Failed/Cancelled, group được chuyển sang Completed.

## Vòng đời

### Tạo và start

- Create: `IActiveJobCollection.CreateGroup(request)`
- Start: `IActiveJobCollection.StartGroup(groupId)`

### Điều khiển group

- Pause: `PauseGroup(groupId)`
- Resume: `ResumeGroup(groupId)`
- Cancel: `CancelGroup(groupId)`

### Điều khiển sheet

- Pause: `PauseSheet(sheetId)`
- Resume: `ResumeSheet(sheetId)`
- Cancel: `CancelSheet(sheetId)`

## Pause/Resume

Sheet dùng cơ chế wait theo sự kiện:

- Pause sẽ chặn gần như ngay lập tức.
- Resume tiếp tục ngay (không polling).

Ghi chú triển khai:

- Domain: `JobSheet.WaitIfPausedAsync(token)`
- Executor: đặt checkpoint trước/sau row, resolve cloud, download, xử lý ảnh, cập nhật slide, lưu state.

## Lưu trạng thái và resume

- State được lưu bằng HangfireSQLite.
- Mỗi sheet lưu `NextRowIndex`, status, output path, error count.
- Resume chạy tiếp row kế tiếp; nếu thiếu output file khi resume thì fail sheet.

## Cancel và clear

- Cancel active:
  - Theo group: `IActiveJobCollection.CancelGroup`
  - Toàn bộ: `IActiveJobCollection.CancelAll`
- Cancel + remove (Stop trên UI):
  - Theo group: `IActiveJobCollection.CancelAndRemoveGroup`
  - Theo sheet: `IActiveJobCollection.CancelAndRemoveSheet`
  - Xóa file output và xóa state đã lưu.
- Clear completed:
  - Theo group: `ICompletedJobCollection.RemoveGroup`
  - Toàn bộ: `ICompletedJobCollection.ClearAll`
