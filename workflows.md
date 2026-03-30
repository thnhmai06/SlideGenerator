Chúng ta đang làm phầm mềm tạo slide, đơn giản là từ mẫu powerpoint cho trước, dựa trên dữ liệu được cung cấp sẽ tạo ra các file powerpoint tương ứng với những người (row) trong sheet.

Yêu cầu: Nếu cần khởi tạo/mở file thì cố gắng cho phép thực thi dưới dạng Lazy nếu cần, cố gắng càng tối ưu hiệu năng và bố nhớ càng tốt.

Đầu vào: 1 Graph Request:
```csharp
public sealed record GenerateRequest(
    IReadOnlyDictionary<WorksheetIdentifier, SlideIdentifier> Graph,
    IReadOnlyList<Text.GeneralInstruction> TextInstructions,
    IReadOnlyList<Image.GeneralInstruction> ImageInstructions,
    string SaveFolder);
```
Bạn có thể deep vào trong các kiểu để tìm hiểu dữ liệu sẽ trông như thế nào.
- Graph: Dùng để Match giữa Worksheet và Slide. 1 Worksheet chỉ được map tới 1 slide.
- TextInstructions: Chứa hướng dẫn về cách để thay thế văn bản. Có:
  - GeneralInstruction là hướng dẫn vẫn còn chung, khi chưa xác định được dữ liệu có ở cột (source) nào.
  - Ta sẽ quét qua xem trong sheet đang xét có cột nào trong General không, cột nào xuất hiện trong sheet thì sẽ lấy cột đó làm nguồn dữ liệu, ta có SpecializedInstruction.
  - Placeholder là mẫu {{mustache}} có trong slide, chỉ dẫn thay thế ở chỗ nào.  Source là tên cột chỉ định dữ liệu sẽ nằm ở trên cột nào.
- ImageInstructions: Tương tự TextInstruction nhưng dành cho hình ảnh. Có thêm EditOptions chỉ dẫn sẽ chỉnh sửa ảnh như thế nào. Dữ liệu ở Source tương ứng sẽ mong muốn là URL tới ảnh. URL cần phải được resolve thông qua CloudResolver để nó trỏ tới đúng trang chỉ chứa ảnh. Sau đó tải xuống thông qua DownloadRegistry.
- SaveFolder là nơi để lưu các file powerpoint đã tạo ra, tên mỗi file là tên sheet tương ứng đã được chuẩn hóa cho tên tệp.

Quy trình phải được thực hiện 100% trên Elsa Workflows, cho phép có thể tự khôi phục khi vô tình thoát ra vào lại. Bạn cần sử dụng context7 để tìm hiểu thư viện đó. Không được lưu trạng thái mang tính tạm thời (vd nội dung file, workbook, stream...) vào state, mà nên lưu vào TransientProperties của WorkflowExecutionContext. Nếu có bước nào cần thực hiện lại khi khôi phục workflow thì hãy đánh dấu bước đó là "compensable" và đảm bảo rằng nó có thể chạy lại an toàn (idempotent).
Quy trình:
1. Từ GenerateRequest, quét Graph để xem cần những workbook nào, cần những file template nào.
2. Với mỗi cặp Worksheet-Slide trong Graph:
   - Mở file workbook tương ứng với worksheet nếu chưa mở. Nếu không tồn tại worksheet/book thì skip.
   - Tạo một bản sao của file template slide để chỉnh sửa. Xóa các slide không phải là template chính, chỉ để giữ nguyên slide template là 1 slide thôi. Sau đó quét các placeholders trong slide template này được X (bạn đặt tên).
   - Lấy used range của worksheet để biết dữ liệu nằm ở đâu. Kết hợp với TextInstructions, ImageInstructions và X để xác định được SpecializedInstruction, đảm bảo chúng không chứa dữ liệu thừa mà không dùng đến.
   - Duyệt từ trên xuống dưới của dữ liệu (không tính header)
      + Đối với dữ liệu hình ảnh, thực hiện resolve URL và lưu url đã resolve vào variable/output task để nếu có bị interpurt thì có thể tiếp tục xử lý mà không cần phải resolve lại từ đầu. Qua task tiếp theo, tải ảnh xuống thông qua DownloadRegistry. Sau khi tải xong sẽ cần phải xử lý ảnh, hãy tạo task cho tôi và thêm comment TODO vào đó, tôi sẽ xử lý phần code này. Như vậy khâu chuẩn bị đã xong
      + Thực hiện clone slide template, ta sẽ có một slide mới tương ứng với dòng đó. Đảm bảo THỨ TỰ SLIDE SAU KHI BỎ TEMPLATE SLIDE ĐẦU TIÊN PHẢI LÀ THỨ TỰ CỦA CÁC DÒNG TRONG SHEET.
      + Với mỗi dòng trong sheet, trên slide đó, thực hiện các thay thế văn bản và hình ảnh dựa trên TextInstructions và ImageInstructions đã được specialized cho worksheet đó. Nếu không thể tải xuống ảnh/không có ảnh thì giữ nguyên dữ liệu, tương tự với văn bản, nếu không có dữ liệu thì giữ nguyên placeholder.
      + Nếu cần dọn dẹp các tài nguyên như biến url đã resolve, hãy dọn dẹp cho tôi (bỏ qua bước này nếu elsa có thể làm điều đó)
    - Lưu file đã chỉnh sửa vào máy với tên đã chuẩn hóa và extension đã quy định trong settings.
    - Dọn dẹp các tài nguyên tạm thời. Nếu Workbook/Presentation không còn cần thiết nữa thì hãy đóng nó lại để giải phóng bộ nhớ, đảm bảo chỉ giữ những tài nguyên còn sử dụng nữa mà thôi.

Tôi muốn:
- Những activity không có tính phụ thuộc với nhau thì nên được thực hiện song song để tối ưu hiệu năng, nhưng tổng thể phải giới hạn số lượng row (người) được phần mềm xử lý cùng lúc trong settings.
- Đảm bảo rằng nếu workflow bị gián đoạn (ví dụ do tắt máy, lỗi phần mềm, v.v.) thì khi khôi phục lại workflow có thể tiếp tục từ bước cuối cùng đã thực hiện mà không bị mất dữ liệu hoặc phải làm lại từ đầu.
- Cố gắng bám sát vào infastructure đã có, sử dụng triệt để elsa engine, tránh viết lại logic từ đầu của cái gì đó đã có sẵn (cấm kị).
- Viết đầy đủ XML Doc, kèm theo ở activity đó lưu biến gì vào state/bộ nhớ, quy trình như thế nào trong remarks.
- Trong settings có rất nhiều cấu hình cần để điều chỉnh, tôi muốn bạn cần tích hợp nó ở workflows tương ứng.
- Chỉnh sửa slide cần thông qua XML*, chứ không phải Spire*.

