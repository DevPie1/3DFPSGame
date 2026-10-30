Shader "Game/KatanaSlashTrail"
{
    Properties
    {
        [HDR] _TrailColor ("Trail Color", Color) = (0.18, 0.75, 1, 0.65)
        [HDR] _EdgeColor ("Blade Edge", Color) = (0.85, 0.98, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _TrailColor;
                half4 _EdgeColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half span = pow(saturate(input.uv.x), 0.6h) * smoothstep(0.0h, 0.08h, 1.0h - input.uv.x);
                half edge = smoothstep(0.65h, 1.0h, input.uv.x);
                half3 color = lerp(_TrailColor.rgb, _EdgeColor.rgb, edge);
                return half4(color, _TrailColor.a * input.color.a * span);
            }
            ENDHLSL
        }
    }
}
