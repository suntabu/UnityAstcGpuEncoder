using Lazyun.FixedTypes;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ASTCEncoder
{
    public struct CompressorJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<Color32> source;

        [WriteOnly] public NativeArray<uint4> result;

        public CompressInfo ci;


        public void Execute(int index)
        {
            // read block
            var blockData = Methods.ReadBlockRGBA(ci, source, index);

            // compress
            result[index] = Methods.EncodeBlock(blockData, ci);
        }

        public static CompressorJob Create(Texture2D tex, ASTC_BLOCKSIZE blockSize)
        {
            var ci = new CompressInfo()
            {
                dim = blockSize,
                blockSize = (int)blockSize,
                hasAlpha = true,
                textureWidth = tex.width,
                textureHeight = tex.height
            };
            var job = new CompressorJob
            {
                ci = ci,
                source = new NativeArray<Color32>(tex.GetPixels32(), Allocator.TempJob),
                result = new NativeArray<uint4>(ci.BlockCount, Allocator.TempJob)
            };
            return job;
        }
    }
}