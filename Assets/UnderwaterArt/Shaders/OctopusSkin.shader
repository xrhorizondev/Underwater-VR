Shader "Underwater/Octopus Skin" {
 Properties {_Color("Skin tint",Color)=(.5,.3,.17,1)}
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 float4 _Color;struct Input {float3 worldPos;};
 void surf(Input IN,inout SurfaceOutputStandard o){float3 p=IN.worldPos;float mottles=sin(p.x*43+sin(p.z*21))*sin(p.y*37+p.z*19);float fine=sin(p.x*177)*sin(p.z*151+p.y*163);o.Albedo=_Color.rgb*(.85+.22*mottles+.06*fine);o.Smoothness=.19;o.Metallic=0;o.Alpha=1;}
 ENDCG
 } FallBack "Standard"
}
