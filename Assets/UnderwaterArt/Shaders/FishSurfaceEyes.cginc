#ifndef REEF_SURFACE_EYES
#define REEF_SURFACE_EYES
float _SurfaceEyes;float4 _EyeParams,_EyeIris;
// Bilateral skin pattern in anatomical head space, independent of mesh LOD and UV seams.
float4 ReefSurfaceEye(float3 body,float sideFacing){
 float2 p=(body.zy-_EyeParams.xy)/max(_EyeParams.zw,float2(.001,.001));float r=length(p);
 float edge=max(.025,fwidth(r));float alpha=(1-smoothstep(1-edge,1+edge,r))*smoothstep(.25,.65,sideFacing)*_SurfaceEyes;
 float pupil=1-smoothstep(.48-edge,.48+edge,r);float rim=smoothstep(.80,.98,r);
 float radial=.86+.14*sin(atan2(p.y,p.x)*36+r*17);float3 iris=_EyeIris.rgb*radial;
 float3 color=lerp(iris,float3(.018,.015,.012),rim);color=lerp(color,float3(.003,.005,.008),pupil);
 float glint=1-smoothstep(.07,.16,length(p-float2(-.19,.23)));color=lerp(color,float3(.65,.76,.8),glint*.8);
 return float4(color,alpha);
}
#endif
