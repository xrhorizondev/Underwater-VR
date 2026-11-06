from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
def gaussian_filter(a,s):
    return np.array(Image.fromarray(np.float32(a),'F').convert('L').filter(ImageFilter.GaussianBlur(s)),dtype=float) if a.max()>1 else np.array(Image.fromarray(np.uint8(a*255)).filter(ImageFilter.GaussianBlur(s)),dtype=float)/255
def nearest(points,x,y):
    first=np.full(x.shape,10.);second=first.copy()
    for p in points:
        dx=np.abs(x-p[0]);dx=np.minimum(dx,1-dx);dy=np.abs(y-p[1]);dy=np.minimum(dy,1-dy);d=np.sqrt(dx*dx+dy*dy)
        second=np.minimum(second,np.maximum(first,d));first=np.minimum(first,d)
    return first,second

out=Path(__file__).resolve().parents[1]/'Assets/UnderwaterArt/Textures'
out.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(731)
n=1024
y,x=np.mgrid[0:n,0:n].astype(float)/n
def save(name,a):
    Image.fromarray(np.uint8(np.clip(a,0,1)*255)).save(out/name)
def normal(name,h,strength=6):
    dy,dx=np.gradient(h);v=np.stack((-dx*strength,-dy*strength,np.ones_like(h)),axis=-1);v/=np.linalg.norm(v,axis=-1)[...,None];save(name,v*.5+.5)
# Periodic cellular caustics with warped sample positions.
pts=rng.random((120,2))
q=np.stack(((x+.025*np.sin(y*30)+.018*np.sin(x*45))%1,(y+.02*np.sin(x*24))%1),axis=-1)
d1,d2=nearest(pts,q[...,0],q[...,1]);edge=d2-d1
c=gaussian_filter(np.exp(-edge*360),.8)
save('Caustics.png',c)
# Small polyp mouths and calcareous tissue. Color variants are material tints.
u=x*45;v=y*45;cx=u-np.floor(u)-.5;cy=v-np.floor(v)-.5
r=np.sqrt(cx*cx+cy*cy);ang=np.arctan2(cy,cx)
polyps=np.exp(-((r-.24)*18)**2)*(.6+.4*np.cos(ang*12))
h=.4+polyps*.26-.25*np.exp(-r*r*130)+rng.random((n,n))*.1
rgb=np.stack((h*.42+.42,h*.4+.4,h*.36+.34),axis=-1)
save('LivingCoral_albedo.png',rgb);normal('LivingCoral_normal.png',h,16)
# Brain coral folded ridges, multi-scale grooves.
wave=np.sin((x+.035*np.sin(y*36)+.024*np.sin(x*21+y*18))*160+np.sin(y*92)*1.3)
h=np.power(wave*.5+.5,2)*.65+rng.random((n,n))*.08
save('BrainCoral_albedo.png',np.stack((.36+h*.4,.38+h*.45,.23+h*.23),axis=-1));normal('BrainCoral_normal.png',h,14)
# Turtle carapace scales, with distinct polygon borders and mottling.
pts=rng.random((65,2));d1,d2=nearest(pts,x,y)
border=np.clip((d2-d1)*170,0,1)
mottle=gaussian_filter(rng.random((n,n)),3);mottle=(mottle-mottle.min())/(mottle.max()-mottle.min())
h=border*(.55+mottle*.2)
save('TurtleShell_albedo.png',np.stack((.1+h*.35,.14+h*.3,.06+h*.16),axis=-1));normal('TurtleShell_normal.png',h,8)
print('Generated caustic, polyp, brain-coral and turtle surface maps')
u=x*80;v=y*35;cx=(u+(np.floor(v)%2)*.5)%1-.5;cy=v%1-.5
scales=np.clip(1-np.exp(-((np.sqrt(cx*cx+cy*cy)-.45)*25)**2)*.25,0,1)
light=.72+.25*(np.sin(y*2*np.pi)*.5+.5)
fish=scales*light*(.96+.04*rng.random((n,n)))
gill=np.exp(-((x-.72)*100)**2)*.14
fish-=gill
save('Fish_albedo.png',np.stack((fish*.94,fish*.98,fish),axis=-1));normal('Fish_normal.png',scales,3)
