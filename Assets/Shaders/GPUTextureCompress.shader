﻿Shader "Unlit/GPUTextureCompress"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #pragma multi_compile_local BLOCK_SIZE_4x4 BLOCK_SIZE_5x5 BLOCK_SIZE_6x6 BLOCK_SIZE_8x8 BLOCK_SIZE_10x10 BLOCK_SIZE_12x12
            #pragma multi_compile __ _GPU_COMPRESS_SRGB 

            #include "GPUTextureCompress.hlsl"
            
            // #pragma enable_d3d11_debug_symbols

            struct appdata
            {
                float3 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = float4(v.vertex, 1.0);
                return o;
            }
            
            uint4 frag (v2f i) : SV_Target
            {
                return Compress(floor(i.vertex.xy) * DIM);
            }
            ENDHLSL
        }
    }
}
