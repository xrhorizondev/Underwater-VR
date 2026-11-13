Shader "Underwater/Water Surface From Below" {
 Properties { _Color("Deep blue reflection",Color)=(.018,.36,.56,1) }
 SubShader { Tags {"Queue"="Background" "RenderType"="Opaque"} Cull Off ZWrite On
 Pass { CGPROGRAM
 #pragma multi_compile_instancing
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{UNITY_VERTEX_OUTPUT_STEREO float4 pos:SV_POSITION;float3 world:TEXCOORD0;};
 float4 _Color,_ReefWaterColor;float _ReefTime;sampler2D _ReefCaustics;
 v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float3 p=v.vertex.xyz;p.y+=sin(p.x*.15+_ReefTime*.9)*.23+sin(p.z*.23-_ReefTime*.65)*.16;o.pos=UnityObjectToClipPos(float4(p,1));o.world=mul(unity_ObjectToWorld,float4(p,1)).xyz;return o;}
 float wave(float2 p,float t){p+=float2(sin(p.y*.33+t)*2.1,sin(p.x*.28-t)*2.4);return sin(p.x*2.1+sin(p.y*1.7+t)*.7+t)*.35+sin(p.y*3.2-p.x*.6-t*.8)*.22+sin(p.x*7.2+p.y*5.1+t*.5)*.07;}
 fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float2 p=i.world.xz;float t=_ReefTime*.85;
  #if defined(SHADER_API_MOBILE)
 float2 rippleUV=p*.09+float2(.045,-.023)*t;
 float2 slope=float2(tex2D(_ReefCaustics,rippleUV).r,tex2D(_ReefCaustics,rippleUV.yx*.83-t*.035).r)-.5;
 float3 n=normalize(float3(slope.x*.24,1,slope.y*.24));
 #else
 float h=wave(p,t);float3 n=normalize(float3((h-wave(p+float2(.06,0),t))*1.8,1,(h-wave(p+float2(0,.06),t))*1.8));
 #endif
 float3 view=normalize(i.world-_WorldSpaceCameraPos);float nd=saturate(dot(view,n));
 float window=smoothstep(.36,.85,nd);float3 col=lerp(_Color.rgb,float3(.04,.44,.64),window);
 float3 sun=normalize(float3(.18,.6,.78));float align=saturate(dot(view,normalize(sun+(n-float3(0,1,0))*.45)));
 float glow=pow(align,20),spec=pow(align,220);
 #if defined(SHADER_API_MOBILE)
 float ripple=saturate(.35+slope.x);
 #else
 float ripple=saturate(wave(p+float2(sin(p.y*.9),sin(p.x*.7)),t)*.5+.3);
 #endif
 col+=float3(.02,.06,.08)*ripple*(.25+window)+float3(.15,.3,.36)*glow+float3(1.8,2,2)*spec;
 float hWater=smoothstep(-.55,.55,view.y);float3 column=lerp(_ReefWaterColor.rgb*float3(.45,.5,.76),_ReefWaterColor.rgb*float3(.8,1.8,1.5),hWater);col=lerp(column,col,smoothstep(.16,.45,view.y));
 return float4(col,1);}
 ENDCG }
 }
}
