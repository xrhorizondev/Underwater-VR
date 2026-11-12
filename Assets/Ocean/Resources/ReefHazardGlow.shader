Shader "Underwater/Hazard Glow" {
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct input {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct output {float4 position:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 output vert(input v){output o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(output,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
 fixed4 frag(output i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return fixed4(i.color.rgb,i.color.a*smoothstep(0,.5,1-abs(i.uv.y*2-1)));}
 ENDCG}
 }
}
