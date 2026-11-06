Shader "Underwater/Blue Water Column" {
 SubShader { Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma multi_compile_instancing
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{UNITY_VERTEX_OUTPUT_STEREO float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 float4 _ReefWaterColor;float _ReefLight;
 v2f vert(appdata_base v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float3 d=normalize(i.dir);float h=smoothstep(-.55,.55,d.y);float3 deep=_ReefWaterColor.rgb*float3(.45,.5,.76);float3 bright=_ReefWaterColor.rgb*float3(.8,1.8,1.5);return float4(lerp(deep,bright,h),1);}
 ENDCG }
 }
}

