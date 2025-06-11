@echo off
echo Converting .docx files to .md using Pandoc...

pandoc "v1.2\DroneSim Architecture v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Architecture v1.2.md"
pandoc "v1.2\DroneSim Autopilot v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Autopilot v1.2.md"
pandoc "v1.2\DroneSim Core v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Core v1.2.md"
pandoc "v1.2\DroneSim Flight Dynamic v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Flight Dynamic v1.2.md"
pandoc "v1.2\DroneSim Main Application v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Main Application v1.2.md"
pandoc "v1.2\DroneSim Orchestrator v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Orchestrator v1.2.md"
pandoc "v1.2\DroneSim Physics v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Physics v1.2.md"
pandoc "v1.2\DroneSim Player Input v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Player Input v1.2.md"
pandoc "v1.2\DroneSim Renderer v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Renderer v1.2.md"
pandoc "v1.2\DroneSim Requirements v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Requirements v1.2.md"
pandoc "v1.2\DroneSim Solution Setup v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Solution Setup v1.2.md"
pandoc "v1.2\DroneSim Spawner v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Spawner v1.2.md"
pandoc "v1.2\DroneSim Terrain Gen v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Terrain Gen v1.2.md"
pandoc "v1.2\DroneSim Testing v1.2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.2\DroneSim Testing v1.2.md"
pandoc "v1.3\DroneSim Architecture v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Architecture v1.3.md"
pandoc "v1.3\DroneSim Core v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Core v1.3.md"
pandoc "v1.3\DroneSim Flight Dynamic v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Flight Dynamic v1.3.md"
pandoc "v1.3\DroneSim Main Application v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Main Application v1.3.md"
pandoc "v1.3\DroneSim Orchestrator v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Orchestrator v1.3.md"
pandoc "v1.3\DroneSim Renderer v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Renderer v1.3.md"
pandoc "v1.3\DroneSim Requirements v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Requirements v1.3.md"
pandoc "v1.3\DroneSim Testing v1.3.docx" -f docx -t markdown-simple_tables --wrap=none -o "v1.3\DroneSim Testing v1.3.md"
pandoc "v2\DroneSim Physics v2.docx" -f docx -t markdown-simple_tables --wrap=none -o "v2\DroneSim Physics v2.md"

echo Done.
