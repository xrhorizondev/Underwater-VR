from pathlib import Path
p=Path('D:/GIT Project/Underwater-VR/ArtSource/MarineRefresh/polish.py').read_text()
exec(p.split('# Preserve the original shark')[0]+p[p.index('# Seal duplicate UV-boundary'):])
