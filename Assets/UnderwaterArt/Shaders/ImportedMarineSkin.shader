Shader "Underwater/Imported Marine Skin" {
 Properties {
  _MainTex("Skin atlas",2D)="white"{}
  _BumpMap("Surface detail",2D)="bump"{}
  _Color("Tint",Color)=(1,1,1,1)
  _Glossiness("Smoothness",Range(0,1))=.4
  [Enum(Front,2,Back,1,Both,0)] _Cull("Face orientation",Float)=0
 }
 SubShader {Tags {"RenderType"="Opaque"} Cull [_Cull]
  CGPROGRAM
  #pragma surface surf Standard
  #pragma target 3.0
  sampler2D _MainTex,_BumpMap;fixed4 _Color;half _Glossiness;float _ReefLight;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float facing:VFACE;};
  void surf(Input i,inout SurfaceOutputStandard o){fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Normal=UnpackNormal(tex2D(_BumpMap,i.uv_BumpMap));o.Normal.z*=i.facing>=0?1:-1;o.Smoothness=_Glossiness;o.Metallic=0;o.Alpha=1;o.Emission=c.rgb*_ReefLight*float3(.06,.08,.10);}
  ENDCG
 } FallBack "Diffuse"
}
