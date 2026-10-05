Shader "StealthGame/RainOutsideWindow"
{
 Properties{_Flash("Lightning",Float)=0 _RainSpeed("Rain speed",Float)=1 _RainDensity("Rain density",Float)=1 _Seed("Seed",Float)=0}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
 Pass{
 Tags{"LightMode"="SRPDefaultUnlit"}
 Cull Off ZWrite On
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float _Flash,_RainSpeed,_RainDensity,_Seed;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float hash(float n){return frac(sin(n*127.1+_Seed*31.7)*43758.5453);}
 float drops(float2 uv,float scale,float speed){float2 p=uv*float2(scale,5);p.x+=uv.y*.9;float column=floor(p.x);p.y+=_Time.y*speed*_RainSpeed+hash(column)*30;float2 f=frac(p);float gate=step(hash(column+floor(p.y)*5),saturate(_RainDensity*.65));return (1-smoothstep(.018,.065,abs(f.x-.5)))*(1-smoothstep(.05,.6,f.y))*gate;}
 half4 frag(V i):SV_Target{
 float2 uv=i.uv; // The original window mesh masks the opening and its arch.
 float rain=drops(uv,27,4)*.7+drops(uv+float2(.17,.33),45,6)*.35;
 float3 sky=lerp(float3(.025,.038,.065),float3(.055,.085,.13),uv.y);
 float3 color=sky+rain*float3(.32,.43,.6)+_Flash*(float3(1.3,1.6,2.1)+rain*.7);
 return half4(color,1);
 }
 ENDHLSL
 }
 }
}
