Shader "Underwater/Translucent Jelly Tissue" {
 Properties{_Color("Transmitted tissue",Color)=(.5,.72,.8,.26) _RimColor("Scattered light",Color)=(.18,.48,.6,1) _TissueOffset("Part origin",Vector)=(0,0,0,0) _JellyPhase("Pulse phase",Float)=0}
 SubShader {Tags{"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off
 CGPROGRAM
 #pragma surface surf Standard alpha:fade vertex:vert
 #pragma target 3.0
 float4 _Color,_RimColor,_TissueOffset;float _ReefTime,_JellyPhase;
 struct Input {float3 viewDir;float3 worldPos;float2 uv_MainTex;};
 void vert(inout appdata_full v){float pulse=sin(_ReefTime*1.4+_JellyPhase);float3 p=v.vertex.xyz+_TissueOffset.xyz;p.xz*=1+pulse*.035;p.y*=1-pulse*.06;v.vertex.xyz=p-_TissueOffset.xyz;}
 void surf(Input IN,inout SurfaceOutputStandard o){float rim=pow(1-saturate(abs(dot(normalize(IN.viewDir),float3(0,0,1)))),2);float stripes=pow(saturate(sin(IN.uv_MainTex.x*6.283*32)),12);o.Albedo=_Color.rgb*.4;o.Smoothness=.86;o.Metallic=0;o.Emission=_RimColor.rgb*(.2+rim*.55+stripes*.055);o.Alpha=_Color.a+rim*.38;}
 ENDCG
 }
 FallBack "Transparent/Diffuse"
}
