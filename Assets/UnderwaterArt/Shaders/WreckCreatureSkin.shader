Shader "Underwater/Wreck Creature Skin" {
 Properties {_MainTex("Skin detail",2D)="white"{} _Color("Color",Color)=(.4,.5,.5,1) _VertexColors("Artist vertex colours",Float)=0 _Kind("Species",Float)=0 _TailPhase("Swim cycle",Float)=0}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert addshadow
 #pragma target 3.0
 sampler2D _MainTex;float4 _Color;float _Kind,_TailPhase,_ReefLight,_VertexColors;
 struct Input {float2 uv_MainTex;float3 body;float4 color:COLOR;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.body=v.vertex.xyz;o.color=v.color;float tail=saturate((-v.vertex.z-.15)/1.5);if(_Kind>1.5)v.vertex.y+=sin(_TailPhase+v.vertex.z*.9)*tail*tail*.19;else if(_Kind<.5)v.vertex.x+=sin(_TailPhase+v.vertex.z*3)*tail*tail*.045;}
 void surf(Input i,inout SurfaceOutputStandard o){float3 color=lerp(_Color.rgb,i.color.rgb,_VertexColors);float spots=sin(i.body.x*49+sin(i.body.z*21))*sin(i.body.y*43+i.body.z*13);if(_Kind>1.5)color*=lerp(1.35,.7,smoothstep(-.25,.3,i.body.y));else color*=.9+.16*spots;o.Albedo=color*lerp(float3(1,1,1),tex2D(_MainTex,i.uv_MainTex).rgb,.25);if(_Kind>.5&&_Kind<1.5){float mottles=sin(i.body.z*31+sin(i.body.y*37+i.body.x*17))*sin(i.body.x*43+i.body.y*29);o.Albedo*=lerp(.48,1.25,smoothstep(-.4,.65,mottles));float grain=sin(i.body.z*227)*sin(i.body.y*193+i.body.x*211);o.Normal=normalize(float3(grain*.06,sin(i.body.x*179)*.04,1));}o.Smoothness=_Kind>1.5?.45:.22;o.Metallic=0;o.Alpha=1;o.Emission=o.Albedo*float3(.12,.16,.2)*_ReefLight;}
 ENDCG
 } FallBack "Standard"
}
