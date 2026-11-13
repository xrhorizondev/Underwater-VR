Shader "Underwater/Octopus Skin" {
 Properties {_MainTex("Skin palette",2D)="white"{} _Imported("Imported anatomy",Float)=0 _Motion("Arm response",Float)=0 _Phase("Arm phase",Float)=0 _Color("Skin tint",Color)=(.45,.29,.19,1)}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
 #pragma target 3.0
 float4 _Color;float _Motion,_Phase,_ReefLight;
 struct Input {float3 body;float3 localNormal;float eyePart;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.body=v.vertex.xyz;o.localNormal=v.normal;o.eyePart=v.texcoord1.x;float r=length(v.vertex.xz);float arm=smoothstep(.3,.8,r)*(1-smoothstep(.35,.55,v.vertex.y));float angle=atan2(v.vertex.x,v.vertex.z);v.vertex.y+=sin(_Phase*2.3-r*4+angle*3)*arm*(.022+.05*_Motion);v.vertex.xz*=1+sin(_Phase*2-r*3)*arm*.025*_Motion;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+float3(1,1,1)),f.x),f.y),f.z);}
 void surf(Input i,inout SurfaceOutputStandard o){float mottles=noise(i.body*19)*.65+noise(i.body*47)*.35;float pores=noise(i.body*230);float underside=1-smoothstep(-.65,.05,i.localNormal.y);o.Albedo=lerp(_Color.rgb*(.58+.72*mottles),float3(.57,.43,.32),underside*.6);o.Albedo*=.98+.04*pores;o.Smoothness=.27+pores*.13;o.Metallic=0;o.Alpha=1;float poreX=noise(i.body*230+float3(.4,0,0))-pores;float poreY=noise(i.body*230+float3(0,.4,0))-pores;o.Normal=normalize(float3(poreX*.055,poreY*.055,1));if(i.eyePart>.5&&i.eyePart<2.5){o.Albedo=i.eyePart<1.5?float3(.18,.12,.035):float3(.003,.005,.006);o.Normal=float3(0,0,1);o.Smoothness=.85;}if(i.eyePart>2.5){o.Albedo=float3(.58,.43,.30);o.Smoothness=.32;}o.Emission=o.Albedo*float3(.05,.065,.075)*_ReefLight;}
 ENDCG
 } FallBack "Standard"
}
