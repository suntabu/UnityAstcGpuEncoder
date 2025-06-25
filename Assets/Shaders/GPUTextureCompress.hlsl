// #include "ASTCCompress.hlsl"
#include "ASTC_Encode.hlsl"

Texture2D<float4> _CompressSourceTexture;
SamplerState sampler_CompressSourceTexture;
int _CompressSourceTexture_MipLevel;
float4 _DestRect;
RWTexture2D<float4> _ResultDecompressed : register(u1);

// UV是Block最左下角的位置
void ReadBlockRGBA(Texture2D<float4> SourceTexture, SamplerState TextureSampler, int mipLevel, float2 UV, float2 TexelUVSize, out float4 Block[BLOCK_SIZE])
{
    [unroll]
    for (int y = 0; y < DIM; ++y)
    {
        [unroll]
        for (int x = 0; x < DIM; ++x)
        {
            Block[DIM * y + x] = SourceTexture.SampleLevel(TextureSampler, UV + float2(x, y) * TexelUVSize, mipLevel).rgba;        
        }
    }
}

void WriteDecompressedResult(int2 SamplePos, float4 BlockBaseColor[BLOCK_SIZE])
{
#if _DECOMPRESS_RGB
    for (int y = 0; y < DIM; ++y)
    {
        for (int x = 0; x < DIM; ++x)
        {
            _ResultDecompressed[SamplePos + int2(x, y)] = float4(BlockBaseColor[x + y * DIM], 1.0);
        }
    }
#endif
}

uint4 Compress(float2 SamplePos)
{
    float2 TexelUVSize = _DestRect.zw;
    float2 SampleUV = (SamplePos + float2(0.5, 0.5)) * TexelUVSize;

    float4 BlockBaseColor[BLOCK_SIZE];
    ReadBlockRGBA(_CompressSourceTexture, sampler_CompressSourceTexture, _CompressSourceTexture_MipLevel, SampleUV, TexelUVSize, BlockBaseColor);

#ifdef _GPU_COMPRESS_SRGB
    // Linear -> SRGB
    for (int i = 0; i < BLOCK_SIZE; ++i)
    {
        float3 linearColor = BlockBaseColor[i];
        BlockBaseColor[i] = linearColor <= 0.0031308f ? 12.92f * linearColor : 1.055F * pow(linearColor, 0.4166667F) - 0.055F;
    }
#endif

    // ASTC
    uint4 result = encode_block(BlockBaseColor);
    // WriteDecompressedResult((int2)SamplePos, BlockBaseColor);
    return result;
}
