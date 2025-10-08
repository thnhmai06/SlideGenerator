# Hướng dẫn sử dụng giao diện

Chương trình sử dụng **thư viện PySide6**. Các file `.ui` được tạo và chỉnh sửa bằng công cụ **Qt6 Designer**.

## 1. Chạy giao diện

Để sử dụng giao diện, chỉ cần import `UiAPI.py` và sử dụng các hàm có sẵn. Dưới đây là ví dụ minh họa trong `test.py` bao gồm các vấn đề chưa giải quyết:


```python
from PySide6.QtWidgets import QApplication
from UiAPI import UiAPI

# 1. Tạo QApplication trước
app = QApplication([])

# 2. Tạo API và khởi tạo controller
ui = UiAPI()
ui.init_controller()

# 3. Thêm và cập nhật progress. Giao diện xếp từ dưới lên 
for i in range(5):
    ui.addProgress()  # <--- id của thanh tiến trình được sắp xếp theo thứ tự tạo ra 

ui.updateProgress(0, 10, 'label1')  # <--- cập nhật thanh tiến trình id 0 thành 10% và đặt label là 'label1'
ui.updateProgress(1, 50, 'label2')
ui.updateLog(0, 'example error log \n' * 10)  # <--- cập nhật log của thanh tiến trình id 0
ui.updateLog(1, 'example log completed')

# Ngoài ra còn có ui.getPaths() để lấy các đường dẫn đến file/folder trong ô input

# 4. Chạy app
app.exec()
#Lưu ý: Như ví dụ trên, hiện tại chỉ có thể thêm/cập nhật các thanh tiến trình và log trước khi ứng dụng chạy. 
#Việc thêm/cập nhật trong khi ứng dụng đang chạy chưa được hỗ trợ.
```

## 2. Quản lý Resource
Các file .qrc nằm trong thư mục Resource là resource file, chứa các mã QSS để style giao diện và các icon. \
Các file này được chỉnh sửa trong Qt6 Designer.

Để sử dụng trong code Python, các file `.qrc` cần biên dịch thành file `.py`. Ví dụ:


#### Trong thư mục Resource
```
pyside6-rcc Main.qrc -o MainResource_rc.py 
pyside6-rcc ProgressBar.qrc -o ProgressBarResource_rc.py 
```
Chú ý: Mỗi khi chỉnh sửa các file resource (QSS, ICON), cần biên dịch lại để các file `.py` được cập nhật.

## 3. File .ui
Các file `.ui` được tạo và chỉnh sửa trực tiếp bằng Qt6 Designer. \
Chúng định nghĩa layout và giao diện của ứng dụng, sau đó được load vào PySide6 thông qua UiAPI.py.