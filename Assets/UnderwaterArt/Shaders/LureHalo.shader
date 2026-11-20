Shader "Underwater/Lure Halo" {
 Properties {_Color("Lure glow",Color)=(.3,1.4,1.8,1)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Blend SrcAlpha One ZWrite Off Cull Off
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 half4 _Color;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata_base v){
  v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  float3 center=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
  float2 size=float2(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m01_m11_m21));
  float3 world=center+UNITY_MATRIX_I_V._m00_m10_m20*v.vertex.x*size.x+UNITY_MATRIX_I_V._m01_m11_m21*v.vertex.y*size.y;
  o.pos=UnityWorldToClipPos(world);o.uv=v.texcoord.xy;UNITY_TRANSFER_FOG(o,o.pos);return o;
 }
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float radius=length(i.uv*2-1);half alpha=pow(saturate(1-radius),3)*.45;half4 c=half4(_Color.rgb,alpha);UNITY_APPLY_FOG_COLOR(i.fogCoord,c,half4(0,0,0,0));return c;}
 ENDCG}
 }
}
