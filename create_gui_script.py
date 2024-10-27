import os, subprocess

ui_directory="./gui/ui"
qrc_directory="./gui/"
ui_output_directory="./gui/"
qrc_output_directory="./"

if not os.path.exists(ui_directory):
    print(f"Directory {ui_directory} does not exist.")
else: 
    #File .ui
    for root, dir, files in os.walk(ui_directory):
        for file in files:
            if file.endswith(".ui"):
                ui_file = os.path.join(root, file)
                py_file = os.path.join(ui_output_directory, file.replace(".ui", ".py"))
                # Lệnh chuyển đổi
                command = f"pyuic5 -x {ui_file} -o {py_file}"

                # Chuyển .ui thành .py
                try:
                    subprocess.run(command, check=True, shell=True)
                    print(f"Converted: {ui_file} -> {py_file}")
                except subprocess.CalledProcessError as e:
                    print(f"Failed to convert {ui_file}: {e}")

    #File .qrc
    for root, dir, files in os.walk(qrc_directory):
        for file in files:
            if file.endswith(".qrc"):
                qrc_file = os.path.join(root, file)
                py_file = os.path.join(qrc_output_directory, file.replace(".qrc", "_rc.py"))
                # Lệnh chuyển đổi
                command = f"pyrcc5 {qrc_file} > {py_file}"

                # Chuyển .ui thành .py
                try:
                    subprocess.run(command, check=True, shell=True)
                    print(f"Converted: {qrc_file} -> {py_file}")
                except subprocess.CalledProcessError as e:
                    print(f"Failed to convert {qrc_file}: {e}")
print('\n')

#Tạm dừng chương trình
input("Press Enter to continue...")