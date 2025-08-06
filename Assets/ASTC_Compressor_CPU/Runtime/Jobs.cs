using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace LIBII
{
    [BurstCompile]
    public struct CompressorJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<Color32> source;

        [WriteOnly] public NativeArray<uint4> result;

        public CompressInfo ci;

        public int BlockCount => ci.BlockCount;

        public void Execute(int index)
        {
            // read block
            var blockData = Methods.ReadBlockRGBA(ci, source, index);

            // compress
            result[index] = Methods.EncodeBlock(ref blockData, ci);
        }


        public Span<byte> GetResult()
        {
            var bytes = MemoryMarshal.Cast<uint4, byte>(result.AsSpan());
            return bytes;
        }

        public void Dispose()
        {
            source.Dispose();
            result.Dispose();
        }


        public static CompressorJob Create(Texture2D tex, ASTC_BLOCKSIZE blockSize, bool hasAlpha = true)
        {
            var ci = new CompressInfo()
            {
                dim = blockSize,
                blockSize = (int)blockSize,
                hasAlpha = hasAlpha,
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