Shader "Underwater/Quest Reef" {
 Properties {_Cull("Culling",Float)=0 _BumpMap("Surface normal",2D)="bump"{} _BumpScale("Normal strength",Float)=1 _Color("Tint",Color)=(1,1,1,1) _MainTex("Color",2D)="white"{} _WorldMapping("World Mapping",Float)=0 _WorldScale("Scale",Float)=1 _Sway("Sway",Float)=0 _TextureSaturation("Saturation",Float)=1}
 SubShader {Tags {"RenderType"="Opaque"} Cull [_Cull]
 Pass {Tags {"LightMode"="ForwardBase"} CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_local _ REEF_WORLD_MAPPING
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 sampler2D _MainTex,_ReefCaustics,_BumpMap;half _BumpScale;half4 _Color;float _ReefTime,_WorldMapping,_WorldScale,_Sway,_TextureSaturation,_ReefLight;
 struct Output {float4 pos:SV_POSITION;float4 uv:TEXCOORD0;half3 light:TEXCOORD1;half3 tangentLight:TEXCOORD3;
 #if defined(REEF_WORLD_MAPPING)
 float3 worldTex:TEXCOORD4;half3 weights:TEXCOORD5;
 #endif
 UNITY_FOG_COORDS(2) UNITY_VERTEX_OUTPUT_STEREO};
 Output vert(appdata_full v){Output o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(Output,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
 v.vertex.x+=sin(_ReefTime+world.x+world.z*.7)*_Sway*saturate(v.vertex.y);
 o.pos=UnityObjectToClipPos(v.vertex);half3 n=UnityObjectToWorldNormal(v.normal);
 o.uv.xy=v.texcoord.xy;
 #if defined(REEF_WORLD_MAPPING)
 o.worldTex=world*_WorldScale;o.weights=n*n;o.weights*=o.weights;o.weights/=max(.001,dot(o.weights,1));
 #endif
 half3 tangent=UnityObjectToWorldDir(v.tangent.xyz);half3 sun=_WorldSpaceLightPos0.xyz;o.tangentLight=half3(dot(sun,tangent),dot(sun,cross(n,tangent))*v.tangent.w,dot(sun,n));
 o.uv.zw=world.xz*.23+world.y*.08+_ReefTime*float2(.048,.032);
 o.light=(max(half3(.08,.12,.16),ShadeSH9(half4(n,1)))+_LightColor0.rgb*saturate(dot(n,_WorldSpaceLightPos0.xyz)))*v.color.rgb;
 UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(Output i):SV_Target{
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 #if defined(REEF_WORLD_MAPPING)
 half3 tex=tex2D(_MainTex,i.worldTex.yz).rgb*i.weights.x+tex2D(_MainTex,i.worldTex.xz).rgb*i.weights.y+tex2D(_MainTex,i.worldTex.xy).rgb*i.weights.z;
 #else
 half3 tex=tex2D(_MainTex,i.uv.xy).rgb;
 #endif
 half3 mapped=UnpackNormal(tex2D(_BumpMap,i.uv.xy));i.light*=clamp(1+dot(mapped.xy,i.tangentLight.xy)*_BumpScale,.65,1.35);tex=lerp(dot(tex,half3(.21,.72,.07)),tex,_TextureSaturation);
 half caustic=tex2D(_ReefCaustics,i.uv.zw).r;
 fixed4 col=fixed4(tex*_Color.rgb*(i.light+caustic*_ReefLight*half3(.25,.34,.4)),1);UNITY_APPLY_FOG(i.fogCoord,col);return col;}
 ENDCG}
 }
}

