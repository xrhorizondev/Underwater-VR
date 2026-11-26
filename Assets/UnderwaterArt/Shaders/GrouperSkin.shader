Shader "Underwater/Skinned Grouper" {
 Properties {_MainTex("Skin atlas",2D)="white"{} _BumpMap("Scale relief",2D)="bump"{} _Color("Natural coloration",Color)=(.58,.54,.39,1) _SurfaceEyes("Eyes in skin",Float)=0 _EyeParams("Eye Z Y and radii",Vector)=(.38,.04,.025,.025) _EyeIris("Species iris",Color)=(.24,.16,.055,1)}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap;fixed4 _Color;float _ReefLight;
 #include "FishSurfaceEyes.cginc"
 struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 viewDir;float3 body;float sideFacing;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.body=float3(v.vertex.x,v.texcoord2.y,v.texcoord2.x);o.sideFacing=abs(v.normal.x);}
 void surf(Input i,inout SurfaceOutputStandard o){fixed3 c=tex2D(_MainTex,i.uv_MainTex).rgb;float detail=dot(c,float3(.2126,.7152,.0722));float lifted=pow(max(detail,.0001),.62);o.Albedo=lerp(c,lifted*_Color.rgb,.88)*1.1;if(detail<.012)o.Albedo=c;float2 p=i.uv_MainTex;float spots=sin(p.x*93+sin(p.y*83))*sin(p.y*91+p.x*29);o.Albedo*=lerp(1,.42,smoothstep(.5,.85,spots));o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_BumpMap),.4);o.Normal.z*=i.viewDir.z>=0?1:-1;o.Metallic=.015;o.Smoothness=.22;float4 eye=ReefSurfaceEye(i.body,i.sideFacing);o.Albedo=lerp(o.Albedo,eye.rgb,eye.a);o.Smoothness=lerp(o.Smoothness,.72,eye.a);o.Normal=lerp(o.Normal,float3(0,0,1),eye.a);o.Occlusion=1;o.Alpha=1;o.Emission=o.Albedo*float3(.18,.22,.26)*_ReefLight;}
 ENDCG
 } FallBack "Diffuse"
}
