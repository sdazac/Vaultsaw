Shader "Vaultsaw/MonedaShader"
{
    Properties
    {
        _GoldDark     ("Dorado Borde", Color)    = (0.8, 0.5, 0.0, 1)
        _GoldLight    ("Dorado Centro", Color)   = (1.0, 0.85, 0.1, 1)
        _RimDark      ("Rim Oscuro", Color)      = (0.5, 0.3, 0.0, 1)
        _HighlightCol ("Color Reflejo", Color)   = (1.0, 1.0, 0.9, 1)
        _HighlightPow ("Tamaño Reflejo", Range(4, 128)) = 40
        _RimPower     ("Grosor Borde", Range(0.5, 8))   = 2.5
        _InnerRim     ("Grosor Círculo Interior", Range(0.1, 5)) = 4
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GoldDark;
                float4 _GoldLight;
                float4 _RimDark;
                float4 _HighlightCol;
                float  _HighlightPow;
                float  _RimPower;
                float  _InnerRim;
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

                // NdotL para iluminación base
                float NdotL = saturate(dot(normalWS, light.direction));

                // Fresnel — mide qué tan de lado está la superficie
                // 0 = centro de la moneda, 1 = borde
                float fresnel = 1.0 - saturate(dot(viewDir, normalWS));

                // Borde oscuro (rim exterior)
                float rim = pow(fresnel, _RimPower);

                // Círculo interior más claro
                float inner = 1.0 - pow(fresnel, _InnerRim);

                // Mezcla dorado oscuro → dorado claro hacia el centro
                float3 goldColor = lerp(_GoldDark.rgb, _GoldLight.rgb, inner);

                // Aplica borde oscuro en los extremos
                goldColor = lerp(goldColor, _RimDark.rgb, rim);

                // Iluminación suave
                goldColor *= (NdotL * 0.5 + 0.5);

                // Reflejo blanco estilo cartoon
                float3 halfDir    = normalize(light.direction + viewDir);
                float  NdotH      = saturate(dot(normalWS, halfDir));
                float  highlight  = pow(NdotH, _HighlightPow);
                // Cel shading del reflejo — paso brusco
                highlight = step(0.85, highlight);
                goldColor += _HighlightCol.rgb * highlight;

                return half4(goldColor, 1);
            }
            ENDHLSL
        }
    }
}