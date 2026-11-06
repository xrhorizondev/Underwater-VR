import bpy,sys
from pathlib import Path
root=Path('C:/XR Horizon/Projects/Under Water')
turtle='--' in sys.argv and 'turtle' in sys.argv[sys.argv.index('--')+1:]
folder=root/('Builds/RealisticWindows/TurtleMotionFrames' if turtle else 'Builds/RealisticWindows/FishMotionFrames')
files=sorted(folder.glob('frame_*.png'))
assert len(files)==144, f'Expected 144 frames, got {len(files)}'
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
editor=scene.sequence_editor_create()
strip=editor.strips.new_image('Unity fish motion capture',str(files[0]),channel=1,frame_start=1)
for f in files[1:]:strip.elements.append(f.name)
scene.frame_start=1;scene.frame_end=len(files)
scene.render.resolution_x=960;scene.render.resolution_y=540;scene.render.resolution_percentage=100
scene.render.fps=24;scene.render.use_sequencer=True
scene.render.image_settings.file_format='FFMPEG'
scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='HIGH'
scene.render.filepath=str(root/('Renders/Turtle_Motion_Preview.mp4' if turtle else 'Renders/Fish_Motion_Preview.mp4'))
scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
bpy.ops.render.render(animation=True)
print('FISH_VIDEO_ENCODED',scene.render.filepath)
