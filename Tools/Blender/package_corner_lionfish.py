import bpy,sys,runpy
from pathlib import Path
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');bpy.ops.wm.open_mainfile(filepath=str(out/'Lionfish_Animated.blend'))
for image in bpy.data.images:
 if image.type=='RENDER_RESULT':continue
 if len(image.pixels)==0:continue
 image.filepath_raw=str(out/(image.name.replace('/','_')+'.png'));image.file_format='PNG';image.save();image.pack()
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.context.scene.objects:
 if obj.type in {'MESH','ARMATURE'}:obj.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Lionfish_Animated.blend'))
sys.argv=['export','--','--output',str(out/'Lionfish_Animated.fbx'),'--max-triangles','500000','--max-bones','8','--max-materials','2','--max-texture','8192','--require-animation'];runpy.run_path(r'C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('LIONFISH_TEXTURE_PACKAGE_READY')
