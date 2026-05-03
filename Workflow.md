# Workflow của `GeneratingWorkflow`

## Mục tiêu
- Biến một `Recipe` / `GeneratingRequest` thành các file PowerPoint đầu ra theo từng worksheet.
- Với mỗi row dữ liệu, workflow tạo một slide clone từ template, điền text và ảnh, rồi xuất file hoàn chỉnh.

## Input chính
- `Graph`: ánh xạ giữa worksheet và slide template.
- `TextInstructions`: danh sách instruction cho text placeholder.
- `ImageInstructions`: danh sách instruction cho ảnh.
- `SaveFolder` / `OutputPath`: thư mục xuất file.
- `WorkbookSummaries` và `PresentationSummaries`: dữ liệu summary để lọc và kiểm tra tính hợp lệ.

## State trung gian quan trọng
- `WorksheetKeys`: danh sách worksheet hợp lệ sau khi lọc.
- `WorksheetOutputPaths`: đường dẫn file output theo worksheet.
- `WorksheetRowIndices`: các row sẽ xử lý cho từng worksheet.
- `WorksheetTextInstructions` / `WorksheetImageInstructions`: instruction đã được lọc theo sheet/slide thực tế.
- `RowResolvedInstructions`: các image instruction đã resolve ra URI và file download.
- `DownloadTasks`, `EditTasks`, `SlideTasks`: các task chạy theo từng phase.
- `Errors`: nơi gom exception theo ngữ cảnh.

## Pipeline tổng quát

### 1) Lọc worksheet hợp lệ
- Chỉ giữ các worksheet có workbook summary tồn tại.
- Worksheet name phải khớp với workbook summary.
- Kết quả được lưu vào `WorksheetKeys`.

### 2) Setup theo từng worksheet
- Acquire gate `ReadWorkbook`.
- `CreateWorkingPresentation`:
  - kiểm tra workbook và template tồn tại;
  - copy template sang file output;
  - giữ lại chỉ slide template làm việc ở index 1.
- `SimplyInstructions`:
  - lấy placeholder text và image shapes từ summary của slide template;
  - lấy headers từ workbook;
  - lọc instruction chỉ khi khớp dữ liệu thực tế.
- Release gate `ReadWorkbook`.

### 3) Download ảnh
- Tạo `DownloadTasks` bằng tổ hợp: worksheet × row × image instruction.
- Acquire gate `DownloadImage`.
- `DownloadImage`:
  - đọc row data từ workbook;
  - `Flatten()` instruction để lấy source hợp lệ;
  - resolve cloud URI;
  - tải ảnh về thư mục temp nếu chưa tồn tại;
  - lưu kết quả vào `RowResolvedInstructions`.
- Release gate `DownloadImage`.

### 4) Chỉnh ảnh
- Tạo `EditTasks` chỉ từ các instruction đã download thành công.
- Acquire gate `EditImage`.
- `EditImage`:
  - lấy kích thước shape đích trên slide;
  - tính ROI;
  - crop/resize ảnh cho đúng kích thước;
  - ghi ra file edited.
- Nếu lỗi, workflow vẫn ghi exception và fallback bằng cách copy ảnh gốc.
- Release gate `EditImage`.

### 5) Clone slide và điền nội dung
- Tạo `SlideTasks` cho từng row.
- Acquire gate `EditPresentation`.
- `CloneTemplateSlide`:
  - clone slide template làm việc ở index 1;
  - chèn clone vào vị trí phù hợp theo row index.
- `EditSlide`:
  - build map text từ row dữ liệu;
  - replace text placeholder;
  - replace image bằng file edited đã xử lý;
  - save presentation.
- Release gate `EditPresentation`.

### 6) Finalize
- `RemoveWorkingTemplateSlide` xóa slide template làm việc khỏi file output.
- Kết quả cuối cùng chỉ còn các slide đã clone và được điền nội dung.

## Luồng dữ liệu ngắn gọn
`Recipe / Request` → lọc worksheet → tạo output template → lọc instruction → download ảnh → edit ảnh → clone slide → replace text/ảnh → xóa template slide → xuất file.

## Điểm cần lưu ý
- `Recipe.Graph` không được rỗng.
- Đường dẫn luôn được normalize bằng `Path.GetFullPath(...)`.
- Instruction bị loại nếu không khớp header, placeholder hoặc shape name.
- Lỗi được gom vào `Errors`, nên một row lỗi không nhất thiết làm hỏng toàn bộ workflow.
- Slide template làm việc luôn ở index 1 (`WorkingTemplateSlideIndex = 1`).
- `TemplateSlideIndex` phải được tính đúng để slide clone và shape lookup không bị lệch.

## Ghi chú về model
- File hiện tại có `Recipe.cs`, trong khi workflow code đang dùng kiểu tương đương `GeneratingRequest`.
- Về mặt ý nghĩa, `Recipe` là cấu trúc mô tả pipeline đầu vào: graph + text instructions + image instructions + alias map.

