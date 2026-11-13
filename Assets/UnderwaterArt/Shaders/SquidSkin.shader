Shader "Underwater/Squid Skin" {
 Properties {_MainTex("Skin",2D)="white"{} _Color("Tint",Color)=(.16,.29,.32,1) _SwimPhase("Swim phase",Float)=0}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert addshadow
 #pragma target 3.0
 sampler2D _MainTex;float4 _Color;float _SwimPhase;struct Input {float2 uv_MainTex;};
 void vert(inout appdata_full v){float arm=saturate((-v.vertex.z-4)/12);v.vertex.xy+=float2(sin(_SwimPhase*1.3+v.vertex.z*.45),cos(_SwimPhase*1.1+v.vertex.z*.4))*arm*arm*.65;float pulse=1+sin(_SwimPhase*1.8)*.018*(1-arm);v.vertex.xy*=pulse;}
 void surf(Input IN,inout SurfaceOutputStandard o){o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;o.Smoothness=.2;o.Alpha=1;}
 ENDCG
 } FallBack "Standard"
}
