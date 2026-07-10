//
//  OutlineFillURP.shader
//  QuickOutline (URP variant)
//

Shader "Custom/Outline Fill URP" {
  Properties {
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0

    _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
    _OutlineWidth("Outline Width", Range(0, 10)) = 2
    _OutlineEmission("Outline Emission", Range(0, 10)) = 1
    [HideInInspector] _WireframeDrawMode("Wireframe Draw Mode", Float) = 0
    [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _OutlineStencilComp("Fill Stencil Compare", Float) = 6
  }

  SubShader {
    Tags {
      "Queue" = "Transparent+110"
      "RenderType" = "Transparent"
      "RenderPipeline" = "UniversalPipeline"
      "DisableBatching" = "True"
    }

    Pass {
      Name "Fill"
      Cull Off
      ZTest [_ZTest]
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      ColorMask RGB

      Stencil {
        Ref 1
        Comp [_OutlineStencilComp]
      }

      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      CBUFFER_START(UnityPerMaterial)
        float4 _OutlineColor;
        float _OutlineWidth;
        float _OutlineEmission;
        float _WireframeDrawMode;
        float _ZTest;
      CBUFFER_END

      struct Attributes {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float3 smoothNormal : TEXCOORD3;
        float4 barycentric : TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct Varyings {
        float4 positionCS : SV_POSITION;
        half4 color : COLOR;
        float3 bary : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      Varyings vert(Attributes input) {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        output.color = half4(_OutlineColor.rgb, _OutlineColor.a);
        output.bary = input.barycentric.xyz;

        if (_WireframeDrawMode > 0.5) {
          output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
          return output;
        }

        float3 normalOS = dot(input.smoothNormal, input.smoothNormal) > 1e-12 ? input.smoothNormal : input.normalOS;
        float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
        float3 viewPosition = TransformWorldToView(positionWS);
        float3 normalWS = TransformObjectToWorldNormal(normalOS);
        float3 viewNormal = normalize(mul(UNITY_MATRIX_V, float4(normalWS, 0.0)).xyz);
        float3 extrudedView = viewPosition + viewNormal * (-viewPosition.z * _OutlineWidth / 1000.0);
        output.positionCS = mul(UNITY_MATRIX_P, float4(extrudedView, 1.0));
        return output;
      }

      half4 frag(Varyings input) : SV_Target {
        half wfMode = (half)_WireframeDrawMode;
        float3 b = input.bary;
        float em = max(_OutlineEmission, 0.0);
        half4 tint = input.color;

        if (dot(b, b) <= 1e-8 || wfMode < (half)0.5) {
          half3 rgb = tint.rgb * (half)em;
          return half4(rgb, tint.a);
        }

        float minBary = min(b.x, min(b.y, b.z));
        float delta = max(fwidth(minBary), 1e-5) * max(_OutlineWidth, 1e-4);
        float edge = 1.0 - smoothstep(0.0, delta, minBary);

        if (wfMode < (half)1.5) {
          half a = tint.a * (half)edge;
          clip((float)a - 0.001);
          half3 rgb = tint.rgb * (half)em * (half)edge;
          return half4(rgb, a);
        }

        if (wfMode < (half)2.5) {
          half3 rgb = tint.rgb * (half)em * ((half)1.0 - (half)0.35 * (half)edge);
          return half4(rgb, tint.a);
        }

        return half4(tint.rgb * (half)em, tint.a);
      }
      ENDHLSL
    }
  }
}
