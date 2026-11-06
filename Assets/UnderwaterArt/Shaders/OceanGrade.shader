Shader "Hidden/Underwater/Optics" {
 Properties{_MainTex("Source",2D)="white"{}}
 SubShader{Cull Off ZWrite Off ZTest Always
 Pass{CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _MainTex; UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);float4 _MainTex_TexelSize;float4x4 _InverseProjection,_CameraWorld;float _ReefTime,_ReefLight;float _Exposure;
 float3 aces(float3 x){return saturate((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14));}
 fixed4 frag(v2f_img i):SV_Target{
 float2 uv=i.uv;float3 c=tex2D(_MainTex,uv).rgb;
 float depth=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,uv));
 // Short-range contact occlusion. Depth samples are bounded to preserve distant silhouettes.
 float ao=0;float2 px=_MainTex_TexelSize.xy*5;
 for(int k=0;k<8;k++){float a=k*.7854;float d=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,uv+float2(cos(a),sin(a))*px));float delta=depth-d;ao+=smoothstep(.03,.3,delta)*(1-smoothstep(.4,1.8,delta));}
 c*=1-ao*.035;
 float4 vp=mul(_InverseProjection,float4(uv*2-1,1,1));float3 ray=normalize(mul((float3x3)_CameraWorld,vp.xyz/vp.w));
 float3 origin=_WorldSpaceCameraPos;float rayLength=min(depth,38);float scattering=0;
 for(int j=0;j<16;j++){float s=(j+.5)/16.0;float3 p=origin+ray*(s*rayLength);float2 q=p.xz+float2(.48,-.2)*(8-p.y);float beam=pow(saturate(.52+.28*sin(q.x*.68+q.y*.17)+.24*sin(q.x*1.43-q.y*.32+_ReefTime*.06)),10);scattering+=beam*exp(-s*rayLength*.055);}
 c+=float3(.075,.23,.4)*scattering/16*min(rayLength*.06,1.6)*_ReefLight;
 float3 glow=0;float2 stepUV=_MainTex_TexelSize.xy*3;
 for(int b=0;b<4;b++){float a=b*1.5708;glow+=max(0,tex2D(_MainTex,uv+float2(cos(a),sin(a))*stepUV).rgb-1);}
 c+=glow*.055;
 c=aces(c*_Exposure);float luma=dot(c,float3(.2126,.7152,.0722));c=lerp(luma.xxx,c,1.025);float vig=1-.16*pow(length((uv-.5)*1.4),2);c*=vig;
 return float4(c,1);
 }
 ENDCG}
 }
}

