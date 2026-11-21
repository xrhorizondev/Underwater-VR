from pathlib import Path
p=Path('D:/GIT Project/Underwater-VR/ArtSource/MarineRefresh/polish.py').read_text();start=p.index('# Lionfish is an unlit');end=p.index('# Seal duplicate');exec(p.split('# Preserve the original shark')[0]+p[start:end].replace("export('Lionfish',12500)","export('Lionfish',20500)"))
