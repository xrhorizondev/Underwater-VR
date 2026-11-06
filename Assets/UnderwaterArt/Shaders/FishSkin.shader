Shader "Underwater/Living Fish Skin" {
 Properties {
  _MainTex("Photographic scales",2D)="white"{}
  _BumpMap("Scale relief",2D)="bump"{}
  _Color("Natural coloration",Color)=(.85,.9,.92,1)
  _Species("Reef color species",Float)=0
  _Pattern("Reef species markings",Float)=0
  _TailPhase("Tail phase",Float)=0
  _TailAmplitude("Tail amplitude",Float)=.055
  _TurnBend("Turn flexion",Float)=0
 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap;
 fixed4 _Color;float _ReefLight;float _Species;float _Pattern;float _TailPhase,_TailAmplitude,_TurnBend;
 struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 viewDir;float3 bodyPosition;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.bodyPosition=v.vertex.xyz;
  // Canonical mesh: head +Z, tail -Z, length 1 metre. Head and eyes remain stable.
  float tail=saturate((.28-v.vertex.z)/.78);
  float envelope=tail*tail;
  float phase=_TailPhase+tail*3.4;
  v.vertex.x+=sin(phase)*envelope*_TailAmplitude+envelope*_TurnBend;
  float derivative=-(2*tail*sin(phase)+3.4*envelope*cos(phase))*_TailAmplitude/.78;
  v.normal.z-=derivative*v.normal.x;
  float fin=smoothstep(.045,.11,abs(v.vertex.x))*(1-smoothstep(.3,.5,abs(v.vertex.z)));
  v.vertex.y+=sin(_TailPhase*.65+v.vertex.z*12)*fin*.008;
  v.normal=normalize(v.normal);
 }
 void surf(Input IN,inout SurfaceOutputStandard o){
  fixed4 c=tex2D(_MainTex,IN.uv_MainTex);
  float scaleDetail=dot(c.rgb,float3(.2126,.7152,.0722));
  float lifted=pow(max(scaleDetail,.0001),.62);
  o.Albedo=lerp(c.rgb,lifted*_Color.rgb,.88)*1.1;
  if(scaleDetail<.012)o.Albedo=c.rgb;
  if(_Pattern>.5){float z=IN.bodyPosition.z;float bands=_Pattern<1.5?min(abs(z-.24),min(abs(z+.02),abs(z+.33))):abs(frac(z*8+.2)-.5)*.15;float white=1-smoothstep(.022,.038,bands);float black=1-smoothstep(.04,.052,bands);float3 marking=lerp(_Color.rgb,float3(.009,.012,.014),black);marking=lerp(marking,float3(.8,.82,.77),white);float detail=dot(c.rgb,float3(.2126,.7152,.0722));o.Albedo=marking*lerp(.3,1.1,saturate(detail*2));if(detail<.035)o.Albedo=c.rgb*.7;}
  if(_Species>.5){
   float z=IN.bodyPosition.z,y=IN.bodyPosition.y;float3 pigment;
   if(_Species<1.5){pigment=lerp(float3(.78,.46,.006),float3(1,.74,.035),saturate(-y*2+.45));}
   else if(_Species<2.5){pigment=float3(.008,.11,.7);float band=smoothstep(-.03,.02,y)* (1-smoothstep(.14,.21,y+.04*sin(z*12)))*smoothstep(-.38,-.25,z)*(1-smoothstep(.26,.36,z));pigment=lerp(pigment,float3(.004,.009,.025),band);pigment=lerp(pigment,float3(.92,.62,.008),1-smoothstep(-.34,-.29,z));}
   else if(_Species<3.5){pigment=float3(.78,.8,.63);float stripes=min(abs(z-.25),min(abs(z),abs(z+.25)));pigment=lerp(pigment,float3(.65,.21,.015),1-smoothstep(.035,.07,stripes));float eyeSpot=1-smoothstep(.035,.05,length(float2(z+.17,y-.22)));pigment=lerp(pigment,float3(.008,.009,.012),eyeSpot);}
   else if(_Species<4.5){pigment=lerp(float3(.9,.59,.01),float3(.31,.012,.57),smoothstep(-.05,.035,z));}
   else if(_Species<5.5){pigment=lerp(float3(.94,.24,.045),float3(.66,.025,.19),smoothstep(.02,.24,y));pigment=lerp(pigment,float3(.94,.53,.19),(1-smoothstep(-.16,0,y)));}
   else {pigment=lerp(float3(.62,.72,.65),float3(.8,.58,.04),smoothstep(.03,.2,y));float stripes=1-smoothstep(.14,.25,abs(frac((z+.5)*5)-.5));pigment=lerp(pigment,float3(.018,.025,.035),stripes*smoothstep(-.36,-.29,z));}
   o.Albedo=pigment*(.38+lifted*.9);
   if(z>.2&&scaleDetail<.022)o.Albedo=c.rgb*.8;
  }
  o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),.45);
  o.Metallic=.035;o.Smoothness=.3;
  // Art-directed bounced water light keeps coloration legible on the shaded side.
  o.Emission=o.Albedo*float3(.3,.34,.38)*_ReefLight;
  o.Occlusion=1;o.Alpha=1;
 }
 ENDCG
 }
 FallBack "Standard"
}

