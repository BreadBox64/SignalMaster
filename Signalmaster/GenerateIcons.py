# E:\User\Programs\Inkscape\bin\inkscape.exe "IconSources/$fileName.svg" --export-type=png --export-filename="Content/$fileName.png"

import subprocess, os

path = os.path.realpath(os.path.join(os.path.realpath(__file__), ".."))
for file in os.listdir(path + "/IconSources"):
	subprocess.run([f"E:\User\Programs\Inkscape/bin/inkscape.com {path}/IconSources/{file[:-4]}.svg --export-type=png --export-filename={path}/Content/{file[:-4]}.png"])
	print(file[:-4])