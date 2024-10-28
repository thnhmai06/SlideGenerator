from src import convertUI
import sys

try:
    convertUI.UIconverter() # Chuyển .ui -> .py
    convertUI.QRCconverter() # Chuyển .qrc -> .py    
except Exception as err: # Báo lỗi, ko chuyển dc
    print("ERR: Khong the chuyen file UI thanh code")
    print(err)
    print('\n')
    input("Press Enter to continue...")
    sys.exit(1)