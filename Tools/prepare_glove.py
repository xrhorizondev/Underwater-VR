import numpy as np,json,struct,pathlib
def read(side):
 data=pathlib.Path('Tools/glove-'+side+'.glb').read_bytes();n=struct.unpack_from('<I',data,12)[0];j=json.loads(data[20:20+n]);blob=data[28+n:]
 def acc(i):
  a=j['accessors'][i];b=j['bufferViews'][a['bufferView']];dim={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']];dtype={5121:'u1',5123:'<u2',5125:'<u4',5126:'<f4'}[a['componentType']];return np.frombuffer(blob,dtype,count=a['count']*dim,offset=b.get('byteOffset',0)+a.get('byteOffset',0)).reshape(-1,dim)
 p=j['meshes'][0]['primitives'][0];s=j['skins'][0];bones=[j['nodes'][k]['name'] for k in s['joints']];joints=np.linalg.inv(acc(s['inverseBindMatrices']).reshape(-1,4,4).transpose(0,2,1))[:,:3,3];return p,acc,bones,joints
if __name__=='__main__':
 p,a,b,j=read('left');print(p);print(list(zip(b,j.round(3))));print(a(p['attributes']['POSITION']).min(0),a(p['attributes']['POSITION']).max(0))
def generate(side):
 p,acc,names,joints=read(side);attr=p['attributes'];pos=acc(attr['POSITION']).copy();norm=acc(attr['NORMAL']);weights=acc(attr['WEIGHTS_0']);bones=acc(attr['JOINTS_0']);uv=acc(attr['TEXCOORD_0'])
 sign=1 if side=='left' else -1
 def axes(v):return np.stack([-sign*v[:,2],-sign*v[:,0],-v[:,1]],axis=1)
 wrist=joints[names.index('wrist')]
 pos=axes(pos-wrist);norm=axes(norm);joints=axes(joints-wrist)
 # Wrist sits behind the controller's grip centre.
 pos[:,2]-=.065;joints[:,2]-=.065
 delta=[np.zeros_like(pos),np.zeros_like(pos)]
 for digit in ['index-finger','middle-finger','ring-finger','pinky-finger','thumb']:
  chain=[names.index(digit+'-'+s) for s in ['phalanx-proximal','phalanx-intermediate','phalanx-distal','tip'] if digit+'-'+s in names]
  for k,j in enumerate(chain):
   w=np.sum(weights*(bones==j),axis=1);v=pos.copy()
   for h in reversed(chain[:min(k+1,len(chain)-1)]):
    # Rotate around each preceding knuckle in local hand space.
    a=.56 if digit=='thumb' else .76;c,s=np.cos(a),np.sin(a);v-=joints[h];y=v[:,1].copy();z=v[:,2].copy();v[:,1]=c*y-s*z;v[:,2]=s*y+c*z;v+=joints[h]
   delta[0 if digit=='index-finger' else 1]+=(v-pos)*w[:,None]
 sidewall=np.clip((abs(norm[:,0])-.42)*3,0,1)*np.clip((pos[:,2]+.075)*80,0,1)
 colors=np.array([.027,.029,.032])+(np.array([.035,.12,.52])-np.array([.027,.029,.032]))*sidewall[:,None]
 indices=acc(p['indices']).reshape(-1,3).astype(int)
 # Use authored outward normals to set Unity winding.
 a,b,c=pos[indices[0]]; 
 if np.dot(np.cross(b-a,c-a),norm[indices[0,0]])<0:indices=indices[:,::-1]
 rows=np.concatenate([pos,norm,uv,colors,*delta],axis=1)
 # Extend the original wrist into a bound neoprene cuff.
 start=len(rows);cuff=[]
 for z,rx,ry in [(-.137,.034,.023),(-.134,.035,.024),(-.129,.034,.023),(-.105,.033,.022),(-.10,.034,.023),(-.095,.033,.022),(-.073,.031,.021),(-.065,.032,.021)]:
  for k in range(32):
   angle=2*np.pi*k/32;x,y=np.cos(angle),np.sin(angle);col=[.03,.10,.42] if x*sign<-.6 and z>-.129 else [.018,.02,.022]
   cuff.append([x*rx,y*ry,z,x,y,0,k/32,(z+.14)*12,*col,*([0]*6)])
 tris=[]
 for r in range(7):
  for k in range(32):
   a=start+r*32+k;b=start+r*32+(k+1)%32;tris.extend([[a,b,a+32],[b,b+32,a+32]])
 rows=np.vstack([rows,cuff]);indices=np.vstack([indices,tris])
 target=pathlib.Path('Assets/Ocean/Resources')/('Glove'+side.title()+'.bytes')
 target.write_bytes(struct.pack('<ii',len(rows),indices.size)+rows.astype('<f4').tobytes()+indices.astype('<i4').tobytes())
 print(target,len(rows),len(indices))
for side in ['left','right']:generate(side)
