Shader "Vaultsaw/FlameParticle"
{
    Properties
    {
        _MainTex    ("Textura", 2D)      = "white" {}
        _CoreColor  ("Color Centro", Color)  = (0.5, 0.9, 1.0, 1)
        _OuterColor ("Color Exterior", Color) = (0.0, 0.3, 1.0, 0)
        _Intensity  ("Intensidad", Range(1, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One        // Additive — hace que brille
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;      // color de la partícula
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _CoreColor;
                float4 _OuterColor;
                float  _Intensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Samplea la textura de llama
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Aplica color del gradiente del particle system
                half4 color    = texColor * IN.color;

                // Brillo additive
                color.rgb     *= _Intensity;

                return color;
            }
            ENDHLSL
        }
    }
}