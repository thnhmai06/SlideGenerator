# backend_demo.py
import sys
import random
from PySide6.QtWidgets import QProgressBar
from PySide6.QtCore import QObject, Signal, QThread
import test  # import test.py của bạn

NUM_THREADS = 5  # số progressbar demo

# -----------------------------
# Worker thread
# -----------------------------
class Worker(QObject):
    progress = Signal(int)  # gửi tiến trình 0-100
    finished = Signal()     # báo khi kết thúc

    def __init__(self, name: str):
        super().__init__()
        self.name = name

    def run(self):
        import time
        total = 100
        for i in range(total + 1):
            self.progress.emit(i)
            # sleep ngẫu nhiên giữa 0.02 – 0.1s để tiến trình không đồng bộ
            time.sleep(random.uniform(0.02, 0.1))
        print(f"[{self.name}] Hoàn thành")
        self.finished.emit()

# -----------------------------
# Tạo progressbar + thread
# -----------------------------
def create_worker_bar(manager: test.ProgressManager, name: str):
    # Tạo progressbar **trong GUI thread**
# Thay trong create_worker_bar
    bar_widget = manager.add_progress_bar()
    bar = bar_widget.progressBar  # trực tiếp

    if not bar:
        print(f"[{name}] Không tìm thấy progressBar trong widget")
        return None, None

    # Tạo worker và thread
    thread = QThread()
    worker = Worker(name)
    worker.moveToThread(thread)

    # Kết nối signals
    worker.progress.connect(bar.setValue)
    worker.finished.connect(thread.quit)
    worker.finished.connect(worker.deleteLater)
    thread.finished.connect(thread.deleteLater)

    # Khởi động worker khi thread bắt đầu
    thread.started.connect(worker.run)

    return worker, thread

# -----------------------------
# Main
# -----------------------------
def main():
    app, window, manager = test.start_ui()

    threads = []
    for i in range(NUM_THREADS):
        name = f"Thread-{i+1}"
        worker, thread = create_worker_bar(manager, name)
        if worker and thread:
            thread.start()
            threads.append(thread)

    sys.exit(app.exec())

if __name__ == "__main__":
    main()
