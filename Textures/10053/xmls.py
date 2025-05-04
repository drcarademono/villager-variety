import os
import xml.etree.ElementTree as ET

def modify_xml(filename):
    tree = ET.parse(filename)
    root = tree.getroot()

    for elem in root.iter():
        if elem.tag in ['scaleX', 'scaleY']:
            value = float(elem.text)
            new_value = value / 1.5
            elem.text = str(new_value)

    tree.write(filename)

for root, dirs, files in os.walk('.'):
    for file in files:
        if file.endswith('.xml'):
            modify_xml(os.path.join(root, file))
