import os

#? Các mẫu HTML dùng để thông báo
def report(success, fails):
    return f'<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Tạo Slide hoàn tất!</span></p><p><span style=" font-size:10pt;">Thành công: {success}/{success+fails}| Thất bại: {fails}/{success+fails}</span></p></body></html>'
noPowerPoint = '<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Vui lòng cài đặt Microsoft PowerPoint (Office) <br/>để sử dụng phần mềm này!</span><br/></p></body></html>'
noSavePath = '<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Vui lòng chọn vị trí lưu!</span><br/></p></body></html>'

invaidTemplate = 'Template không hợp lệ!'
invaidCSV = 'Danh sách sinh viên không hợp lệ!'
def notAImageFile(path):
    fileName = os.path.basename(path)
    return f'File không phải là ảnh: {fileName}'

def notAImageURL(url):
    return f'URL không phải là ảnh: {url}'

def notExistFile(path):
    filename = os.path.basename(path)
    return f'File không tồn tại: {filename}'

def cannotConnectToURL(url):
    return f'Không thể kết nối đến: {url}'

def cantSaveFile(path):
    filename = os.path.basename(path)
    return f'Không thể lưu file: {filename}'