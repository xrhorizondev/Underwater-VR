Shader "Underwater/Stingray Skin" {
 Properties {_MainTex("Skin",2D)="white"{} _Color("Color",Color)=(.42,.37,.28,1) _StrokePhase("Stroke",Float)=0 _StrokeStrength("Strength",Float)=1}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Lambert vertex:vert addshadow
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color;float _StrokePhase,_StrokeStrength,_ReefLight;
 struct Input {float2 uv_MainTex;};
 void vert(inout appdata_full v){
  float side=v.vertex.x<0?-1:1;float span=saturate((abs(v.vertex.x)-.22)/1.65);
  float phase=_StrokePhase-span*.65-v.vertex.z*.65;
  float lift=sin(phase)*pow(span,1.35)*.58*_StrokeStrength;
  v.vertex.y+=lift;v.vertex.x-=side*span*span*(1-cos(phase))*.06*_StrokeStrength;
  v.normal.x-=side*cos(phase)*pow(span,.6)*.48*_StrokeStrength;v.normal=normalize(v.normal);
  if(v.vertex.z< -1.1)v.vertex.x+=sin(_StrokePhase*.6-v.vertex.z)*.055*saturate(-v.vertex.z-1.1);
 } void surf(Input i,inout SurfaceOutput o){o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb;o.Emission=o.Albedo*float3(.02,.03,.04)*_ReefLight;o.Alpha=1;}
 ENDCG
 }
 Fallback "Diffuse"
}
