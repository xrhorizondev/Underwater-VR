Shader "Underwater/Reference Comb Tissue" {
 Properties { _MainTex("Tissue and comb rows",2D)="white"{} }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent"}
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float4 t=tex2D(_MainTex,i.uv);float rim=pow(1-abs(dot(normalize(i.normal),normalize(UnityWorldSpaceViewDir(i.world)))),2.5);float row=saturate((t.a-.17)*2.1);float3 rainbow=.5+.5*cos(i.world.y*23-_Time.y*2+float3(0,2.094,4.189));float wave=.65+.35*sin(i.world.y*75-_Time.y*5);float3 col=t.rgb*.6+rim*float3(.35,.24,.48)+row*lerp(t.rgb*1.9,rainbow,.24)*wave;return float4(col,saturate(.055+t.a*.72+rim*.22));}
 ENDCG
 }
 }
}
