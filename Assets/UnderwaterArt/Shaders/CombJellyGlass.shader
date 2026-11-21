Shader "Underwater/Comb Jelly Glass" {
 Properties {
  _MainTex("Transparent tissue RGBA",2D)="white"{}
  _Color("Tissue tint and opacity",Color)=(.68,.86,.95,.8)
  _RowStrength("Comb row shimmer",Range(0,1))=.6
  _CombPhase("Comb phase",Float)=0
  [Enum(Front,2,Back,1,Both,0)] _Cull("Face orientation",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull [_Cull]
  Pass {
   Tags {"LightMode"="ForwardBase"}
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;half _RowStrength;float _ReefTime,_CombPhase;
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;half3 normal:TEXCOORD2;half3 tissue:TEXCOORD3;UNITY_FOG_COORDS(4) UNITY_VERTEX_OUTPUT_STEREO};
   v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.tissue=v.color.rgb;UNITY_TRANSFER_FOG(o,o.pos);return o;}
   fixed4 frag(v2f i):SV_Target{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    fixed4 tex=tex2D(_MainTex,i.uv);half3 n=normalize(i.normal);half3 view=normalize(UnityWorldSpaceViewDir(i.world));half fresnel=pow(1-abs(dot(n,view)),3);
    half rows=pow(saturate((tex.a-.10)/.65),1.3);half height=pow(saturate(i.tissue.g),2.2);half angle=pow(saturate(i.tissue.b),2.2);half wave=.5+.5*sin(height*42-_ReefTime*3+_CombPhase);
    half3 rainbow=.5+.5*cos(6.28318*(height*.8+angle*.3-_ReefTime*.08)+half3(0,2.094,4.189));
    half3 light=normalize(_WorldSpaceLightPos0.xyz);half spec=pow(saturate(abs(dot(n,normalize(light+view)))),64);
    half3 color=lerp(half3(1,1,1),tex.rgb,.08)*_Color.rgb*(.32+fresnel*.35)+rows*_RowStrength*.75*lerp(half3(.65,.82,.9),rainbow,.65)*(.35+.35*wave)+spec*_LightColor0.rgb*.22;
    half alpha=saturate((tex.a*.35+fresnel*.24+rows*.23)*_Color.a);
    fixed4 c=fixed4(color,alpha);UNITY_APPLY_FOG(i.fogCoord,c);return c;
   }
   ENDCG
  }
 }
}
