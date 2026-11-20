Shader "Underwater/Tripo Moray PBR" {
 Properties {
  _Color("Tint",Color)=(1,1,1,1)
  _MainTex("Original base color",2D)="white"{}
  [Normal] _BumpMap("Original normal",2D)="bump"{}
  _OrmMap("Original occlusion roughness metallic",2D)="white"{}
  _BumpScale("Normal strength",Range(0,1))=.55
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  #pragma multi_compile_instancing
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap,_OrmMap;fixed4 _Color;half _BumpScale;
  struct Input {float2 uv_MainTex;float4 color:COLOR;};
  void surf(Input i,inout SurfaceOutputStandard o){
   fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;
   half3 orm=tex2D(_OrmMap,i.uv_MainTex).rgb;
   o.Albedo=c.rgb*i.color.rgb;o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_MainTex),_BumpScale);
   o.Occlusion=orm.r;o.Smoothness=saturate(1-orm.g)*.65;o.Metallic=orm.b;o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
