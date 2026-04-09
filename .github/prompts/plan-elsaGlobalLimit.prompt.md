## Kế hoạch 01: Giới hạn toàn cục worksheet bằng Elsa + tách 2 pipeline

### Mục tiêu
1. Giới hạn số worksheet được xử lý đồng thời ở cấp độ toàn cục, không phân biệt workflow, mặc định là 2.
2. Không dùng cơ chế pause activity để chờ lượt trong bộ nhớ; chỉ tạo hoặc dispatch instance khi có slot.
3. Tách luồng xử lý thành 2 pipeline chính:
- Pipeline Chuẩn bị: từ đầu đến DownloadImages và EditImages.
- Pipeline Chỉnh sửa: clone và thay thế nội dung slide sau khi chuẩn bị xong.

### Cách tiếp cận
1. Trước khi triển khai, tra cứu MCP server DeepWiki về Elsa để xác nhận đúng hành vi runtime, dispatcher, ParallelForEach, persistence và throttling của phiên bản đang dùng.
2. Xác định chính xác điểm chèn admission control tại boundary IPC và Application.
3. Tách orchestration thành 2 workflow Elsa độc lập theo mô hình producer và consumer.
4. Định nghĩa handoff contract giữa 2 pipeline để truyền dữ liệu từ Chuẩn bị sang Chỉnh sửa.
5. Triển khai queue giới hạn và semaphore toàn cục để tránh over-dispatch.
6. Bảo đảm release slot theo đúng lifecycle success, fail hoặc cancel của worksheet.

### Tệp cần tác động
1. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Application/Tasks/Generation/GenerationWorkflow.cs
2. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Application/Tasks/Generation/Activities/*
3. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Ipc/Program.cs
4. d:/Development/Code/.multi/SlideGenerator/SlideGenerator.Ipc/Endpoints/RpcEndpoint.Jobs.cs

### Xác minh
1. Kiểm thử active worksheet toàn cục không vượt quá limit.
2. Kiểm thử nhiều job chạy song song, số editing worker luôn nhỏ hơn hoặc bằng 2.
3. Kiểm thử failure isolation theo worksheet.
4. Kiểm thử cancel để xác nhận slot được giải phóng đúng lúc.
