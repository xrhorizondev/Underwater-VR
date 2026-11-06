Shader "Underwater/Marine Snow" {
 Properties{_Color("Scatter",Color)=(.5,.8,.8,.3)}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off Cull Off
 Pass{CGPROGRAM
 #pragma multi_compile_instancing
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {UNITY_VERTEX_INPUT_INSTANCE_ID float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {UNITY_VERTEX_OUTPUT_STEREO float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};fixed4 _Color;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float r=length(i.uv-.5)*2;return float4(i.color.rgb,i.color.a*pow(saturate(1-r),3));}
 ENDCG}
 }
}

