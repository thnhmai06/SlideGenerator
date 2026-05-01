# Kế hoạch Triển khai: Tích hợp Workflow-Core cho Kiến trúc AST

## 1. Mục tiêu
Chuyển đổi cơ chế điều phối Workflow từ việc sử dụng một `WcInterpreterStep` nguyên khối (thực thi vòng lặp duyệt AST bằng tay) sang việc sử dụng trực tiếp các tính năng điều phối của **Workflow-Core** (Fluent API: `.Then`, `.Parallel`, `.ForEach`, `.If`). Code ở `Application Layer` (định nghĩa AST và logic Activity) phải được giữ nguyên hoàn toàn.

## 2. Phân tích Kiến trúc và Đề xuất Giải pháp

### 2.1. Vấn đề của hiện tại
Hiện tại, `WcWorkflowAdapter.Build` chỉ tạo ra duy nhất 1 step là `WcInterpreterStep`. Bên trong step này, một interpreter chạy toàn bộ cây AST.
- **Nhược điểm**: Workflow-Core không biết được các bước thực tế bên trong. Không thể tận dụng được cơ chế Logging từng bước, Persistence, Error Handling, và Retries của Workflow-Core.

### 2.2. Đề xuất: AST-to-WorkflowCore Mapper
Thay vì có 1 step, chúng ta sẽ viết một **Mapper** chạy ở giai đoạn `Build(IWorkflowBuilder<TData> builder)`. Mapper này sẽ duyệt cây AST đệ quy và dịch mỗi node thành các lệnh tương ứng của Workflow-Core.

#### A. Ánh xạ các Nodes cấu trúc (Structural Nodes)
- **Sequence**: Duyệt mảng `Steps` trong AST và nối tiếp chúng bằng phương thức `.Then(...)` của Workflow-Core.
- **Parallel**: Sử dụng phương thức `.Parallel()` của Workflow-Core. Mỗi nhánh của node `Parallel` trong AST sẽ tương ứng với một `.Do(...)`. Kết thúc bằng `.Join()`.
- **ForEach**: Sử dụng phương thức `.ForEach(...)` của Workflow-Core. Hàm lấy dữ liệu (Items) từ AST sẽ được ánh xạ sang hàm đọc từ `TData`/`Context`. Phần thân (`Body`) của ForEach sẽ được build bên trong block `.Do(...)`.
- **Condition (If)**: Dịch sang `.If(...)` của Workflow-Core.

#### B. Ánh xạ Activity (Nghiệp vụ)
- Tạo một Step chung trong Infrastructure: `GenericActivityStep<TData> : StepBodyAsync`.
- Step này sẽ nhận vào instance của `Activity<TData>` (thông qua `.Input(step => step.Activity, data => activityInstance)`).
- Tại thời điểm `RunAsync`, `GenericActivityStep` sẽ tạo một implementation của `IExecutionContext<TData>` (gói `IStepExecutionContext` của Workflow-Core) và gọi hàm `ExecuteAsync` của AST Activity.

#### C. Quản lý Context và Variables (Scopes)
- Trong AST, `IExecutionContext` có tính chất cây (phạm vi biến toàn cục và cục bộ trong vòng lặp) truy cập thông qua `Handle<T>`.
- **Giải pháp**: Yêu cầu (hoặc tự động inject) đối tượng `TData` của Workflow-Core có khả năng chứa một `Dictionary<string, object>` để lưu các biến toàn cục.
- **Xử lý child scope cho ForEach**:
  - Workflow-Core tự quản lý scope của vòng lặp và cung cấp phần tử hiện tại thông qua `context.Item`.
  - Chúng ta sẽ chèn một step phụ `SetLoopVariableStep` ngay đầu block `.Do` của `ForEach` để đọc `context.Item` và gán vào Variable dictionary ứng với `Handle<T>` của vòng lặp, sau đó mới gọi body của ForEach. Để đảm bảo an toàn cho parallel loop, cấu trúc dictionary cần hỗ trợ phân biệt biến theo `ExecutionPointer.Id` (hoặc `context.ExecutionPointer`) để tách biệt scope.

## 3. Các bước triển khai (Implementation Steps)

1. **Chuẩn bị hạ tầng Variables**:
   - Tạo class `WorkflowVariableManager` (hoặc mở rộng `TData` hiện tại nếu được) để có thể lưu biến an toàn trong mô hình đa luồng của Workflow-Core. Cần thiết kế để mapping ID của `ExecutionPointer` với dictionary tương ứng (để giả lập Child Scope).
   - Viết class `WcExecutionContextAdapter` kế thừa `IExecutionContext<TData>` dùng chung cho `GenericActivityStep`. Adapter này sẽ giao tiếp với `WorkflowVariableManager`.

2. **Tạo Generic Steps**:
   - Tạo `GenericActivityStep<TData> : StepBodyAsync` chịu trách nhiệm chạy 1 Activity duy nhất.
   - Tạo `SetVariableStep<TData> : StepBodyAsync` để dùng cho ForEach (gán `context.Item` vào biến).

3. **Xây dựng AST Mapper**:
   - Tạo class `AstToWorkflowCoreMapper` chứa logic đệ quy:
     - Xử lý `Sequence<TData>`.
     - Xử lý `Parallel<TData>`.
     - Xử lý `ForEach<TItem, TData>`.
     - Xử lý `Activity<TData>` thường.

4. **Tích hợp vào Adapter hiện tại**:
   - Cập nhật `WcWorkflowAdapter<TDef, TData>.Build(...)` để thay vì gọi `.StartWith<WcInterpreterStep...>()`, nó sẽ gọi `AstToWorkflowCoreMapper.Map(builder, def.Build())`.

5. **Dọn dẹp (Cleanup)**:
   - Xóa bỏ `WcInterpreterStep`, `WcInterpreterContext` (những tệp cũ không còn cần thiết sau khi chuyển sang cơ chế Mapper).
   - Đảm bảo tất cả các test liên quan đến việc tạo Workflow vẫn pass.

## 4. Xác nhận (Verification)
- Mọi Workflow cũ viết bằng DSL (AST) vẫn chạy đúng kết quả.
- Dữ liệu loop trong ForEach (cả tuần tự và song song) không bị xung đột (Race conditions) do đã được phân tách scope chính xác.
- Khi quan sát bằng công cụ hiển thị Workflow-Core (hoặc Database Persistence), chúng ta sẽ thấy từng bước nghiệp vụ được tách ra độc lập (có đầy đủ event Start/End cho từng Activity thay vì chỉ 1 block duy nhất).