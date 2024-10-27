import os, subprocess

def UIconverter(directory="./gui/ui", output_directory="./gui/"):
    if not os.path.exists(directory):
        print(f"Directory {directory} does not exist.")
    else: 
        #File .ui
        for root, dir, files in os.walk(directory):
            for file in files:
                if file.endswith(".ui"):
                    ui_file = os.path.join(root, file)
                    py_file = os.path.join(output_directory, file.replace(".ui", ".py"))
                    # Lệnh chuyển đổi
                    command = f"pyuic5 -x {ui_file} -o {py_file}"

                    # Chuyển .ui thành .py
                    try:
                        subprocess.run(command, check=True, shell=True)
                        print(f"Converted: {ui_file} -> {py_file}")
                    except subprocess.CalledProcessError as e:
                        print(f"Failed to convert {ui_file}: {e}")

def QRCconverter(directory="./gui/", output_directory="./"):
    #File .qrc
    for root, dir, files in os.walk(directory):
        for file in files:
            if file.endswith(".qrc"):
                qrc_file = os.path.join(root, file)
                py_file = os.path.join(output_directory, file.replace(".qrc", "_rc.py"))
                # Lệnh chuyển đổi
                command = f"pyrcc5 {qrc_file} > {py_file}"

                # Chuyển .qrc thành .py
                try:
                    subprocess.run(command, check=True, shell=True)
                    print(f"Converted: {qrc_file} -> {py_file}")
                except subprocess.CalledProcessError as e:
                    print(f"Failed to convert {qrc_file}: {e}")