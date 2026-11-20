Shader "Underwater/Imported Squid Fin" {
 Properties {_MainTex("Fin atlas",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 sampler2D _MainTex;half4 _Color;float _ReefLight;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;half3 normal:TEXCOORD2;UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;o.normal=UnityObjectToWorldNormal(v.normal);UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);fixed4 c=tex2D(_MainTex,i.uv)*_Color;half3 n=normalize(i.normal);half3 light=max(half3(.045,.065,.085),ShadeSH9(half4(n,1)))+_LightColor0.rgb*(.2+.6*abs(dot(n,_WorldSpaceLightPos0.xyz)))+_ReefLight*half3(.09,.12,.14);c.rgb*=light;c.a*=lerp(1,.72,smoothstep(.65,1,i.uv.x));UNITY_APPLY_FOG(i.fogCoord,c);return c;}
 ENDCG}
 }
}
