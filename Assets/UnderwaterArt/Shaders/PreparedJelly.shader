Shader "Underwater/Prepared Jelly" {
 Properties {_MainTex("Tissue",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off
 CGPROGRAM
 #pragma surface surf Standard alpha:fade
 sampler2D _MainTex;float4 _Color;
 struct Input {float2 uv_MainTex;};
 void surf(Input i,inout SurfaceOutputStandard o){float4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Smoothness=.35;o.Alpha=c.a;o.Emission=c.rgb*.06;}
 ENDCG
 } FallBack "Transparent/Diffuse"
}
