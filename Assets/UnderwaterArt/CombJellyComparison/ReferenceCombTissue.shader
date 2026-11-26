Shader "Underwater/Reference Comb Tissue" {
 Properties { _MainTex("Tissue and comb rows",2D)="white"{} _CombTime("Comb clock",Float)=0 _CombPhase("Individual phase",Float)=0 _UseCombCoordinates("Anatomical row coordinates",Float)=0 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent"}
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 sampler2D _MainTex;float _CombTime,_CombPhase,_UseCombCoordinates;
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 anatomy:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;float height:TEXCOORD4;UNITY_FOG_COORDS(3) UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.height=lerp(o.world.y,v.anatomy.x,_UseCombCoordinates);UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float4 t=tex2D(_MainTex,i.uv);float rim=pow(1-abs(dot(normalize(i.normal),normalize(UnityWorldSpaceViewDir(i.world)))),2.5);float row=saturate((t.a-.17)*2.1);float3 rainbow=.5+.5*cos(i.height*18-_CombTime*2.6179939+_CombPhase+float3(0,2.094,4.189));float phase=i.height*35-_CombTime*6.5449847+_CombPhase;float contrast=saturate(1-fwidth(i.height)*35);float wave=.20+.80*lerp(.2734375,pow(.5+.5*cos(phase),4),contrast);float3 col=t.rgb*.6+rim*float3(.35,.24,.48)+row*(t.rgb*.45+rainbow*wave*.9);float4 color=float4(col,saturate(.055+t.a*.72+rim*.22));UNITY_APPLY_FOG(i.fogCoord,color);return color;}
 ENDCG
 }
 }
}
