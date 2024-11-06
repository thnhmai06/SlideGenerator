import os
import pandas as pd

placeholders = []
shapesPath = "./temp/shapes/"

# A Function Load fields from csv file and put it in placeholders using pandas
def importCSVFields(csv_path: str) -> None:
    # @params: None
    df = pd.read_csv(csv_path)
    fields = df.columns.tolist()
    for field in fields:
        placeholders.append(field)

def importTemplate():
    if not os.path.exists(shapesPath):
        os.makedirs(shapesPath)

    # TODO: Download all shapes to shapesPath here