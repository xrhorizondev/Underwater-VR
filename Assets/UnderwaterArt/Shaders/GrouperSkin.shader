Shader "Underwater/Skinned Grouper" {
 Properties {_MainTex("Skin atlas",2D)="white"{} _BumpMap("Scale relief",2D)="bump"{} _Color("Natural coloration",Color)=(.58,.54,.39,1)}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap;fixed4 _Color;float _ReefLight;
 struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 viewDir;};
 void surf(Input i,inout SurfaceOutputStandard o){fixed3 c=tex2D(_MainTex,i.uv_MainTex).rgb;float detail=dot(c,float3(.2126,.7152,.0722));float lifted=pow(max(detail,.0001),.62);o.Albedo=lerp(c,lifted*_Color.rgb,.88)*1.1;if(detail<.012)o.Albedo=c;float2 p=i.uv_MainTex;float spots=sin(p.x*93+sin(p.y*83))*sin(p.y*91+p.x*29);o.Albedo*=lerp(1,.42,smoothstep(.5,.85,spots));o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_BumpMap),.4);o.Normal.z*=i.viewDir.z>=0?1:-1;o.Metallic=.015;o.Smoothness=.22;o.Occlusion=1;o.Alpha=1;o.Emission=o.Albedo*float3(.18,.22,.26)*_ReefLight;}
 ENDCG
 } FallBack "Diffuse"
}
