import os

def delete_file(path: str):
    """
    Xóa tất cả các file trong thư mục hoặc xóa file nếu là file.
    Args:
        path (str): Đường dẫn đến file hoặc thư mục.
    Returns:
        None
    """
    if os.path.exists(path):
        if os.path.isfile(path) or os.path.islink(path):
            os.unlink(path)
        elif os.path.isdir(path):
            for filename in os.listdir(path):
                file_path = os.path.join(path, filename)
                if os.path.isfile(file_path) or os.path.islink(file_path):
                    os.unlink(file_path)
                elif os.path.isdir(file_path):
                    os.rmdir(file_path)

def copy_file(from_: str, to_: str):
    import shutil

    if from_ == to_:
        return
    if os.path.exists(to_):
        os.remove(to_)
    shutil.copyfile(from_, to_)
    return to_