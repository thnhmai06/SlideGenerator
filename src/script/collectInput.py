import os, requests, re
from src.context import infoContext
from src.script import showUI

# Thank you github copilot for helping me coding this file hehe :D

urlRegex = r'https?://(?:[-\w.]|(?:%[\da-fA-F]{2}))+'
templateImageDir = "./images/template"
replaceImageDir = "./images/replace"

class ClientInput:
    def __init__(self):
        self.templatePath = ""
        self.csvPath = ""
        self.savePath = ""
        self.config = {
            "text": [],
            "image": []
        }

# save image from response to savePath
def saveImage(response, savePath):
    with open(savePath, 'wb') as file:
        file.write(response.content)
def isImage(imagePath):
    return imagePath.endswith(('.png', '.jpg', '.jpeg', '.gif', '.bmp', '.webp'))

def getClientInput(ui):
    input = ClientInput()
    input.templatePath = ui.template_path.text()
    input.csvPath = ui.csv_path.text()
    input.savePath = ui.save_path.text()

    # Get the text configurations
    for index in range(ui.config_text_list.count()):
        input.config["text"].append(ui.config_text_list.item(index).text())

    # Get the image configurations
    for row in range(ui.config_image_table.rowCount()):
        placeholder = ui.config_image_table.item(row, 0).text()
        imagePath = ui.config_image_table.item(row, 1).text()
        input.config["image"].append({"placeholder": placeholder, "imagePath": imagePath, "isUrl": re.match(urlRegex, imagePath)})
    return input

# Check the input is all valid
def checkValidateInputandSaveImages(input):
    if not os.path.exists(input.templatePath) and not input.templatePath.endswith('.pptx'):
        print("Template file is not invaid: ", input.templatePath)
        showUI.showError(infoContext.invaidTemplate)
        return False
    if not os.path.exists(input.csvPath):
        print("CSV is not invaid: ", input.csvPath)
        showUI.showError(infoContext.invaidCSV)
        return False
    
    # also check if input.config.image is a valid image file if it is not a URL
    for config in input.config.image:
        if not config["isUrl"] and not os.path.exists(config["imagePath"]):
            print(f"Image file {config['imagePath']} does not exist")
            showUI.showError(infoContext.notExistFile(config["imagePath"]))
            return False
        if os.path.exists(config["imagePath"]) and not isImage(config["imagePath"]):
            print(f"File {config['imagePath']} is not an image file")
            showUI.showError(infoContext.notAImageFile(config["imagePath"]))
            return False

        # and check if the URL is a image file url
        if config["isUrl"]:
            try:
                response = requests.head(config["imagePath"])
            except:
                print(f"Cannot connect to: {config['imagePath']}")
                showUI.showError(infoContext.cannotConnectToURL(config["imagePath"]))
                return False
            if not response.headers['Content-Type'].startswith('image'):
                print(f"URL {config['imagePath']} is not an image file")
                showUI.showError(infoContext.notAImageURL(config["imagePath"]))
                return False
            
            # and if the URL is valid, save the image and save it to templateImageDir directory with the same name
            saveImage(response, os.path.join(templateImageDir, os.path.basename(config["imagePath"])))
    return True

def Collect(ui):
    input = getClientInput(ui)
    if not checkValidateInputandSaveImages(input):
        return False
    return True
