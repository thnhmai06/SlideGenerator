from PySide6.QtWidgets import QApplication
from UiAPI import UiAPI



# 1. Tạo QApplication trước
app = QApplication([])


# 2. Tạo API và khởi tạo controller
ui = UiAPI()
ui.init_controller()


# 3. Thêm và cập nhật progress. Giao diện xếp từ dưới lên 
for i in range(5):
    ui.addProgress() #  <--- id của thanh tiến trình được sắp xếp theo thứ tự được tạo ra 

ui.updateProgress(0, 10, 'label1')  # <--- ví dụ cập nhật thanh tiến trình có id 0 thành 10% và đặt tên label là label1 
ui.updateProgress(1, 50, 'label2')
ui.updateLog(0, 'example error log \n' * 10)  # <--- ví dụ cập nhật log của thanh tiến trình có id 0 thành 'example error log \n' * 10
ui.updateLog(1, 'example log completed')

# 4. Chạy app
app.exec()


# Xem chi tiết file UiAPI.py


# Vấn đề: Như trên thì ta chỉ có thể cập nhật/thêm các thứ rồi mới có thể khởi chạy ứng dụng. 
# Tức là chưa làm được việc thêm/cập nhật sau khi ứng dụng khởi chạy hay ngay trong quá trình chạy 