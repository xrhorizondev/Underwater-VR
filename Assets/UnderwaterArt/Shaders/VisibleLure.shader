Shader "Underwater/Visible Lure" {
 Properties {_Color("Bioluminescence",Color)=(.3,1.4,1.8,1)}
 SubShader {Tags {"RenderType"="Opaque"}
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 half4 _Color;float _ReefTime;
 struct v2f {float4 pos:SV_POSITION;float pulse:TEXCOORD0;UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;o.pulse=.88+.12*sin(_ReefTime*1.7+world.z);UNITY_TRANSFER_FOG(o,o.pos);return o;}
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half4 c=half4(_Color.rgb*i.pulse,1);UNITY_APPLY_FOG(i.fogCoord,c);return c;}
 ENDCG}
 }
}
