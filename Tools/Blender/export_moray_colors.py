import bpy,sys,runpy
from pathlib import Path
folder=Path('D:/GIT Project/Underwater-VR/ArtSource/TripoMoray');bpy.ops.wm.open_mainfile(filepath=str(folder/'TripoMoray.blend'));skin=bpy.data.objects['Supplied moray skin'];print('COLOR_LAYERS',[(a.name,a.domain) for a in skin.data.color_attributes])
for a in list(skin.data.color_attributes):
 if a.name!='Color':skin.data.color_attributes.remove(a)
skin.data.color_attributes.active_color_index=0;skin.data.color_attributes.render_color_index=0
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'TripoMoray.blend'))
rig=bpy.data.objects['TripoMorayRig'];rig.animation_data.action=None
for t in rig.animation_data.nla_tracks:t.mute=False
sys.argv=['export','--','--output',str(folder/'TripoMoray.fbx'),'--collection','TripoMorayAsset','--max-triangles','20000','--max-bones','16','--max-texture','4096','--max-materials','2','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
