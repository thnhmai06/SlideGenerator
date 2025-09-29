#!/bin/bash
# Script build resource PySide6

# Nạp conda
source /home/van/Applications/Linux/Miniconda/etc/profile.d/conda.sh

# Kích hoạt môi trường
conda activate qt_py311

# Biên dịch resource
pyside6-rcc Main.qrc -o MainResource_rc.py
pyside6-rcc ProgressBar.qrc -o ProgressBarResource_rc.py