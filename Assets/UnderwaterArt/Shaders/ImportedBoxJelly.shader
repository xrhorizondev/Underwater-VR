Shader "Underwater/Imported Box Jelly" {
 Properties {
  _MainTex("Tissue",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  [Enum(Front,2,Back,1,Both,0)] _Cull("Face orientation",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Cull [_Cull]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO};
   v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);UNITY_TRANSFER_FOG(o,o.pos);return o;}
   fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);fixed4 c=tex2D(_MainTex,i.uv)*_Color;UNITY_APPLY_FOG(i.fogCoord,c);return c;}
   ENDCG
  }
 }
}
