Shader "Underwater/Reef Surface" {
 Properties {
  _Color("Living tissue tint",Color)=(1,1,1,1)
  _MainTex("Photographic albedo",2D)="white"{}
  _BumpMap("Micro surface normal",2D)="bump"{}
  _OcclusionMap("Ambient occlusion",2D)="white"{}
  _BumpScale("Normal strength",Range(0,2))=1
  _Glossiness("Smoothness",Range(0,1))=.18
  _WorldScale("World texture scale",Float)=.35
  _WorldMapping("Triplanar mapping",Range(0,1))=0
  _CausticStrength("Caustic illumination",Range(0,3))=.65
  _TextureSaturation("Texture saturation",Range(0,1))=1
  _Encrustation("Living reef coverage",Range(0,1))=0
  _Sway("Soft tissue motion",Float)=0
  _Swim("Tail motion",Float)=0
  _EmissionColor("Bioluminescence",Color)=(0,0,0,1)
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 300 Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
  #pragma target 3.0
  sampler2D _MainTex,_BumpMap,_OcclusionMap,_ReefCaustics;
  fixed4 _Color,_EmissionColor;
  half _Glossiness,_BumpScale,_WorldMapping,_WorldScale,_CausticStrength,_Sway,_Swim;
  float _ReefTime,_ReefLight;half _TextureSaturation,_Encrustation;
  float hash31(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
  float reefNoise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash31(i),hash31(i+float3(1,0,0)),f.x),lerp(hash31(i+float3(0,1,0)),hash31(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(i+float3(0,0,1)),hash31(i+float3(1,0,1)),f.x),lerp(hash31(i+float3(0,1,1)),hash31(i+float3(1,1,1)),f.x),f.y),f.z);}
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float2 uv_OcclusionMap;float3 worldPos;float3 worldNormal;float4 color:COLOR; INTERNAL_DATA};
  void vert(inout appdata_full v){float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;v.vertex.x+=sin(_ReefTime*1.05+w.z*.7+w.x)*_Sway*saturate(v.vertex.y);v.vertex.x+=sin(_ReefTime*6+v.vertex.z*6+w.x)*_Swim*saturate(-v.vertex.z*2);}
  void surf(Input IN,inout SurfaceOutputStandard o){
   fixed4 tex=tex2D(_MainTex,IN.uv_MainTex);
   if(_WorldMapping>.5){float3 n=abs(WorldNormalVector(IN,float3(0,0,1)));float3 weights=pow(n,4);weights/=max(.001,dot(weights,1));float3 p=IN.worldPos*_WorldScale;tex=tex2D(_MainTex,p.yz)*weights.x+tex2D(_MainTex,p.xz)*weights.y+tex2D(_MainTex,p.xy)*weights.z;}
   float luminance=dot(tex.rgb,float3(.2126,.7152,.0722));
   o.Albedo=lerp(luminance.xxx,tex.rgb,_TextureSaturation)*_Color.rgb*IN.color.rgb;
   #if !defined(SHADER_API_MOBILE)
   if(_Encrustation>.01){float3 p=IN.worldPos;float mask=reefNoise(p*2.7)*.45+reefNoise(p*8.7)*.35+reefNoise(p*23)*.2;float palette=reefNoise(p*1.6+17);float3 tissue=palette<.38?float3(.22,.008,.003):palette<.62?float3(.09,.008,.032):palette<.8?float3(.19,.075,.009):float3(.018,.055,.028);float cover=smoothstep(.51,.63,mask)*smoothstep(.22,.5,reefNoise(p*39))*_Encrustation;o.Albedo=lerp(o.Albedo,tissue*(.2+luminance*1.8),cover);}
   #else
   if(_Encrustation>.01){half cover=smoothstep(.38,.68,tex.g)*_Encrustation;o.Albedo=lerp(o.Albedo,half3(.22,.025,.015)*(luminance+.2),cover*.65);}
   #endif
   // Water attenuates warm light with view distance; preserve nearby reef colors.
   float waterPath=distance(_WorldSpaceCameraPos,IN.worldPos);o.Albedo*=exp(-float3(.022,.007,.002)*waterPath);
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   o.Occlusion=lerp(.45,1,tex2D(_OcclusionMap,IN.uv_OcclusionMap).r)*lerp(.6,1,IN.color.a);
   o.Smoothness=_Glossiness;o.Metallic=0;o.Alpha=1;
   float2 uv=IN.worldPos.xz*.23+IN.worldPos.y*.08;
   float c1=tex2D(_ReefCaustics,uv+float2(_ReefTime*.048,_ReefTime*.032)).r;
   float c2=tex2D(_ReefCaustics,uv*1.19+float2(-_ReefTime*.032,_ReefTime*.04)).r;
   float facing=saturate(WorldNormalVector(IN,float3(0,0,1)).y*.7+.3);
   o.Emission=o.Albedo*(float3(.62,.8,1)*pow(saturate(min(c1,c2)*1.3),1.6)*_CausticStrength*1.45*facing+float3(.008,.013,.023))*_ReefLight+_EmissionColor.rgb;
  }
  ENDCG
 }
 FallBack "Standard"
}



