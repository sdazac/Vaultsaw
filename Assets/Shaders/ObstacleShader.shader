Shader "Vaultsaw/ObstacleShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Alpha("Transparency", Range(0, 1)) = 0.5
        _FresnelColor("Fresnel Glow Color", Color) = (0, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.5, 5)) = 2.0
        _FresnelIntensity("Fresnel Intensity", Range(0, 2)) = 1.0
        _GlowColor("Glow Color", Color) = (0, 1, 1, 1)
        _GlowIntensity("Glow Intensity", Range(0, 2)) = 0.5
        _GlowWidth("Glow Width", Range(0, 0.1)) = 0.02
        _PulseSpeed("Pulse Speed", Range(0.5, 5)) = 2.0
        _PulseIntensity("Pulse Intensity", Range(0, 1)) = 0.5
        _RefractionStrength("Refraction Strength", Range(0, 1)) = 0.5
        _Smoothness("Glass Smoothness", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _Alpha;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelIntensity;
                half4 _GlowColor;
                half _GlowIntensity;
                half _GlowWidth;
                half _PulseSpeed;
                half _PulseIntensity;
                half _RefractionStrength;
                half _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseTexture = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                
                // Apply pulsating effect to base color
                half pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0, pulse, _PulseIntensity);
                half4 baseColor = baseTexture * _BaseColor * pulse;
                
                // Calculate Fresnel effect
                half3 normalWS = normalize(IN.normalWS);
                half3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);
                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                
                // Apply fresnel glow
                half4 fresnelGlow = fresnel * _FresnelColor * _FresnelIntensity;
                
                // Combine base color with fresnel glow
                half4 finalColor = baseColor + fresnelGlow;
                
                // Apply glassy transparency
                // Make edges (grazing angles) more opaque and center more transparent (glass effect)
                half glassFresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), 1.0 / max(_Smoothness, 0.1));
                half glassAlpha = lerp(_Alpha * 0.3, _Alpha, glassFresnel * _RefractionStrength);
                finalColor.a = baseTexture.a * _BaseColor.a * glassAlpha;
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Glow Pass - render backfaces to create glow effect
        Pass
        {
            Cull Front
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _Alpha;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelIntensity;
                half4 _GlowColor;
                half _GlowIntensity;
                half _GlowWidth;
                half _PulseSpeed;
                half _PulseIntensity;
                half _RefractionStrength;
                half _Smoothness;
            CBUFFER_END
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Scale up the mesh slightly for glow effect
                float3 positionOS = IN.positionOS.xyz;
                float3 normalOS = normalize(IN.normalOS);
                positionOS += normalOS * _GlowWidth;
                OUT.positionHCS = TransformObjectToHClip(positionOS);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Pulsating glow
                half pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0, pulse, _PulseIntensity);
                
                half4 glowColor = _GlowColor * _GlowIntensity * pulse;
                glowColor.a = _Alpha * 0.6;
                return glowColor;
            }
            ENDHLSL
        }
    }
}
