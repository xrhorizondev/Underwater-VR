Shader "Underwater/Reef Shark Skin" {
 Properties {_Color("Skin",Color)=(.27,.34,.36,1) _TailPhase("Swim",Float)=0}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert addshadow
 #pragma target 3.0
 float4 _Color;float _TailPhase,_ReefLight;
 struct Input {float3 body;float4 color:COLOR;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.body=v.vertex.xyz;o.color=v.color;float tail=saturate((-v.vertex.z-.1)/1.5);v.vertex.x+=sin(_TailPhase+tail*3)*tail*tail*.17;}
 void surf(Input i,inout SurfaceOutputStandard o){float underside=1-smoothstep(-.09,.06,i.body.y);float grain=sin(i.body.z*181+i.body.x*63)*sin(i.body.y*209);o.Albedo=lerp(_Color.rgb,float3(.65,.68,.65),underside)*(.96+grain*.035)*i.color.rgb;o.Normal=normalize(float3(grain*.025,0,1));o.Metallic=0;o.Smoothness=.38;o.Emission=o.Albedo*float3(.12,.16,.2)*_ReefLight;o.Alpha=1;}
 ENDCG
 } FallBack "Standard"
}
