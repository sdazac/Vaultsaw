Shader "Vaultsaw/ObstaculoShader"
{
    Properties
    {
        _ColorA     ("Color Oscuro", Color)  = (0.8, 0.1, 0.1, 1)
        _ColorB     ("Color Brillante", Color) = (1, 0.5, 0, 1)
        _PulseSpeed ("Velocidad Pulso", Range(0.5, 5)) = 2
        _RimColor   ("Color Rim", Color)     = (1, 0.2, 0.2, 1)
        _RimPower   ("Rim Power", Range(0.1, 8)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float  _PulseSpeed;
                float4 _RimColor;
                float  _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir  = normalize(IN.viewDirWS);
                Light  light    = GetMainLight();

                // Pulso usando seno del tiempo
                float pulse     = (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5;
                float3 color    = lerp(_ColorA.rgb, _ColorB.rgb, pulse);

                // Iluminación simple
                float NdotL     = saturate(dot(normalWS, light.direction));
                color          *= (NdotL * 0.7 + 0.3);

                // Rim de peligro
                float rim       = 1.0 - saturate(dot(viewDir, normalWS));
                float rimEffect = pow(rim, _RimPower);
                color          += _RimColor.rgb * rimEffect;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}