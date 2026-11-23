Shader "Underwater/Reference Lionfish" {
 Properties {
  _MainTex("Reference atlas",2D)="white"{}
  [Toggle] _Cutout("Fin membrane",Float)=0
  _Cutoff("Fin cutoff",Range(0,1))=.25
  [Enum(Off,0,Front,1,Back,2)] _Cull("Cull",Float)=2
 }
 SubShader {
  Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull [_Cull] ZWrite On
  CGPROGRAM
  #pragma surface surf Standard addshadow
  #pragma target 3.0
  sampler2D _MainTex;half _Cutout,_Cutoff;
  struct Input {float2 uv_MainTex;float4 color:COLOR;float3 viewDir;};
  void surf(Input i,inout SurfaceOutputStandard o) {
   fixed4 c=tex2D(_MainTex,i.uv_MainTex);
   clip(lerp(1,c.a-_Cutoff,_Cutout));
   o.Albedo=c.rgb;o.Metallic=0;o.Smoothness=lerp(.28,.78,i.color.a)*(1-_Cutout*.7);
   o.Normal=float3(0,0,i.viewDir.z>=0?1:-1);o.Alpha=1;
  }
  ENDCG
 } FallBack "Diffuse"
}
