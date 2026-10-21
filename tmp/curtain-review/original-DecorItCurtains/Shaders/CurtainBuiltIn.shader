Shader "DecorIt/Curtain BuiltIn"
{
    Properties
    {
        _BaseColor("Cor da amostra", Color) = (1,1,1,1)
        _BaseMap("Trama neutra", 2D) = "white" {}
        _OpacityMap("Vazados: preto=furo branco=fio (R)", 2D) = "white" {}
        [Normal] _NormalMap("Normal da trama", 2D) = "bump" {}
        _NormalStrength("Intensidade do normal", Range(0,2)) = 0.35
        _ThreadOpacity("Opacidade dos fios", Range(0,1)) = 0.8
        _Cutoff("Limiar do recorte", Range(0.001,1)) = 0.35
        [Enum(Opaque,0,AlphaClip,1,Transparent,2)] _Surface("Modo (reaplique pelo importador)", Float) = 1
        [HideInInspector] _SrcBlend("SrcBlend", Float) = 1
        [HideInInspector] _DstBlend("DstBlend", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
        [HideInInspector] _Cull("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "UnityStandardUtils.cginc"
        sampler2D _BaseMap;
        sampler2D _OpacityMap;
        sampler2D _NormalMap;
        float4 _BaseMap_ST;
        fixed4 _BaseColor;
        half _NormalStrength;
        half _ThreadOpacity;
        half _Cutoff;
        half _Surface;

        half CurtainAlpha(float2 uv)
        {
            if (_Surface < 0.5h) return 1.0h;
            half alpha = tex2D(_OpacityMap, uv).r * _ThreadOpacity;
            if (_Surface < 1.5h)
            {
                clip(alpha - _Cutoff);
                return 1.0h;
            }
            return alpha;
        }
        ENDCG

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "AutoLight.cginc"
            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                half3 normal : TEXCOORD2;
                half4 tangent : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                SHADOW_COORDS(5)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = half4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w * unity_WorldTransformParams.w);
                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_SHADOW(o);
                return o;
            }
            fixed4 Frag(Varyings i, fixed facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half alpha = CurtainAlpha(i.uv);
                half3 baseColor = tex2D(_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half3 normalTS = UnpackScaleNormal(tex2D(_NormalMap, i.uv), _NormalStrength);
                half3 normal = normalize(i.normal);
                half3 tangent = normalize(i.tangent.xyz);
                half3 bitangent = normalize(cross(normal, tangent)) * i.tangent.w;
                normal = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normal);
                normal *= facing >= 0 ? 1.0h : -1.0h;
                half3 lightDirection = normalize(UnityWorldSpaceLightDir(i.worldPos));
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
                half3 lighting = max(ShadeSH9(half4(normal, 1.0h)), 0.0h) +
                    _LightColor0.rgb * saturate(dot(normal, lightDirection)) * attenuation;
                fixed4 color = fixed4(baseColor * lighting, alpha);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull [_Cull]
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            struct ShadowVaryings
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            ShadowVaryings ShadowVert(appdata_base v)
            {
                ShadowVaryings o = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                return o;
            }
            float4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                CurtainAlpha(i.uv);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    Fallback Off
}
