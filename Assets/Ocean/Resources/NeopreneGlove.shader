Shader "Underwater/Neoprene Glove" {
 SubShader { Tags {"RenderType"="Opaque"} Pass { Tags {"LightMode"="ForwardBase"}
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 struct Output {float4 pos:SV_POSITION;half3 shade:TEXCOORD0;float2 uv:TEXCOORD1;half3 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
 Output vert(appdata_full v){Output o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(Output,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);half3 n=UnityObjectToWorldNormal(v.normal);o.shade=max(.22,ShadeSH9(half4(n,1)))+_LightColor0.rgb*saturate(dot(n,_WorldSpaceLightPos0.xyz));o.uv=v.texcoord.xy;o.color=v.color.rgb;return o;}
 half4 frag(Output i):SV_Target{float2 thread=i.uv*float2(65,100);float fade=saturate(1-max(fwidth(thread.x),fwidth(thread.y)));float weave=1+.07*fade*cos(thread.x*6.283)*cos(thread.y*6.283);return half4(i.color*i.shade*weave,1);}
 ENDCG
 }}
}
