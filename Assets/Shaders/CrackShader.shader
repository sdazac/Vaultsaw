Shader "Vaultsaw/CrackShader"
{
    Properties
    {
        _BaseColor      ("Color Base", Color)       = (0.8, 0.4, 0.1, 1)
        _DamagedColor   ("Color Dañado", Color)     = (0.5, 0.1, 0.0, 1)
        _CriticalColor  ("Color Crítico", Color)    = (0.2, 0.0, 0.0, 1)
        _Damage         ("Daño (0=sano 1=roto)", Range(0,1)) = 0
        _CrackColor     ("Color Grietas", Color)    = (0.05, 0.05, 0.05, 1)
        _CrackThreshold ("Grosor Grietas", Range(0.01, 0.5)) = 0.15
        _EmissionColor  ("Emisión Grietas", Color)  = (1, 0.3, 0.0, 1)
        _EmissionPower  ("Brillo Grietas", Range(0,3)) = 1.5
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DamagedColor;
                float4 _CriticalColor;
                float  _Damage;
                float4 _CrackColor;
                float  _CrackThreshold;
                float4 _EmissionColor;
                float  _EmissionPower;
            CBUFFER_END

            // Función hash para generar números pseudoaleatorios
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Genera patrón de grietas basado en UV y daño
            float CrackPattern(float2 uv, float damage)
            {
                float cracks = 0;

                // Grieta 1 — diagonal principal
                float line1 = abs(uv.x - uv.y - 0.1);
                cracks += smoothstep(_CrackThreshold, 0.0, line1) * damage;

                // Grieta 2 — diagonal secundaria
                float line2 = abs(uv.x + uv.y - 1.1);
                cracks += smoothstep(_CrackThreshold * 0.8, 0.0, line2) * max(0, damage - 0.3);

                // Grieta 3 — horizontal
                float line3 = abs(uv.y - 0.45);
                cracks += smoothstep(_CrackThreshold * 0.6, 0.0, line3) * max(0, damage - 0.5);

                // Grieta 4 — vertical
                float line4 = abs(uv.x - 0.55);
                cracks += smoothstep(_CrackThreshold * 0.6, 0.0, line4) * max(0, damage - 0.6);

                // Grietas pequeñas adicionales en estado crítico
                float line5 = abs(uv.x * 0.5 - uv.y * 0.8 + 0.2);
                cracks += smoothstep(_CrackThreshold * 0.4, 0.0, line5) * max(0, damage - 0.75);

                return saturate(cracks);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                Light  light    = GetMainLight();
                float  NdotL    = saturate(dot(normalWS, light.direction));
                float  ambient  = 0.3f;

                // Color base según nivel de daño
                float3 baseColor;
                if (_Damage < 0.4)
                    baseColor = lerp(_BaseColor.rgb, _DamagedColor.rgb, _Damage / 0.4);
                else if (_Damage < 0.75)
                    baseColor = lerp(_DamagedColor.rgb, _CriticalColor.rgb, (_Damage - 0.4) / 0.35);
                else
                    baseColor = _CriticalColor.rgb;

                // Iluminación simple
                baseColor *= (NdotL + ambient);

                // Patrón de grietas
                float crackMask = CrackPattern(IN.uv, _Damage);

                // Emisión naranja en las grietas (simula lava/calor interior)
                float3 emission = _EmissionColor.rgb * crackMask * _EmissionPower * _Damage;

                // Mezcla color base con grietas oscuras
                float3 finalColor = lerp(baseColor, _CrackColor.rgb, crackMask);

                // Suma emisión encima
                finalColor += emission;

                // Pulso de la emisión — late como si tuviera energía dentro
                float pulse = (sin(_Time.y * 3.0) * 0.5 + 0.5) * _Damage;
                finalColor += _EmissionColor.rgb * crackMask * pulse * 0.5;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attr { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Vary { float4 positionHCS : SV_POSITION; };

            Vary vertShadow(Attr IN)
            {
                Vary OUT;
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float  NdotL    = dot(normalWS, lightDir);
                float4 posHCS   = TransformWorldToHClip(posWS);
                posHCS.z       -= max(0.005 * (1.0 - NdotL), 0.001);
                OUT.positionHCS = posHCS;
                return OUT;
            }

            half4 fragShadow(Vary IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}