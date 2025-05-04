import os
import re

# Match files that start with a number from 1002 to 1070 followed by an underscore
pattern = re.compile(r"^(10[0-6][0-9]|1070)_(.+)$")

def process_filenames_recursively(root_dir="."):
    for dirpath, dirnames, filenames in os.walk(root_dir):
        for filename in filenames:
            match = pattern.match(filename)
            if match:
                number_str, rest = match.groups()
                number = int(number_str)
                new_number = number + 9000
                new_filename = f"{new_number}_{rest}"
                old_path = os.path.join(dirpath, filename)
                new_path = os.path.join(dirpath, new_filename)
                print(f"Renaming: {old_path} -> {new_path}")
                os.rename(old_path, new_path)

if __name__ == "__main__":
    process_filenames_recursively()

