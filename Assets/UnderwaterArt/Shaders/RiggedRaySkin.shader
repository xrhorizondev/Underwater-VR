Shader "Underwater/Rigged Ray Skin" {
 Properties {_ProceduralStroke("Anatomical wing animation",Float)=0 _StrokePhase("Stroke phase",Float)=0 _StrokeStrength("Stroke strength",Float)=1 _MainTex("Source anatomy mask",2D)="gray"{} _Color("Natural skin",Color)=(.25,.24,.19,1)}
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
 #pragma target 3.0
 sampler2D _MainTex;float4 _Color;float _ReefLight,_ProceduralStroke,_StrokePhase,_StrokeStrength;
 struct Input {float2 uv_MainTex;float eyePart;float normalY;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.eyePart=v.texcoord1.x;o.normalY=v.normal.y;if(_ProceduralStroke>.5){float wing=saturate((abs(v.vertex.x)-.18)/.78);float phase=_StrokePhase-wing*.9+v.vertex.z*.4;v.vertex.y+=sin(phase)*pow(wing,1.6)*.17*_StrokeStrength;float slope=(1.6*pow(max(wing,.0001),.6)*sin(phase)-.9*pow(wing,1.6)*cos(phase))*.17*_StrokeStrength/.78*sign(v.vertex.x);v.normal=normalize(float3(v.normal.x-slope*v.normal.y,v.normal.y,v.normal.z));if(v.vertex.z<-.5)v.vertex.x+=sin(_StrokePhase*.55-v.vertex.z*2)*.025*saturate(-v.vertex.z-.5);}}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input i,inout SurfaceOutputStandard o){float2 p=i.uv_MainTex;float mottles=noise(p*42)*.7+noise(p*113)*.3;float grain=noise(p*900);float spot=1-smoothstep(.08,.16,noise(p*175));float underside=smoothstep(.84,.94,dot(tex2D(_MainTex,p).rgb,float3(.2126,.7152,.0722)));if(_ProceduralStroke>.5)underside=1-smoothstep(-.4,.15,i.normalY);o.Albedo=lerp(_Color.rgb*(.7+.6*mottles),float3(.58,.55,.47),underside);o.Albedo=lerp(o.Albedo,o.Albedo*.45,spot*.45*(1-underside));o.Normal=normalize(float3((noise(p*900+.3)-grain)*.1,(noise(p*900-.3)-grain)*.1,1));o.Smoothness=.22+grain*.12;if(i.eyePart>.5&&i.eyePart<2.5){o.Albedo=i.eyePart<1.5?float3(.15,.12,.045):float3(.003,.005,.007);o.Normal=float3(0,0,1);o.Smoothness=.8;}if(i.eyePart>3.5){o.Albedo=float3(.035,.03,.02);o.Smoothness=.18;}o.Alpha=1;o.Emission=o.Albedo*.065*_ReefLight;}
 ENDCG
 } FallBack "Standard"
}
