Shader "CustomOutline/URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 0.5, 1)
        _OutlineWidth ("Outline Width", Float) = 1
        _OutlineEmission ("Outline Emission", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _OutlineCull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        LOD 100

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Cull [_OutlineCull]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile __ CUSTOMOUTLINE_SILHOUETTE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineEmission;
                float _OutlineCull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 barycentric : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 barycentric : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posOS = input.positionOS.xyz;
                #if defined(CUSTOMOUTLINE_SILHOUETTE)
                float3 n = normalize(input.normalOS);
                posOS += n * (_OutlineWidth * 0.002);
                #endif

                float3 positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.barycentric = input.barycentric;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half em = (half)max(_OutlineEmission, 0.0);

                #if defined(CUSTOMOUTLINE_SILHOUETTE)
                half3 rgb = (half3)_OutlineColor.rgb * em;
                half a = (half)_OutlineColor.a;
                clip(a - 0.001h);
                return half4(rgb, a);
                #else
                float3 b = input.barycentric;
                float minBary = min(b.x, min(b.y, b.z));
                float delta = max(fwidth(minBary), 1e-5) * max(_OutlineWidth, 1e-4);
                float edge = 1.0 - smoothstep(0.0, delta, minBary);
                half a = (half)_OutlineColor.a * (half)edge;
                clip(a - 0.001h);
                half3 rgb = (half3)_OutlineColor.rgb * em * (half)edge;
                return half4(rgb, a);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
