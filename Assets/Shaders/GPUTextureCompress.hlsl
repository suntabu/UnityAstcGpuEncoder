// #include "ASTCCompress.hlsl"
#include "ASTC_Encode.hlsl"

Texture2D<float4> _CompressSourceTexture;
SamplerState sampler_CompressSourceTexture;
int _CompressSourceTexture_MipLevel;
float4 _DestRect;

inline half3 LinearToGammaSpace (half3 linRGB)
{
     linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
     return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);
}
// UV是Block最左下角的位置
void ReadBlockRGBA(Texture2D<float4> SourceTexture, SamplerState TextureSampler, int mipLevel, float2 UV, float2 TexelUVSize, out float4 Block[BLOCK_SIZE])
{
    [unroll]
    for (int y = 0; y < DIM; ++y)
    {
        [unroll]
        for (int x = 0; x < DIM; ++x)
        {
            float4 texel = SourceTexture.SampleLevel(TextureSampler, UV + float2(x, y) * TexelUVSize, mipLevel).rgba;
#if IS_NORMALMAP
            texel.b = 1.0f;
            texel.a = 1.0f;
#endif

#ifdef _GPU_COMPRESS_SRGB
            // Linear -> SRGB
            texel.rgb = LinearToGammaSpace(texel.rgb);
#endif
            Block[DIM * y + x] = texel * 255.0f;        
        }
    }
}

uint4 Compress(float2 SamplePos)
{
    float2 TexelUVSize = _DestRect.zw;
    float2 SampleUV = (SamplePos + float2(0.5, 0.5)) * TexelUVSize;

    float4 BlockBaseColor[BLOCK_SIZE];
    ReadBlockRGBA(_CompressSourceTexture, sampler_CompressSourceTexture, _CompressSourceTexture_MipLevel, SampleUV, TexelUVSize, BlockBaseColor);

    // ASTC
    uint4 result = encode_block(BlockBaseColor);
    return result;
}
