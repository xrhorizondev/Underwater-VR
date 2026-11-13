Shader "Underwater/Water Surface From Below" {
 Properties {_Color("Water tint",Color)=(.018,.36,.56,1)}
 SubShader {Tags {"Queue"="Background" "RenderType"="Opaque"} Cull Off ZWrite On
 Pass {CGPROGRAM
 #pragma multi_compile_instancing
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 float4 _Color,_ReefWaterColor;float _ReefTime;
 v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float3 p=v.vertex.xyz;p.y+=sin(p.x*.12+_ReefTime*.5)*.12+sin(p.z*.09-_ReefTime*.4)*.1;o.pos=UnityObjectToClipPos(float4(p,1));o.world=mul(unity_ObjectToWorld,float4(p,1)).xyz;return o;}
 fixed4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=i.world.xz;float t=_ReefTime;
 float2 slope=float2(cos(p.x*.12+t*.5)*.0144,cos(p.y*.09-t*.4)*.009);
 float3 normal=normalize(float3(-slope.x,1,-slope.y));float3 view=normalize(i.world-_WorldSpaceCameraPos);float facing=saturate(dot(view,normal));
 float window=smoothstep(.45,.92,facing);float3 column=lerp(_ReefWaterColor.rgb*float3(.45,.5,.76),_ReefWaterColor.rgb*float3(.8,1.8,1.5),smoothstep(-.55,.55,view.y));float3 col=lerp(column,lerp(_Color.rgb,float3(.10,.48,.63),window),smoothstep(.16,.5,view.y));
 float sun=pow(saturate(dot(view,normalize(float3(.18,.85,.38)+float3(slope.x,0,slope.y)*2))),96);col+=float3(.24,.30,.28)*sun;
 return float4(col,1);}
 ENDCG}
 }
}
