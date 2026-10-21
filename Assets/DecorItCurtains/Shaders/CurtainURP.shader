Shader "DecorIt/Curtain URP"
{
    Properties
    {
        _BaseColor("Cor da amostra", Color) = (1,1,1,1)
        _BaseMap("Trama neutra", 2D) = "white" {}
        _OpacityMap("Vazados: preto=furo branco=fio (R)", 2D) = "white" {}
        [Normal] _NormalMap("Normal da trama", 2D) = "bump" {}
        _NormalStrength("Intensidade do normal", Range(0,2)) = 0.35
        _ThreadOpacity("Opacidade dos fios", Range(0,1)) = 0.8
        _Transmission("Luz atravessando os fios", Range(0,1)) = 0.25
        _Cutoff("Limiar do recorte", Range(0.001,1)) = 0.35
        [Enum(Opaque,0,AlphaClip,1,Transparent,2)] _Surface("Modo (reaplique pelo importador)", Float) = 1
        [HideInInspector] _SrcBlend("SrcBlend", Float) = 1
        [HideInInspector] _DstBlend("DstBlend", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
        [HideInInspector] _Cull("Cull", Float) = 0
    }
    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal": "12.0" }
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_OpacityMap); SAMPLER(sampler_OpacityMap);
        TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _NormalStrength;
            half _ThreadOpacity;
            half _Transmission;
            half _Cutoff;
            half _Surface;
        CBUFFER_END

        half CurtainAlpha(float2 uv)
        {
            if (_Surface < 0.5h) return 1.0h;
            half alpha = SAMPLE_TEXTURE2D(_OpacityMap, sampler_OpacityMap, uv).r * _ThreadOpacity;
            if (_Surface < 1.5h)
            {
                clip(alpha - _Cutoff);
                return 1.0h;
            }
            return alpha;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                half3 vertexLightFront : TEXCOORD6;
                half3 vertexLightBack : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = normal.normalWS;
                output.tangentWS = half4(normal.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(position);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    output.vertexLightFront = VertexLighting(position.positionWS, normal.normalWS);
                    output.vertexLightBack = VertexLighting(position.positionWS, -normal.normalWS);
                #endif
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half alpha = CurtainAlpha(input.uv);
                half3 weave = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half3 baseColor = lerp(0.65h, 1.0h, dot(weave, half3(0.2126h, 0.7152h, 0.0722h))) * _BaseColor.rgb;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalStrength);
                half3 normal = normalize(input.normalWS);
                half3 tangent = normalize(input.tangentWS.xyz);
                half3 bitangent = normalize(cross(normal, tangent)) * input.tangentWS.w;
                normal = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normal);
                normal *= IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lighting = max(SampleSH(normal), 0.0h);
                lighting += mainLight.color * saturate(dot(normal, mainLight.direction)) *
                    mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                // Thin cloth receives a little backlight; opacity remains controlled by the weave.
                lighting += mainLight.color * saturate(dot(-normal, mainLight.direction)) *
                    mainLight.distanceAttenuation * mainLight.shadowAttenuation * _Transmission;
                #ifdef _ADDITIONAL_LIGHTS
                    uint count = GetAdditionalLightsCount();
                    for (uint index = 0u; index < count; index++)
                    {
                        Light light = GetAdditionalLight(index, input.positionWS, half4(1,1,1,1));
                        lighting += light.color * saturate(dot(normal, light.direction)) *
                            light.distanceAttenuation * light.shadowAttenuation;
                        lighting += light.color * saturate(dot(-normal, light.direction)) *
                            light.distanceAttenuation * light.shadowAttenuation * _Transmission;
                    }
                #endif
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    lighting += IS_FRONT_VFACE(frontFace, input.vertexLightFront, input.vertexLightBack);
                #endif
                half3 color = MixFog(baseColor * lighting, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                CurtainAlpha(input.uv);
                return 0;
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
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #endif
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                CurtainAlpha(input.uv);
                return 0;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
