Shader "Underwater/Imported Marine Skin" {
 Properties {
  _MainTex("Skin atlas",2D)="white"{}
  _BumpMap("Surface detail",2D)="bump"{}
  _BumpScale("Surface depth",Range(0,1))=1
  _Color("Tint",Color)=(1,1,1,1)
  _Glossiness("Smoothness",Range(0,1))=.4
  _RoughnessMap("Roughness",2D)="white"{}
  _UseRoughnessMap("Use roughness map",Range(0,1))=0
  _OcclusionMap("Cavity occlusion",2D)="white"{}
  _OcclusionStrength("Cavity depth",Range(0,1))=.45
  [Enum(Front,2,Back,1,Both,0)] _Cull("Face orientation",Float)=0
 }
 SubShader {Tags {"RenderType"="Opaque"} Cull [_Cull]
  CGPROGRAM
  #pragma surface surf Standard
  #pragma target 3.0
  sampler2D _MainTex,_BumpMap,_RoughnessMap,_OcclusionMap;fixed4 _Color;half _Glossiness,_UseRoughnessMap,_OcclusionStrength,_BumpScale;float _ReefLight;
  // Tangent-space view direction preserves two-sided normal orientation without
  // VFACE in the generated vertex signature (invalid with Vulkan multiview).
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 viewDir;};
  void surf(Input i,inout SurfaceOutputStandard o){fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Normal=normalize(lerp(float3(0,0,1),UnpackNormal(tex2D(_BumpMap,i.uv_BumpMap)),_BumpScale));o.Normal.z*=i.viewDir.z>=0?1:-1;o.Smoothness=lerp(_Glossiness,1-tex2D(_RoughnessMap,i.uv_MainTex).r,_UseRoughnessMap);o.Occlusion=lerp(1,tex2D(_OcclusionMap,i.uv_MainTex).r,_OcclusionStrength);o.Metallic=0;o.Alpha=1;o.Emission=c.rgb*_ReefLight*float3(.06,.08,.10);}
  ENDCG
 } FallBack "Diffuse"
}
