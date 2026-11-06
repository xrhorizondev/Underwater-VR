Shader "Underwater/Diver Bubbles" {
 Properties {_Color("Reflection tint",Color)=(.68,.87,.94,.62)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;fixed4 color:COLOR;UNITY_FOG_COORDS(2) UNITY_VERTEX_OUTPUT_STEREO};
 fixed4 _Color;float4 _ReefWaterColor;float _ReefLight;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color*_Color;UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 half3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos-i.world);half facing=saturate(dot(n,view));half fresnel=pow(1-facing,4);half3 reflected=reflect(-view,n);
 // Low-cost approximation of the bright water surface and dark reef reflected in air bubbles.
 half sky=smoothstep(-.25,.65,reflected.y);half3 water=max(_ReefWaterColor.rgb,half3(.01,.04,.06));half3 reflection=lerp(water*.12,lerp(water,half3(.68,.9,1),.6),sky);
 half highlight=pow(saturate(dot(reflected,normalize(half3(-.3,.85,.35)))),80);half lowerArc=pow(saturate(-reflected.y),12)*.08;
 half alpha=saturate((.035+fresnel*.52+highlight*.72+lowerArc)*i.color.a);half3 color=reflection*i.color.rgb+highlight*max(.2,_ReefLight)+lowerArc*water;
 // Fade within 20 cm of the eye so exhalation cannot flash across the headset.
 alpha*=smoothstep(.12,.25,distance(_WorldSpaceCameraPos,i.world));fixed4 col=fixed4(color,alpha);UNITY_APPLY_FOG(i.fogCoord,col);return col;}
 ENDCG}
 }
}
