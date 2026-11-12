Shader "Underwater/Contact Edge" {
 Properties {_Intensity("Intensity",Range(0,1))=0}
 SubShader {Tags {"Queue"="Overlay" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 float _Intensity;
 struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct output {float4 position:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 output vert(input v){output o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(output,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=float4(v.vertex.xy,UNITY_NEAR_CLIP_VALUE,1);o.uv=v.uv;return o;}
 fixed4 frag(output i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 edge=abs(i.uv*2-1);return fixed4(.9,.025,.015,_Intensity*smoothstep(.65,.96,max(edge.x,edge.y)));}
 ENDCG}
 }
}
