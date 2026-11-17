Shader "Underwater/Imported Squid Fin" {
 Properties {_MainTex("Fin atlas",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 sampler2D _MainTex;half4 _Color;float _ReefLight;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);fixed4 c=tex2D(_MainTex,i.uv)*_Color;c.rgb*=lerp(half3(.10,.15,.22),half3(.65,.8,.9),_ReefLight);UNITY_APPLY_FOG(i.fogCoord,c);return c;}
 ENDCG}
 }
}
