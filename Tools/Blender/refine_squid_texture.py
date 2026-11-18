import bpy,numpy as np
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/SquidPreview'
bpy.ops.wm.open_mainfile(filepath=str(out/'ReferenceSquid.blend'))
n=2048;v,u=np.mgrid[0:n,0:n]/(n-1);rng=np.random.default_rng(731)
m=np.sin(u*73+np.sin(v*53)*2)*np.cos(v*137+np.sin(u*41));grain=rng.random((n,n));spots=np.clip((np.sin(u*907+np.sin(v*311)*2)*np.cos(v*733)-.35)*3,0,1)
dorsal=(np.sin(u*np.pi*2)*.5+.5)**.65
base=np.ones((n,n,4),np.float32);base[:,:,:3]=np.array([.72,.50,.42])*(1-dorsal[:,:,None])+np.array([.36,.11,.075])*dorsal[:,:,None];base[:,:,:3]*=(.87+.15*m+.12*grain)[:,:,None];base[:,:,:3]+=spots[:,:,None]*np.array([.14,.09,.055])
swatches=[(.60,.37,.29),(.10,.30,.32),(.007,.012,.015),(.76,.73,.62),(.95,.98,.99)]
for i,c in enumerate(swatches):base[int(n*950/1024):,int(i*n/5):int((i+1)*n/5),:3]=c
height=np.clip(.5+.12*m+.11*spots+.04*(grain-.5),0,1);height[int(n*950/1024):]=.5
for name,data in [('SquidSkin',base),('SquidHeight',np.dstack([height,height,height,np.ones_like(height)]))]:
 old=bpy.data.images.get(name)
 if old:bpy.data.images.remove(old,do_unlink=True)
 im=bpy.data.images.new(name,width=n,height=n,alpha=True)
 if name.endswith('Height'):im.colorspace_settings.name='Non-Color'
 im.pixels.foreach_set(data.astype(np.float32).ravel());im.filepath_raw=str(out/(name+'.png'));im.file_format='PNG';im.save()
for mat in bpy.data.materials:
 if not mat.use_nodes:continue
 nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF')
 if not bs or 'fin' in mat.name.lower():continue
 for node in nodes:
  if node.type=='TEX_IMAGE' and node.image and 'Skin' in node.image.name:node.image=bpy.data.images['SquidSkin']
 tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['SquidHeight'];bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.5;bump.inputs['Distance'].default_value=.012;mat.node_tree.links.new(tex.outputs['Color'],bump.inputs['Height']);mat.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'ReferenceSquid.blend'))
print('SQUID_DETAIL_TEXTURES_READY 2048 height=true')
