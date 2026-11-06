Shader "Underwater/Manta Skin" {
 Properties {_MainTex("Manta skin",2D)="white"{} _BumpMap("Skin detail",2D)="bump"{} _DorsalMarkingTint("Muted back markings",Color)=(.075,.085,.09,1) _StrokePhase("Stroke",Float)=0 _StrokeStrength("Wing stroke",Float)=1}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert addshadow
 #pragma target 3.0
 float _StrokePhase,_StrokeStrength;float _ReefLight;
 sampler2D _MainTex,_BumpMap;float4 _DorsalMarkingTint;
 struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 local;float3 localNormal;float3 worldPos;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.local=v.vertex.xyz;o.localNormal=v.normal;
 float span=saturate((abs(v.vertex.x)-.45)/1.95);float phase=_StrokePhase-span*.85+v.vertex.z*.24;
 v.vertex.y+=sin(phase)*pow(span,1.5)*.72*_StrokeStrength;
 float slope=(1.5*sqrt(span)*sin(phase)-.85*pow(span,1.5)*cos(phase))*.72/1.95*_StrokeStrength*sign(v.vertex.x);
 v.normal=normalize(float3(v.normal.x-slope*v.normal.y,v.normal.y,v.normal.z));
 if(v.vertex.z< -1.1)v.vertex.x+=sin(_StrokePhase*.5-v.vertex.z)*.025*saturate(-v.vertex.z-1.1);
 }
 void surf(Input i,inout SurfaceOutputStandard o){
 float3 skin=tex2D(_MainTex,i.uv_MainTex).rgb;
 // Remap the bright dorsal patches without darkening the pale underside.
 float luminance=dot(skin,float3(.2126,.7152,.0722));
 float dorsal=smoothstep(-.05,.35,i.localNormal.y);
 float marking=smoothstep(.12,.5,luminance)*dorsal;
 o.Albedo=lerp(skin,_DorsalMarkingTint.rgb*(.85+.15*luminance),marking);
 o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_BumpMap),.45);o.Smoothness=.12;o.Metallic=0;o.Alpha=1;o.Emission=o.Albedo*float3(.012,.02,.025)*_ReefLight;
 }
 ENDCG
 }
 Fallback "Diffuse"
}



