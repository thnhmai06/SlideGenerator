#? Các mẫu HTML dùng để thông báo
def successText(success, fails):
    return f'<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Tạo Slide hoàn tất!</span></p><p><span style=" font-size:10pt;">Thành công: {success}/{success+fails}| Thất bại: {fails}/{success+fails}</span></p></body></html>'
noPowerPointText = '<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Vui lòng cài đặt Microsoft PowerPoint (Office) <br/>để sử dụng phần mềm này!</span><br/></p></body></html>'