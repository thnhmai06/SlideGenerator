# SlideGenerator — Frontend Design & Implementation Brief

## 1. Tổng quan

Đây là toàn bộ ý tưởng frontend cho project **SlideGenerator**.

Frontend sẽ được xây dựng bằng **Avalonia UI** và mục tiêu cuối cùng là triển khai **frontend E2E**, không chỉ dừng lại ở việc thiết kế giao diện trong Figma.

Thiết kế cần được thực hiện với khả năng triển khai thực tế trong Avalonia làm mục tiêu. Vì vậy, ngoài việc thiết kế các page, cần xây dựng hệ thống **components, states, interactions, animations và application shell** đủ rõ ràng để có thể chuyển thành frontend hoàn chỉnh.

Project tương đối phức tạp nên không cần cố gắng giải quyết tất cả mọi thứ ngay từ đầu. Có thể tập trung vào việc:

* Thiết kế các UI components cần thiết.
* Đặt các components vào trong các pages để minh họa cách sử dụng.
* Thiết kế application shell và toolbar.
* Thiết kế các trạng thái, interaction và animation quan trọng.
* Sau đó triển khai frontend thực tế bằng Avalonia.

Nếu một component có vấn đề hoặc cách thể hiện chưa được chỉ rõ, có thể chủ động thiết kế theo cách phù hợp.

---

# 2. Công nghệ và phạm vi triển khai

Frontend sử dụng:

**Avalonia UI**

Mục tiêu là triển khai **frontend E2E**.

Điều này có nghĩa là Figma/design chỉ là bước thiết kế; kết quả cuối cùng cần là một frontend Avalonia thực tế của SlideGenerator.

UI nên được tổ chức theo hướng component có thể tái sử dụng trong code, thay vì thiết kế từng màn hình như những sản phẩm hoàn toàn độc lập.

Các phần chính cần được xem xét trong toàn bộ frontend:

* Application shell.
* Toolbar phía trên cùng.
* Navigation/sidebar nếu cần.
* Các page chính.
* Reusable components.
* Light/Dark theme.
* Theme transition animation.
* Logo animation.
* Hover/focus/active states.
* Các UI state cần thiết cho application.

---

# 3. Phạm vi thiết kế

Vì project khá phức tạp, không cần thiết kế mọi thứ thành một màn hình hoàn chỉnh ngay lập tức.

Có thể tập trung vào:

1. Thiết kế các **components**.
2. Tạo các **variants/states** cần thiết cho components.
3. Đặt chúng vào các **pages** để minh họa.
4. Đảm bảo các pages và components tạo thành một application UI thống nhất.
5. Sau đó dùng thiết kế này làm cơ sở để triển khai frontend bằng Avalonia.

Các component nhỏ hoặc phổ biến có thể sử dụng thư viện UI bên ngoài thay vì tự thiết kế lại từ đầu.

---

# 4. Phong cách giao diện

Phong cách tổng thể mong muốn:

* Hiện đại.
* Thoáng.
* Không quá dày đặc.
* Có cảm giác polished.
* Gần với style của **v1** đang thực hiện.
* Có thể tham khảo **Unsloth** về cách tổ chức giao diện, khoảng trống và animation.

Không cần thiết kế theo kiểu quá nặng tính enterprise hoặc quá nhiều panel nhỏ chen chúc nhau.

---

# 5. Application Shell

Application shell là một phần quan trọng của frontend.

Đặc biệt, **toolbar phía trên cùng cũng là một phần chính thức của giao diện app và phải được thiết kế**.

Không nên chỉ tập trung vào phần nội dung page rồi bỏ qua toolbar.

Toolbar cần được xem như một reusable part của application shell và phải phù hợp với toàn bộ hệ thống UI.

---

# 6. Logo và logo animation

Project có **logo animation**.

Quy tắc mong muốn:

> Khi một khu vực/component hiển thị logo đầy đủ cả tên thì sẽ hiển thị cả animation.

Animation có trình tự:

**logo → animation → full → giữ**

Sau khi animation hoàn thành, logo sẽ ở trạng thái đầy đủ và giữ nguyên trạng thái đó.

Logo animation không chỉ được xem là một animation riêng lẻ, mà là một phần của visual identity của application.

---

# 7. About Page

About Page cần có các phần theo thứ tự sau.

## Logo

Hiển thị **logo đầy đủ cả tên** ở giữa.

Tại những nơi hiển thị logo đầy đủ tên, sử dụng logo animation đã mô tả ở trên.

## Mô tả phần mềm

Hiển thị ở giữa:

**An automated, template-based presentation generator**

và:

**Phần mềm tự động tạo slide thuyết trình theo mẫu.**

Nội dung được căn giữa.

## Kiểm tra cập nhật

Có một khu vực dành cho **Check for Updates**.

Khu vực này cần hiển thị:

* Phiên bản hiện tại.
* Thông tin cập nhật.

Cách trình bày có thể tham khảo cách **Google Chrome** hiển thị update.

## Developers

Hiển thị những người tham gia phát triển project.

Phần này có thể được cá nhân hóa cho từng người.

Ví dụ:

* Chủ project có thể có crown.
* Developer chức năng có thể có icon computer.
* Designer có thể có icon paint.
* Các icon nghiêng khoảng 30°.
* Tên có thể được thu gọn.
* Khi hover thì hiển thị tên.

Đây chỉ là ví dụ về ý tưởng.

Có thể customize cách thể hiện tùy ý để tạo cảm giác mỗi thành viên có một identity riêng.

Thông tin như **ảnh và tên của từng người sẽ được tự động update**.

## GitHub

Có một khu vực dành cho **GitHub repository**.

## Supporters

Có một khu vực dành cho supporter.

Cần có:

* Một nơi để người dùng click vào để ủng hộ.
* Danh sách/hiển thị những người đã ủng hộ.

Có thể tham khảo concept của **osu!supporter**:

https://osu.ppy.sh/home/support

## Copyright

Cuối trang hiển thị:

`© 2024 - {current year} Thanh Mai. Released under AGPL-3.0.`

---

# 8. Thay đổi ở khu vực Run / Check

Ở phần giao diện trước đây có **Run** và **Check** thì bỏ hai phần này.

Thay vào đó hiển thị thống kê:

* Số records.
* Số text.
* Số image.

---

# 9. Đa ngôn ngữ / i18n

Application là **đa ngôn ngữ**.

Vì vậy trong UI design **không hard-code text trực tiếp**.

Thay vào đó sử dụng **i18n key**.

Ví dụ:

`recipes.recipe.name`

Tức là trong thiết kế có thể hiển thị key thay cho text thực tế.

Convention có thể tham khảo:

https://www.locize.com/blog/guide-to-i18n-key-naming

Mục tiêu là frontend có thể thay text theo localization system mà không cần thay đổi cấu trúc UI.

Ví dụ thay vì thiết kế:

`Recipe Name`

thì sử dụng:

`recipes.recipe.name`

Khi implementation có translation tương ứng thì key này sẽ được thay bằng text thực tế.

---

# 10. Light / Dark Theme

Application có **Light Mode** và **Dark Mode**.

Khi chuyển giữa hai theme, không đổi theme một cách tức thời.

Cần có **animation chuyển đổi theme**.

Ý tưởng animation mong muốn là kiểu:

**phóng to ra bên ngoài**

Có thể tham khảo transition của **Unsloth**.

Mục tiêu là tạo cảm giác theme đang thực sự chuyển đổi thay vì chỉ đổi toàn bộ màu sắc ngay lập tức.

---

# 11. UI Components

Không cần tự viết tất cả component nhỏ từ đầu.

Có thể sử dụng các UI component library bên ngoài để tránh phải xây dựng lại những component cơ bản.

Ví dụ có thể tham khảo:

### shadcn/ui

https://ui.shadcn.com/docs/figma

### Magic UI

https://magicui.design/

Có thể tham khảo cách **Unsloth sử dụng shadcn/ui kết hợp với Magic UI**.

### Icons

Có thể sử dụng:

**Hugeicons**

https://hugeicons.com/

Khi cần tham khảo một thư viện/component library khác trong Figma, có thể tìm theo:

`{Tên thư viện} + "figma"`

---

# 12. Component Design

Do mục tiêu cuối cùng là frontend Avalonia E2E, các components cần được thiết kế theo hướng có thể tái sử dụng.

Không chỉ thiết kế trạng thái mặc định.

Khi cần thiết, component nên có các state tương ứng như:

* Default.
* Hover.
* Active.
* Selected.
* Disabled.
* Focus.
* Loading.
* Error.

Các state này chỉ cần được thiết kế ở những component thực sự cần.

Không cần cố gắng tạo ra thật nhiều variant nếu UI không sử dụng chúng.

---

# 13. Pages

Các components sau khi thiết kế nên được đặt vào các pages để minh họa giao diện thực tế của application.

Mục đích không phải chỉ để có các component riêng lẻ đẹp mắt, mà cần thể hiện:

* Component hoạt động cùng các component khác như thế nào.
* Khoảng cách và hierarchy giữa các thành phần.
* Layout của page.
* Application shell.
* Toolbar.
* Navigation.
* Theme.
* Animation/interaction cần thiết.

---

# 14. Animation và Interaction

Animation là một phần quan trọng của visual design.

Đặc biệt cần chú ý:

### Logo

`logo → animation → full → giữ`

### Theme transition

Khi đổi Light/Dark mode có animation mở rộng ra bên ngoài.

Ngoài hai animation trên, các animation/transition khác có thể được thiết kế khi cần để UI có cảm giác tự nhiên hơn.

Không cần biến mọi interaction thành animation nếu điều đó không cần thiết.

---

# 15. Design direction

Tổng thể frontend nên giữ một visual language thống nhất.

Các điểm quan trọng nhất:

**Modern + Spacious + Polished**

Có thể lấy cảm hứng từ:

* SlideGenerator v1.
* Unsloth.
* shadcn/ui.
* Magic UI.
* osu! Supporter cho khu vực supporter.

Các reference trên dùng để tham khảo cách tổ chức và cảm giác giao diện, không nhất thiết phải sao chép nguyên bản.

---

# 16. Figma và implementation

Figma được sử dụng để xác định design system, components và page layout.

Tuy nhiên mục tiêu cuối cùng không phải chỉ tạo một file Figma đẹp.

Design cần hướng tới:

**Figma → Avalonia implementation → frontend E2E**

Do đó, trong quá trình thiết kế cần lưu ý rằng:

* Component phải có khả năng triển khai thực tế.
* Layout phải phù hợp với application desktop.
* States cần đủ rõ để implementation.
* Theme cần có cấu trúc rõ ràng.
* Animation cần có cách thể hiện đủ rõ để triển khai.
* Components nên reusable.

---

# 17. Những gì không cần quá cứng nhắc

Đây là ý tưởng tổng thể chứ không phải mọi chi tiết đều đã được quyết định.

Nếu một component có vấn đề, layout không hợp lý hoặc một chi tiết chưa được mô tả cụ thể thì có thể chủ động đưa ra cách thiết kế phù hợp.

Không cần cố gắng bám từng ví dụ một cách máy móc.

Ví dụ crown/computer/paint ở developer section chỉ là cách minh họa cho ý tưởng **cá nhân hóa từng developer**.

---

# 18. Mục tiêu cuối cùng

Mục tiêu là có một frontend SlideGenerator hoàn chỉnh, hiện đại và nhất quán, được triển khai **E2E bằng Avalonia**.

Figma/design cần giúp xác định rõ:

**Design system → Components → Pages → Interactions → Animations → Avalonia frontend**

Project khá phức tạp nên không cần giải quyết tất cả trong một bước.

Điều quan trọng là xây dựng được nền tảng UI đủ tốt để sau đó có thể tiếp tục triển khai logic frontend và backend.

Nếu có điểm nào chưa rõ hoặc cần quyết định thêm, có thể hỏi trực tiếp để thống nhất trước khi triển khai.

---

## Reference

* Unsloth — visual style, spacing, theme transition.
* osu! Supporter — supporter section.
* shadcn/ui — component reference.
* Magic UI — component/animation reference.
* Hugeicons — icon set.
* Locize — i18n key naming convention.

## Tinh thần chung

Project này khá phức tạp, và phần frontend cũng sẽ là một phần lớn của toàn bộ application.

Có thể chủ động sáng tạo trong những phần chưa được quy định cụ thể, nhưng cần giữ đúng những ý tưởng cốt lõi ở trên.

**Cố lên nhé.**
