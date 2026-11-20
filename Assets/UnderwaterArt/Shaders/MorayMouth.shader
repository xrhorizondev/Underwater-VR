Shader "Underwater/Moray Mouth Lining" {
 Properties {_Color("Tint",Color)=(1,1,1,1)}
 SubShader {
  Tags {"RenderType"="Opaque"} Cull Off
  CGPROGRAM
  #pragma surface surf Standard
  #pragma target 3.0
  #pragma multi_compile_instancing
  fixed4 _Color;
  struct Input {float4 color:COLOR;};
  void surf(Input i,inout SurfaceOutputStandard o){half tooth=smoothstep(.7,.82,max(i.color.r,max(i.color.g,i.color.b)));o.Albedo=i.color.rgb*_Color.rgb*lerp(half3(.35,.22,.2),half3(.85,.85,.8),tooth);o.Metallic=0;o.Smoothness=.23;o.Occlusion=1;o.Alpha=1;}
  ENDCG
 }
 FallBack "Diffuse"
}
