using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using ASTCEncoder;

namespace LIBII
{
    [BurstCompile]
    public struct CompressorJob<T> : IJobParallelFor where T : unmanaged, INativeArray<float4>
    {
        [NativeDisableParallelForRestriction] public NativeArray<Color32> source;
        public NativeArray<T> blocks;
        [WriteOnly] public NativeArray<uint4> result;

        public CompressInfo ci;

        public int BlockCount => ci.BlockCount;

        public void Execute(int index)
        {
            // read block
            var blockData = Methods.ReadBlockRGBA<T>(ci, source, index, blocks[index]);

            // compress
            result[index] = Methods.EncodeBlock<T>(blockData, ci);
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
    }

    public class CompressorASTC
    {
        private CompressorJob<Array16<float4>> compressorJob4x4;
        private CompressorJob<Array25<float4>> compressorJob5x5;
        private CompressorJob<Array36<float4>> compressorJob6x6;

        private ASTC_BLOCKSIZE blockSize;
        private Texture2D texture;
        private bool hasAlpha;

        public CompressorASTC(Texture2D tex, ASTC_BLOCKSIZE blockSize, bool hasAlpha = true)
        {
            this.blockSize = blockSize;
            texture = tex;
            this.hasAlpha = hasAlpha;
        }

        public JobHandle Compress()
        {
            JobHandle jobHandle = default;
            switch (blockSize)
            {
                case ASTC_BLOCKSIZE.ASTC_4x4:
                    compressorJob4x4 = Create<Array16<float4>>(texture, blockSize, hasAlpha);
                    jobHandle = compressorJob4x4.Schedule(compressorJob4x4.BlockCount, 8);
                    break;
                case ASTC_BLOCKSIZE.ASTC_5x5:
                    compressorJob5x5 = Create<Array25<float4>>(texture, blockSize, hasAlpha);
                    jobHandle = compressorJob5x5.Schedule(compressorJob5x5.BlockCount, 8);
                    break;
                case ASTC_BLOCKSIZE.ASTC_6x6:
                    compressorJob6x6 = Create<Array36<float4>>(texture, blockSize, hasAlpha);
                    jobHandle = compressorJob6x6.Schedule(compressorJob6x6.BlockCount, 8);
                    break;
            }

            return jobHandle;
        }


        private CompressorJob<T> Create<T>(Texture2D tex, ASTC_BLOCKSIZE blockSize, bool hasAlpha = true)
            where T : unmanaged, INativeArray<float4>
        {
            var ci = new CompressInfo()
            {
                dim = blockSize,
                blockSize = (int)blockSize,
                hasAlpha = hasAlpha,
                textureWidth = tex.width,
                textureHeight = tex.height
            };

            NativeArray<Color32> source = new NativeArray<Color32>(tex.GetPixels32(), Allocator.Persistent);
            NativeArray<uint4> result = new NativeArray<uint4>(ci.BlockCount, Allocator.Persistent);
            CompressorJob<T> job = new CompressorJob<T>
            {
                ci = ci,
                source = source,
                result = result,
                blocks = new NativeArray<T>(ci.BlockCount, Allocator.Persistent)
            };

            return job;
        }

        public void Dispose()
        {
            switch (blockSize)
            {
                case ASTC_BLOCKSIZE.ASTC_4x4:
                    compressorJob4x4.Dispose();
                    break;
                case ASTC_BLOCKSIZE.ASTC_5x5:
                    compressorJob5x5.Dispose();
                    break;
                case ASTC_BLOCKSIZE.ASTC_6x6:
                    compressorJob6x6.Dispose();
                    break;
            }
        }

        public Span<byte> GetResult()
        {
            switch (blockSize)
            {
                case ASTC_BLOCKSIZE.ASTC_4x4:
                    return compressorJob4x4.GetResult();
                case ASTC_BLOCKSIZE.ASTC_5x5:
                    return compressorJob5x5.GetResult();
                case ASTC_BLOCKSIZE.ASTC_6x6:
                    return compressorJob6x6.GetResult();
            }

            return default;
        }
    }
}