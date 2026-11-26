Shader "Underwater/Instanced Marine" {
 Properties {
  _MainTex("Albedo",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _TailPhase("Tail phase",Float)=0
  _TailAmplitude("Tail amplitude",Float)=0
  _TurnBend("Turn bend",Float)=0
  _Copperband("Copperband natural color correction",Float)=0
  _BodyGloss("Scale sheen",Range(0,1))=.06
  _EyeGloss("Pupil gloss",Range(0,1))=.45
  _FillLight("Underwater visibility floor",Range(0,1))=0
  _SurfaceEyes("Eyes in skin",Float)=0 _EyeParams("Eye Z Y and radii",Vector)=(.38,.04,.025,.025) _EyeIris("Species iris",Color)=(.24,.16,.055,1)
  [Enum(Front,2,Back,1,Both,0)] _Cull("Cull",Float)=0
 }
 SubShader {
  Tags {"RenderType"="Opaque"} Cull [_Cull]
  Pass {
   Tags {"LightMode"="ForwardBase"}
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;float _ReefLight,_Copperband,_BodyGloss,_EyeGloss,_FillLight;
   #include "FishSurfaceEyes.cginc"
   UNITY_INSTANCING_BUFFER_START(Fish)
    UNITY_DEFINE_INSTANCED_PROP(float,_TailPhase)
    UNITY_DEFINE_INSTANCED_PROP(float,_TailAmplitude)
    UNITY_DEFINE_INSTANCED_PROP(float,_TurnBend)
   UNITY_INSTANCING_BUFFER_END(Fish)
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed3 light:TEXCOORD1;UNITY_FOG_COORDS(2) float3 body:TEXCOORD3;float glint:TEXCOORD4;float sideFacing:TEXCOORD5;UNITY_VERTEX_OUTPUT_STEREO};
   v2f vert(appdata v){
    UNITY_SETUP_INSTANCE_ID(v);v2f o;UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.body=v.vertex.xyz;o.sideFacing=abs(v.normal.x);
    float tail=saturate((.28-v.vertex.z)/.78);float phase=UNITY_ACCESS_INSTANCED_PROP(Fish,_TailPhase)+tail*3.4;
    float amplitude=UNITY_ACCESS_INSTANCED_PROP(Fish,_TailAmplitude);float bend=UNITY_ACCESS_INSTANCED_PROP(Fish,_TurnBend);
    v.vertex.x+=sin(phase)*tail*tail*amplitude+tail*tail*bend;
    float mid=smoothstep(.02,.18,tail)*(1-smoothstep(.65,1,tail));v.vertex.x+=sin(phase-.45)*mid*amplitude*.12;
    float fin=smoothstep(.065,.11,abs(v.vertex.x))*(1-smoothstep(.12,.25,abs(v.vertex.z)))*smoothstep(-.12,-.04,v.vertex.y)*(1-smoothstep(.08,.16,v.vertex.y));v.vertex.y+=sin(phase*.75)*fin*amplitude*.2;
    v.normal.z-=-(2*tail*sin(phase)+3.4*tail*tail*cos(phase))*amplitude/.78;
    o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);
    float3 normal=UnityObjectToWorldNormal(normalize(v.normal));float diffuse=saturate(dot(normal,_WorldSpaceLightPos0.xyz));
    o.light=UNITY_LIGHTMODEL_AMBIENT.rgb+_LightColor0.rgb*(.10+.65*diffuse)+float3(.12,.16,.2)*_ReefLight;
    o.light=max(o.light,_FillLight*float3(.85,.95,1));
    float3 viewer=normalize(_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz);o.glint=pow(saturate(dot(normal,normalize(viewer+_WorldSpaceLightPos0.xyz))),32);
    UNITY_TRANSFER_FOG(o,o.pos);return o;
   }
   fixed4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);fixed4 c=tex2D(_MainTex,i.uv)*_Color;
    if(_Copperband>.5){float blue=smoothstep(.015,.12,c.b-max(c.r,c.g*.85));float yellow=smoothstep(.05,.25,min(c.r,c.g)-c.b);float silver=dot(c.rgb,float3(.25,.45,.3));c.rgb=lerp(c.rgb,silver.xxx,blue);c.rgb=lerp(c.rgb,c.rgb*float3(1,.56,.22),yellow);}
    float4 pattern=ReefSurfaceEye(i.body,i.sideFacing);c.rgb=lerp(c.rgb,pattern.rgb,pattern.a);
    float dark=1-smoothstep(.025,.16,dot(c.rgb,float3(.21,.72,.07)));float eye=dark*smoothstep(.12,.24,i.body.z)*smoothstep(-.005,.035,i.body.y);
    c.rgb=c.rgb*i.light+i.glint*lerp(_BodyGloss,_EyeGloss,eye)*_ReefLight;c.a=1;UNITY_APPLY_FOG(i.fogCoord,c);return c;}
   ENDCG
  }
 }
 FallBack "Mobile/Diffuse"
}
