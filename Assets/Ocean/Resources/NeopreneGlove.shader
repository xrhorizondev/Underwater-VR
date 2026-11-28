Shader "Underwater/Neoprene Glove" {
 Properties {
  _SkinColor ("Swimmer skin", Color) = (.68,.43,.30,1)
  _PalmColor ("Palm skin", Color) = (.82,.59,.45,1)
 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off Pass { Tags {"LightMode"="ForwardBase"}
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 half4 _SkinColor,_PalmColor;
 struct Output {float4 pos:SV_POSITION;half3 shade:TEXCOORD0;float2 uv:TEXCOORD1;float3 local:TEXCOORD2;half palm:TEXCOORD3;half3 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
 Output vert(appdata_full v){Output o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(Output,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);half3 n=UnityObjectToWorldNormal(v.normal);o.shade=clamp(max(half3(.55,.58,.62),ShadeSH9(half4(n,1)))+_LightColor0.rgb*saturate(dot(n,_WorldSpaceLightPos0.xyz))*.55,.45,1.4);o.uv=v.texcoord.xy;o.local=v.vertex.xyz;o.palm=smoothstep(-.15,.65,-v.normal.y);o.color=lerp(_SkinColor.rgb,_PalmColor.rgb,o.palm);return o;}
 half4 frag(Output i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 pores=i.uv*float2(210,250);float fade=saturate(1-max(fwidth(pores.x),fwidth(pores.y)));float skin=1+.012*fade*sin(pores.x*6.283)*sin(pores.y*6.283);float crease=min(abs(i.local.z+.022-i.local.x*i.local.x*8),abs(i.local.z-.006+i.local.x*.22));float width=max(.0004,fwidth(crease));float palmArea=1-smoothstep(.025,.042,abs(i.local.x));skin*=1-.09*i.palm*palmArea*(1-smoothstep(width,width*2.5,crease));return half4(i.color*i.shade*skin,1);}
 ENDCG
 }}
}
