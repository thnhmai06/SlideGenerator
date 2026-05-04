# Logic Pipeline Workflow: Slide Generation

Tài liệu này mô tả luồng logic thuần túy của quy trình tự động sinh Slide, không bao gồm các chi tiết triển khai kỹ thuật (biến, công cụ, framework). Quy trình được thiết kế như một dây chuyền sản xuất hàng loạt gồm 3 giai đoạn và 6 bước cốt lõi.

## Giai đoạn A: Chuẩn bị Pipeline (Xác thực & Tạo khuôn)
Giai đoạn này đảm bảo dữ liệu đầu vào là chính xác và khuôn đúc (template) đã sẵn sàng.

**Bước 1: Đối chiếu thực tế (Xác thực)**
* Hệ thống nhận danh sách các yêu cầu (request) sinh slide.
* Nhóm các yêu cầu theo file Excel (Workbook) và file PowerPoint (Presentation).
* Mở các file gốc để "soi" thực tế: Kiểm tra xem Tên sheet, Tên slide có tồn tại không? Các khung ảnh (shape), khung chữ (placeholder) mà người dùng muốn điền vào có thực sự tồn tại trên slide đó không?
* Nếu yêu cầu nào trỏ tới một thành phần không có thực, hệ thống sẽ gạch bỏ (xóa) yêu cầu đó ngay lập tức để không tạo ra "rác" cho luồng xử lý phía sau.

**Bước 2: Đặt khuôn (Tạo file đích)**
* Lấy file PowerPoint chứa template gốc copy sang một vị trí mới để làm file thành phẩm (cấu trúc thư mục: `Vị trí lưu/Tên Excel/Tên Sheet.ext`).
* Mở file thành phẩm này ra và thực hiện dọn dẹp: Xóa tất cả các slide, chỉ giữ lại đúng 1 slide duy nhất chứa các khung mẫu (thường là slide đầu tiên). 
* Slide duy nhất này chính là "khuôn đúc" chuẩn cho toàn bộ quá trình nhân bản sau này.

---

## Giai đoạn B: Chuẩn bị Tài nguyên (Sơ chế Ảnh)
Giai đoạn này tách biệt việc xử lý ảnh ra khỏi việc làm slide. Điểm mấu chốt là nguyên tắc **"Làm rồi thì không làm lại" (Idempotency)** để tối ưu hiệu suất và chống đứt gãy.

**Bước 3: Nhập nguyên liệu thô (Tải ảnh)**
* Quét từng dòng dữ liệu trong Worksheet xem có cột nào chứa link ảnh cần tải không.
* Trước khi tải, hệ thống kiểm tra thư mục tạm: *Ảnh của dòng này đã từng được tải xuống thành công chưa?*
* Nếu chưa có, tiến hành phân giải link và tải ảnh thô nguyên bản từ Cloud về thư mục tạm (cấu trúc: `Temp/Workbook/Worksheet/Column/Download/RowIndex.ext`).

**Bước 4: Sơ chế vừa khuôn (Chỉnh sửa ảnh)**
* Hệ thống đọc kích thước thật của khung ảnh trên "khuôn đúc" (ở Bước 2).
* Lấy ảnh thô (đã tải ở Bước 3), tiến hành cắt (crop) và thay đổi kích thước (resize) cho vừa khít 100% với khung đích.
* Lưu ảnh đã chỉnh sửa vào một thư mục tạm khác (`Temp/Workbook/Worksheet/Column/Edit/RowIndex.ext`).
* *(Tùy chọn)* Nếu có yêu cầu dọn dẹp ảnh thô sau khi tải, tiến hành xóa ảnh ở thư mục Download để tiết kiệm dung lượng.
* Tương tự Bước 3, hệ thống luôn kiểm tra xem ảnh sơ chế đã tồn tại chưa để tránh làm lại.

Chú thích: * **Temp** là folder được quy định trong Setting, không phải %temp%.

---

## Giai đoạn C: Thay thế (Lắp ráp & Hoàn thiện)
Đây là lúc dây chuyền chạy ra sản phẩm cuối cùng.

**Bước 5: Đổ khuôn và Lắp ráp (Điền dữ liệu)**
* Mở file PowerPoint "thành phẩm" (đã chuẩn bị ở Bước 2) tương ứng với từng Worksheet.
* Chạy tuần tự từng dòng dữ liệu (Row) đã được xác thực:
    1.  **Nhân bản:** Copy slide khuôn mẫu (đang ở vị trí số 1) và chèn ngay phía sau.
    2.  **Điền Text:** Tìm và thay thế các đoạn mã đánh dấu (Mustache) bằng chữ thật từ Excel.
    3.  **Chèn Ảnh:** Lấy những bức ảnh đã sơ chế (ở Bước 4) gắp thả vào đúng khung ảnh (Picture/BlipFill).
* Việc chạy tuần tự đảm bảo thứ tự slide sinh ra khớp hoàn toàn với thứ tự dòng dữ liệu trong Excel.

**Bước 6: Rút khuôn và Dọn dẹp (Hoàn thiện)**
* Sau khi chạy xong tất cả các dòng, file lúc này gồm 1 slide khuôn ở trên cùng và hàng loạt slide thành phẩm ở dưới.
* Hệ thống tiến hành **xóa bỏ slide khuôn ban đầu** (slide số 1).
* Lưu và đóng file PowerPoint lại.
* *(Tùy chọn)* Nếu có yêu cầu dọn dẹp, hệ thống sẽ xóa sạch sẽ ảnh sơ chế trong thư mục Edit.

---

## Các Nguyên tắc Hoạt động Cốt lõi
* **Giới hạn truy cập (Concurrency/Gatekeeping):** Tại mỗi bước cần mở file (Excel, PowerPoint) hoặc tải mạng, hệ thống luôn có "cổng kiểm soát" giới hạn số lượng tác vụ chạy song song để không làm quá tải RAM/CPU hoặc bị hệ điều hành khóa file. Đó chính là cơ chế GateLocker theo Gate mà tôi đã triển khai ròi. 
* **Quản lý thực thể (Instance Management):** Để tối ưu, nếu một file cần đọc/ghi nhiều lần trong cùng một bước, hệ thống sẽ giữ file đó mở (cache instance nội bộ) thay vì liên tục đóng/mở lại, đảm bảo không có 2 tiến trình cùng cố mở 1 file gây xung đột.
* **Tính toàn vẹn (Persistence):** Các bước được thiết kế độc lập. Nếu quy trình bị gián đoạn (ví dụ: mất điện, rớt mạng ở Bước 4), khi chạy lại, hệ thống sẽ bỏ qua Bước 1, 2, 3 và tiếp tục chính xác từ chỗ bị đứt ở Bước 4.
* **Phrase trước hoàn tất tất cả thì mới đến lượt phrase sau chạy. Không được nhảy cóc Phrase.**

## Yêu cầu Plan:
* KHÔNG ĐƯỢC SỬA/THÊM/XÓA NHỮNG MODELS MÀ TÔI ĐÃ VIẾT, NHỮNG PROJECT KHÁC TRONG CODE HIỆN TẠI. Chỉ thao tác TRONG PHẠM VI SlideGenerator.Services.