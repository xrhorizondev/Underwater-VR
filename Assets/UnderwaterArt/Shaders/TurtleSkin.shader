Shader "Underwater/Loggerhead Skin" {
 Properties{_MainTex("Scutes and scaled skin",2D)="white"{} _BumpMap("Fine relief",2D)="bump"{} _Color("Natural tint",Color)=(.78,.72,.59,1) _StrokePhase("Stroke cycle",Float)=.1 _HeadTurn("Head turn",Float)=0 _Blink("Blink",Float)=0 _EyePart("Eye part",Float)=0 _Glossiness("Smoothness",Range(0,1))=.19}
 SubShader{Tags{"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap;float4 _Color;float _StrokePhase,_Glossiness,_ReefLight,_HeadTurn,_Blink,_EyePart;
 struct Input{float2 uv_MainTex;float2 uv_BumpMap;};
 void vert(inout appdata_full v){
  float head=smoothstep(.26,.49,v.vertex.z)*(1-smoothstep(.18,.3,abs(v.vertex.x)));
  float turn=_HeadTurn*head;float hs=sin(turn),hc=cos(turn);float2 hp=v.vertex.xz-float2(0,.26);
  v.vertex.xz=float2(hp.x*hc+hp.y*hs,-hp.x*hs+hp.y*hc)+float2(0,.26);
  v.normal.xz=float2(v.normal.x*hc+v.normal.z*hs,-v.normal.x*hs+v.normal.z*hc);
  v.vertex.y=lerp(v.vertex.y,.01+(v.vertex.y-.01)*.08,saturate(_Blink*_EyePart));
  float phase=frac(_StrokePhase);float stroke=phase<.4?lerp(.35,-.5,smoothstep(0,.4,phase)):phase<.76?lerp(-.5,.35,smoothstep(.4,.76,phase)):.35;
  float side=v.vertex.x<0?-1:1;
  float front=smoothstep(-.08,.18,v.vertex.z);float weight=smoothstep(.28,.46,abs(v.vertex.x))*(1-smoothstep(-.015,.015,v.vertex.y));
  float angle=side*stroke*weight*front;float sn=sin(angle),cs=cos(angle);float2 p=v.vertex.xy-float2(side*.22,-.045);
  v.vertex.xy=float2(p.x*cs-p.y*sn,p.x*sn+p.y*cs)+float2(side*.22,-.045);
  v.vertex.z-=sin(phase*6.283)*weight*front*.035;
  v.normal.xy=float2(v.normal.x*cs-v.normal.y*sn,v.normal.x*sn+v.normal.y*cs);
  float rear=(1-front)*smoothstep(.16,.35,abs(v.vertex.x));v.vertex.y+=sin(_StrokePhase*6.283*.5+side)*rear*.015;
 }
 void surf(Input IN,inout SurfaceOutputStandard o){o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),.65);o.Metallic=0;o.Smoothness=_Glossiness;o.Occlusion=1;o.Alpha=1;o.Emission=o.Albedo*float3(.055,.07,.08)*_ReefLight;}
 ENDCG
 }
 FallBack "Standard"
}

