import os
import shutil

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
            os.remove(path)
        elif os.path.isdir(path):
            shutil.rmtree(path)

def copy_file(from_: str, to_: str):
    """
    Sao chép file từ đường dẫn nguồn đến đường dẫn đích.

    Args:
        from_ (str): Đường dẫn nguồn.
        to_ (str): Đường dẫn đích.

    Returns:
        str: Đường dẫn đích.
    """
    if from_ == to_:
        return
    if os.path.exists(to_):
        os.remove(to_)
    shutil.copyfile(from_, to_)
    return to_