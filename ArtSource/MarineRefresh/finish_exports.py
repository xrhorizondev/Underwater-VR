import bpy,sys,runpy,re
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR')
for kind in ['Dolphin','CombJelly']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/MarineRefresh'/(kind+'.blend')));out=root/'Assets/UnderwaterArt/MarineRefresh'/kind
 for m in bpy.data.materials:
  if not m.node_tree:continue
  for n in m.node_tree.nodes:
   if n.type=='TEX_IMAGE' and n.image and max(n.image.size)>1024:
    im=n.image;im.scale(1024,1024);im.filepath_raw=str(out/'textures'/(re.sub('[^a-zA-Z0-9]+','_',im.name)+'_Auxiliary.png'));im.file_format='PNG';im.save();im.pack()
 bpy.ops.object.select_all(action='SELECT');bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/MarineRefresh'/(kind+'.blend')))
 sys.argv=['export','--','--output',str(out/(kind+'.fbx')),'--max-triangles','8500' if kind=='Dolphin' else '6500','--max-bones','48','--max-materials','3','--max-texture','1024','--require-animation']
 runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
