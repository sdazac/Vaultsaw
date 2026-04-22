Shader "Vaultsaw/SierraShader"
{
    Properties
    {
        _BaseColor      ("Color Base", Color)       = (0.6, 0.6, 0.6, 1)
        _SpecularColor  ("Color Specular", Color)   = (1, 1, 1, 1)
        _Shininess      ("Brillo Metálico", Range(10, 256)) = 80
        _PropulsionActive ("Propulsión Activa", Range(0, 1)) = 0
        _PropulsionColor ("Color Propulsión", Color) = (0.2, 0.5, 1, 1)
        _EmissionIntensity ("Intensidad Emisión", Range(0, 3)) = 1.5
        _RimPower       ("Rim Power", Range(0.1, 8)) = 3
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SpecularColor;
                float  _Shininess;
                float  _PropulsionActive;
                float4 _PropulsionColor;
                float  _EmissionIntensity;
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
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDir   = normalize(IN.viewDirWS);
                Light  mainLight = GetMainLight();

                // Blinn-Phong base (metálico)
                float3 halfDir   = normalize(mainLight.direction + viewDir);
                float  NdotL     = saturate(dot(normalWS, mainLight.direction));
                float  NdotH     = saturate(dot(normalWS, halfDir));
                float  specular  = pow(NdotH, _Shininess);

                float3 baseColor = _BaseColor.rgb * NdotL;
                float3 spec      = _SpecularColor.rgb * specular;
                float3 metallic  = baseColor + spec;

                // Rim light (borde brillante)
                float rim        = 1.0 - saturate(dot(viewDir, normalWS));
                float rimEffect  = pow(rim, _RimPower);

                // Mezcla estado normal vs propulsión
                float3 propColor = _PropulsionColor.rgb * _EmissionIntensity;
                float3 rimColor  = lerp(float3(1,1,1), propColor, _PropulsionActive);
                float3 emission  = propColor * _PropulsionActive * _EmissionIntensity;

                float3 finalColor = metallic + rimEffect * rimColor + emission;
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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