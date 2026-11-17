Shader "Underwater/Imported Squid Skin" {
 Properties {_MainTex("Skin atlas",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader {Tags {"RenderType"="Opaque"} Cull Back
 Pass {Tags {"LightMode"="ForwardBase"} CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 sampler2D _MainTex;half4 _Color;float _ReefLight;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;half3 light:TEXCOORD1;UNITY_FOG_COORDS(2) UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;half3 n=UnityObjectToWorldNormal(v.normal);o.light=max(half3(.045,.065,.085),ShadeSH9(half4(n,1)))+_LightColor0.rgb*saturate(dot(n,_WorldSpaceLightPos0.xyz))*.8+_ReefLight*half3(.09,.12,.14);UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);fixed4 c=tex2D(_MainTex,i.uv)*_Color;c.rgb*=i.light;c.a=1;UNITY_APPLY_FOG(i.fogCoord,c);return c;}
 ENDCG}
 }
}
