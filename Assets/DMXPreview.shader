Shader "Custom/DMXPreview"
{
    Properties
    {
        _ChromaKeyColor("Chroma Key Color", Color) = (0.0, 0.0, 0.0, 0.0)
        [MainTexture] _BaseMap("DMX Texture", 2D) = "Black"
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ChromaKeyColor;
                float4 _BaseMap_TexelSize;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = v.vertex;
                o.vertex.xy *= 5.0;

#ifdef UNITY_REVERSED_Z
                o.vertex.z = 1.0;
#else
                o.vertex.z = 0.0;
#endif
                o.uv = ComputeScreenPos(o.vertex).xyw;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 pixel = floor((i.uv.xy / i.uv.z) * _ScreenParams.xy);

                pixel.y += (_BaseMap_TexelSize.w - _ScreenParams.y);

                float4 color = _BaseMap[pixel];

                return lerp(_ChromaKeyColor, color, color.a);
            }
            ENDHLSL
        }
    }
}
