Shader "CustomOutline/BuiltIn"
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
            "IgnoreProjector" = "True"
        }

        LOD 100

        Pass
        {
            Name "Outline"
            ZWrite Off
            ZTest LEqual
            Cull [_OutlineCull]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ CUSTOMOUTLINE_SILHOUETTE

            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEmission;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 barycentric : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 posOS = v.vertex.xyz;
                #if defined(CUSTOMOUTLINE_SILHOUETTE)
                float3 n = normalize(v.normal);
                posOS += n * (_OutlineWidth * 0.002);
                #endif
                o.pos = UnityObjectToClipPos(float4(posOS, 1.0));
                o.barycentric = v.barycentric;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float em = max(_OutlineEmission, 0.0);

                #if defined(CUSTOMOUTLINE_SILHOUETTE)
                fixed4 c = _OutlineColor;
                c.rgb *= em;
                clip(c.a - 0.001);
                return c;
                #else
                float3 b = i.barycentric;
                float minBary = min(b.x, min(b.y, b.z));
                float delta = max(fwidth(minBary), 1e-5) * max(_OutlineWidth, 1e-4);
                float edge = 1.0 - smoothstep(0.0, delta, minBary);
                fixed a = _OutlineColor.a * edge;
                clip(a - 0.001);
                fixed3 rgb = _OutlineColor.rgb * em * edge;
                return fixed4(rgb, a);
                #endif
            }
            ENDCG
        }
    }

    FallBack Off
}
