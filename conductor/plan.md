# SfTextComposer Implementation Plan

## Objective
Implement `SfTextComposer` in `SlideGenerator.Slides.Services` to parse and render Mustache templates using `Stubble.Core` within Syncfusion presentation shapes, supporting smart context-aware data parsing (Arrays/Objects vs Text).

## Key Files
- `SlideGenerator.Slides/Services/SfTextComposer.cs`

## Proposed Solution & Workflow

1.  **Context-Aware Variable Extraction (`Scan` API):**
    *   Implement the `public static IEnumerable<string> Scan(IShape shape)` method.
    *   Extract the entire text content from `shape.TextBody`.
    *   Use Regex or a robust scanning mechanism to extract all unique Mustache keys (`{{key}}`, `{{{key}}}`, `{{#key}}`, `{{^key}}`, `{{>key}}`, `{{key.prop}}`).
    *   Exclude comments (`{{!comment}}`).

2.  **Context Analysis and Resolution (Context Collision):**
    *   Analyze the template context to determine the expected data type of each variable.
    *   **Context Rules:**
        *   **Object/Array Context:** If a variable is used in a section `{{#var}}...{{/var}}` or inverted section `{{^var}}...{{/var}}`, or has property access `{{var.prop}}`.
        *   **Text Context:** If a variable is used purely as `{{var}}` or `{{{var}}}`.
    *   **Collision Resolution:** Nếu một biến (ví dụ `{{hobbies}}`) được sử dụng ở cả hai ngữ cảnh trong cùng một template (vừa hiển thị raw text, vừa chạy section lặp), hệ thống sẽ ưu tiên xử lý ép kiểu nó thành Object/Array. Kết quả parse sẽ được cache lại để tái sử dụng, đồng thời Stubble tự mặc định gọi `.ToString()` đối với List/Array/Object khi được in ở dạng Text nên không gây lỗi hệ thống.

3.  **Smart Data Parsing (`SmartParse` Logic):**
    *   Before passing data to `Stubble`, parse the input `IReadOnlyDictionary<string, string> instructions` into a `Dictionary<string, object>` cache.
    *   **Parsing Rules:**
        *   **Excel Array:** Chỉ coi là Excel Array nếu chuỗi bắt đầu bằng `{` và kết thúc bằng `}`, có chứa dấu phẩy `,`, và **tuyệt đối KHÔNG chứa dấu hai chấm `:`** (vì `:` là dấu hiệu nhận biết cặp key-value của JSON Object).
            *   Remove wrapping `{}` and `"` if present.
            *   Split by comma.
            *   Trim whitespace from elements.
            *   Convert to `List<string>`.
        *   **Comma-Separated Array:** Nếu chuỗi có dấu `,` và không được bao bọc bởi `{}` hay `[]`, đồng thời context yêu cầu Array. Tách theo dấu phẩy, trim và chuyển thành `List<string>`.
        *   **JSON Object/Array:** Nếu chuỗi hợp lệ chuẩn JSON (có `{}` và `:` hoặc `[]`), dùng `System.Text.Json` để parse sang `Dictionary<string, object>` hoặc `List<object>`.
        *   **Text Context / Fallback:** Nếu là text context thuần túy hoặc tất cả bước parse trên thất bại, giữ nguyên giá trị chuỗi (`string`) ban đầu.

4.  **Rendering and Shape Updating (`Replace` API):**
    *   Implement `public int Replace(IShape shape, IReadOnlyDictionary<string, string> instructions)`.
    *   Concatenate the text from all `TextParts` and `Paragraphs` to construct the full Mustache template. This is crucial because Mustache sections (`{{#section}}`) might span multiple paragraphs/text parts.
    *   Run `Stubble.Render()` using the generated template and the **parsed** dictionary cache.
    *   If the rendered output differs from the original template:
        *   Assign the rendered text to the first `TextPart` of the first `Paragraph`.
        *   Clear the text of all other `TextParts` in the shape to replace the content cleanly.
        *   Return the number of replaced text parts (or calculate logically based on keys found/replaced).

## Verification

*   **Context Collision:** `{{tags}}` and `{{#tags}}{{.}}{{/tags}}` in the same text will safely render (prioritized as Array).
*   **Excel Array:** `{"1", "2"}` renders array elements without confusing it with JSON (vì không có `:`).
*   **JSON Object:** `{"id": 1, "name": "A"}` properly parses as JSON, not an Excel array (có dấu `:`).
*   **Variables:** `{{name}}` replaces with string.
*   **Unescaped:** `{{{html}}}` replaces with string without encoding.
*   **Comma-Separated Array:** `10, 20, 30` mapped to `{{#list}}{{.}}{{/list}}` renders `102030`.