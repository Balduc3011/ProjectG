//
//  OutlineFill.shader
//  QuickOutline
//
//  Created by Chris Nolet on 2/21/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

Shader "Custom/Outline Fill" {
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

      CGPROGRAM
      #include "UnityCG.cginc"

      #pragma vertex vert
      #pragma fragment frag

      struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float3 smoothNormal : TEXCOORD3;
        float4 barycentric : TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f {
        float4 position : SV_POSITION;
        fixed4 color : COLOR;
        float3 bary : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      uniform fixed4 _OutlineColor;
      uniform float _OutlineWidth;
      uniform float _OutlineEmission;
      uniform float _WireframeDrawMode;

      v2f vert(appdata input) {
        v2f output;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        output.color = _OutlineColor;
        output.bary = input.barycentric.xyz;

        // Topology wireframe: draw on actual mesh surface (no shell extrusion).
        if (_WireframeDrawMode > 0.5) {
          output.position = UnityObjectToClipPos(input.vertex);
          return output;
        }

        float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
        float3 viewPosition = UnityObjectToViewPos(input.vertex);
        float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));

        output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth / 1000.0);
        return output;
      }

      fixed4 frag(v2f input) : SV_Target {
        fixed wfMode = _WireframeDrawMode;
        fixed3 b = input.bary;
        float em = max(_OutlineEmission, 0.0);
        fixed4 tint = input.color;

        if (dot(b, b) <= 1e-8 || wfMode < 0.5) {
          fixed4 c = tint;
          c.rgb *= em;
          return c;
        }

        // Same edge math as CustomOutline/BuiltIn (barycentric wireframe).
        float minBary = min(b.x, min(b.y, b.z));
        float delta = max(fwidth(minBary), 1e-5) * max(_OutlineWidth, 1e-4);
        float edge = 1.0 - smoothstep(0.0, delta, minBary);

        // 1 = edges only: rgb and alpha scale by edge (interior transparent — base mesh shows).
        if (wfMode < 1.5) {
          fixed a = tint.a * edge;
          clip(a - 0.001);
          fixed3 rgb = tint.rgb * em * edge;
          return fixed4(rgb, a);
        }

        // 2 = full faces + edge lines (opaque, slightly darker on edges).
        if (wfMode < 2.5) {
          fixed3 rgb = tint.rgb * em * (1.0 - 0.35 * edge);
          return fixed4(rgb, tint.a);
        }

        // 3 = full faces, uniform (no edge lines — opposite of mode 1).
        return fixed4(tint.rgb * em, tint.a);
      }
      ENDCG
    }
  }
}
