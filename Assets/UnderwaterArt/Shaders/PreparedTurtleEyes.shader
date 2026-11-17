Shader "Underwater/Prepared Turtle Eyes" {
 Properties { _MainTex("Color",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Blink("Blink",Range(0,1))=0 _EyeY("Eye centre height",Float)=0 }
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard vertex:vert
 sampler2D _MainTex;float4 _Color;float _Blink,_EyeY;
 struct Input {float2 uv_MainTex;};
 void vert(inout appdata_full v){v.vertex.y=lerp(v.vertex.y,_EyeY,saturate(_Blink)*.97);}
 void surf(Input i,inout SurfaceOutputStandard o){o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb;o.Smoothness=.55;o.Alpha=1;}
 ENDCG
 } FallBack "Standard"
}
