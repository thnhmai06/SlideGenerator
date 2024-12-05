def delete_all_file(PATH: str):
    import os
    if os.path.exists(PATH):
        for filename in os.listdir(PATH):
            file_path = os.path.join(PATH, filename)
            if os.path.isfile(file_path) or os.path.islink(file_path):
                os.unlink(file_path)
            elif os.path.isdir(file_path):
                os.rmdir(file_path)