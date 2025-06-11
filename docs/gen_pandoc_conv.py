from pathlib import Path

# Set your root folder containing DOCX files
root_folder = Path(r".")  # <--- UPDATE THIS PATH

# Set output batch file path
batch_file_path = root_folder / "convert_to_md.bat"

with batch_file_path.open("w", encoding="utf-8") as batch:
    batch.write("@echo off\n")
    batch.write("echo Converting .docx files to .md using Pandoc...\n\n")

    for docx_path in root_folder.rglob("*.docx"):
        md_path = docx_path.with_suffix(".md")
        cmd = f'pandoc "{docx_path}" -f docx -t markdown-simple_tables --wrap=none -o "{md_path}"\n'
        batch.write(cmd)

    batch.write("\necho Done.\n")
