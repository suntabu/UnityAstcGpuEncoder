using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace LIBII
{
    public class Methods
    {
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<float4> ReadBlockRGBA(CompressInfo compressInfo, NativeArray<Color32> source,
            int index)
        {
            var pixels = new NativeArray<float4>(compressInfo.blockSize * compressInfo.blockSize, Allocator.Temp);

            var dim = (int)compressInfo.blockSize;
            var blockCountX = compressInfo.BlockCountX;
            int blockX = index % blockCountX;
            int blockY = index / blockCountX;

            // 计算实际块在纹理中的像素坐标
            int pixelStartX = blockX * dim;
            int pixelStartY = blockY * dim;


            // 计算实际要提取的像素尺寸（处理边界情况）
            int actualBlockWidth = math.min(dim, compressInfo.textureWidth - pixelStartX);
            int actualBlockHeight = math.min(dim, compressInfo.textureHeight - pixelStartY);

            for (int y = 0; y < actualBlockHeight; y++)
            {
                for (int x = 0; x < actualBlockWidth; x++)
                {
                    int srcIndex = (pixelStartY + y) * compressInfo.textureWidth + (pixelStartX + x);
                    int dstIndex = y * actualBlockWidth + x;

                    if (srcIndex < source.Length && dstIndex < pixels.Length)
                    {
                        var p = source[srcIndex];
                        pixels[dstIndex] = new float4(p.r, p.g, p.b, p.a);
                    }
                }
            }

            return pixels;
        }

        [BurstCompile]
        public static uint4 EncodeBlock(NativeArray<float4> texels, CompressInfo compressInfo)
        {
            principal_component_analysis(compressInfo, texels, out var ep0, out var ep1);
            //max_accumulation_pixel_direction(texels, ep0, ep1);

            // endpoints_quant是根据整个128bits减去weights的编码占用和其他配置占用后剩余的bits位数来确定的。
            // for fast compression!
            uint4 best_blockmode;
            if (compressInfo.hasAlpha)
                best_blockmode = new uint4(CC.QUANT_6, CC.QUANT_256, 6, 7);
            else
                best_blockmode = new uint4(CC.QUANT_12, CC.QUANT_256, 12, 7);


//#if !FAST
//	choose_best_quantmethod(texels, ep0, ep1, best_blockmode);
//#endif

            uint weight_quantmethod = best_blockmode.x;
            uint endpoint_quantmethod = best_blockmode.y;
            uint weight_range = best_blockmode.z;
            uint colorquant_index = best_blockmode.w;

            // reference to arm astc encoder "symbolic_to_physical"
            //uint bytes_of_one_endpoint = 2 * (color_endpoint_mode >> 2) + 2;

            uint blockmode = assemble_blockmode(weight_quantmethod);

            uint4 ep_ise = endpoint_ise(compressInfo, colorquant_index, ep0, ep1, endpoint_quantmethod);

            uint4 wt_ise = weight_ise(compressInfo, texels, weight_range - 1, ep0, ep1, weight_quantmethod);

            // assemble to astcblock
            uint color_endpoint_mode;
            if (compressInfo.hasAlpha)
                color_endpoint_mode = CC.CEM_LDR_RGBA_DIRECT;
            else
                color_endpoint_mode = CC.CEM_LDR_RGB_DIRECT;


            return assemble_block(blockmode, color_endpoint_mode, 1, 0, ep_ise, wt_ise);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void principal_component_analysis(CompressInfo ci, NativeArray<float4> texels, out float4 e0,
            out float4 e1)
        {
            int i = 0;
            float4 pt_mean = 0;
            var blockSize = texels.Length;
            for (i = 0; i < blockSize; ++i)
            {
                pt_mean += texels[i];
            }

            pt_mean /= blockSize;

            float[] cov = new float[16];


            for (int k = 0; k < blockSize; ++k)
            {
                float4 texel = texels[k] - pt_mean;


                for (i = 0; i < 4; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                    {
                        cov[(i) * 4 + (j)] += texel[i] * texel[j];
                    }
                }
            }


            for (int q = 0; q < 16; ++q)
            {
                cov[q] /= blockSize - 1;
            }

            // 将 cov 数组重新组合成 float4x4（如果后续函数需要）
            float4x4 covMatrix = new float4x4(
                new float4(cov[0], cov[1], cov[2], cov[3]),
                new float4(cov[4], cov[5], cov[6], cov[7]),
                new float4(cov[8], cov[9], cov[10], cov[11]),
                new float4(cov[12], cov[13], cov[14], cov[15])
            );

            // 继续 PCA 计算...
            float4 vec_k = eigen_vector(covMatrix);

            find_min_max(ci, texels, pt_mean, vec_k, out e0, out e1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint assemble_blockmode(uint weight_quantmethod)
        {
/*
    the first row of "Table C.2.8 - 2D Block Mode Layout".
    ------------------------------------------------------------------------
    10  9   8   7   6   5   4   3   2   1   0   Width Height Notes
    ------------------------------------------------------------------------
    D   H     B       A     R0  0   0   R2  R1  B + 4   A + 2
*/

            uint a = (CC.Y_GRIDS - 2) & 0x3;
            uint b = (CC.X_GRIDS - 4) & 0x3;

            uint d = 0; // dual plane

            // more details from "Table C.2.7 - Weight Range Encodings"	
            uint h = (uint)((weight_quantmethod < 6) ? 0 : 1); // "a precision bit H"
            uint r = (weight_quantmethod % 6) + 2; // "The weight ranges are encoded using a 3 bit value R"

            // block mode
            uint blockmode = (r >> 1) & 0x3;
            blockmode |= (r & 0x1) << 4;
            blockmode |= (a & 0x3) << 5;
            blockmode |= (b & 0x3) << 7;
            blockmode |= h << 9;
            blockmode |= d << 10;
            return blockmode;
        }

        static uint4 endpoint_ise(CompressInfo ci, uint colorquant_index, float4 ep0, float4 ep1,
            uint endpoint_quantmethod)
        {
            // encode endpoints
            uint[] ep_quantized = new uint[8];
            encode_color(colorquant_index, ep0, ep1, ref ep_quantized);

            if (!ci.hasAlpha)
            {
                ep_quantized[6] = 255;
                ep_quantized[7] = 255;
            }

            // endpoints quantized ise encode
            uint4 ep_ise = 0;
            bise_endpoints(ci, ep_quantized, endpoint_quantmethod, ref ep_ise);
            return ep_ise;
        }

        static void encode_color(uint qm_index, float4 e0, float4 e1, ref uint[] endpoint_quantized)
        {
            uint4 e0q = (uint4)round(e0);
            uint4 e1q = (uint4)round(e1);
            endpoint_quantized[0] = e0q.x;
            endpoint_quantized[1] = e1q.x;
            endpoint_quantized[2] = e0q.y;
            endpoint_quantized[3] = e1q.y;
            endpoint_quantized[4] = e0q.z;
            endpoint_quantized[5] = e1q.z;
            endpoint_quantized[6] = e0q.w;
            endpoint_quantized[7] = e1q.w;
        }

        static uint4 weight_ise(CompressInfo ci, NativeArray<float4> texels, uint weight_range, float4 ep0, float4 ep1,
            uint weight_quantmethod)
        {
            int i = 0;
            uint c = CC.X_GRIDS * CC.Y_GRIDS;
            // encode weights
            uint[] wt_quantized = new uint[c];
            calculate_quantized_weights(ci, texels, weight_range, ep0, ep1, ref wt_quantized);


            for (i = 0; i < c; ++i)
            {
                uint w = weight_quantmethod * CC.WEIGHT_QUANTIZE_NUM + wt_quantized[i];
                // wt_quantized[i] = scramble_table[w];
                wt_quantized[i] = CC.ScrambleTable[w];
            }

            // weights quantized ise encode
            uint4 wt_ise = 0;
            bise_weights(wt_quantized, weight_quantmethod, ref wt_ise);
            return wt_ise;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// encode single partition
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// candidate blockmode uint4(weights quantmethod, endpoints quantmethod, weights range, endpoints quantmethod index of table)

        static uint4 assemble_block(uint blockmode, uint color_endpoint_mode, uint partition_count,
            uint partition_index,
            uint4 ep_ise, uint4 wt_ise)
        {
            uint4 phy_blk = new uint4(0, 0, 0, 0);
            // weights ise
            phy_blk.w |= reverse_byte(wt_ise.x & 0xFF) << 24;
            phy_blk.w |= reverse_byte((wt_ise.x >> 8) & 0xFF) << 16;
            phy_blk.w |= reverse_byte((wt_ise.x >> 16) & 0xFF) << 8;
            phy_blk.w |= reverse_byte((wt_ise.x >> 24) & 0xFF);

            phy_blk.z |= reverse_byte(wt_ise.y & 0xFF) << 24;
            phy_blk.z |= reverse_byte((wt_ise.y >> 8) & 0xFF) << 16;
            phy_blk.z |= reverse_byte((wt_ise.y >> 16) & 0xFF) << 8;
            phy_blk.z |= reverse_byte((wt_ise.y >> 24) & 0xFF);

            phy_blk.y |= reverse_byte(wt_ise.z & 0xFF) << 24;
            phy_blk.y |= reverse_byte((wt_ise.z >> 8) & 0xFF) << 16;
            phy_blk.y |= reverse_byte((wt_ise.z >> 16) & 0xFF) << 8;
            phy_blk.y |= reverse_byte((wt_ise.z >> 24) & 0xFF);

            // blockmode & partition count
            phy_blk.x = blockmode; // blockmode is 11 bit

            //if (partition_count > 1)
            //{
            //	uint endpoint_offset = 29;
            //	uint cem_bits = 6;
            //	uint bitpos = 13;
            //	orbits8_ptr(phy_blk, bitpos, partition_count - 1, 2);
            //	orbits8_ptr(phy_blk, bitpos, partition_index & 63, 6);
            //	orbits8_ptr(phy_blk, bitpos, partition_index >> 6, 4);
            //  ...
            //}

            // cem: color_endpoint_mode is 4 bit
            phy_blk.x |= (color_endpoint_mode & 0xF) << 13;

            // endpoints start from ( multi_part ? bits 29 : bits 17 )
            phy_blk.x |= (ep_ise.x & 0x7FFF) << 17;
            phy_blk.y = ((ep_ise.x >> 15) & 0x1FFFF);
            phy_blk.y |= (ep_ise.y & 0x7FFF) << 17;
            phy_blk.z |= ((ep_ise.y >> 15) & 0x1FFFF);

            return phy_blk;
        }


        static float4 eigen_vector(float4x4 m)
        {
            // calc the max eigen value by iteration
            float4 v = new float4(0.26726f, 0.80178f, 0.53452f, 0.0f);
            for (int i = 0; i < 8; ++i)
            {
                v = mul(m, v);
                if (length(v) < CC.SMALL_VALUE)
                {
                    return v;
                }

                v = normalize(mul(m, v));
            }

            return v;
        }

        static void find_min_max(CompressInfo ci, NativeArray<float4> texels, float4 pt_mean, float4 vec_k,
            out float4 e0,
            out float4 e1)
        {
            float a = 1e31f;
            float b = -1e31f;
            for (int i = 0; i < texels.Length; ++i)
            {
                float4 texel = texels[i] - pt_mean;
                float t = dot(texel, vec_k);
                a = min(a, t);
                b = max(b, t);
            }

            e0 = clamp(vec_k * a + pt_mean, 0.0f, 255.0f);
            e1 = clamp(vec_k * b + pt_mean, 0.0f, 255.0f);

            // if the direction-vector ends up pointing from light to dark, FLIP IT!
            // this will make the first endpoint the darkest one.
            float4 e0u = round(e0);
            float4 e1u = round(e1);
            if (e0u.x + e0u.y + e0u.z > e1u.x + e1u.y + e1u.z)
            {
                swap(ref e0, ref e1);
            }


            if (!ci.hasAlpha)
            {
                e0.w = 255.0f;
                e1.w = 255.0f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void swap(ref float4 lhs, ref float4 rhs)
        {
            (lhs, rhs) = (rhs, lhs);
        }

        static void bise_endpoints(CompressInfo ci, uint[] numbers, uint range, ref uint4 outputs)
        {
            uint bitpos = 0;
            uint bits = CC.bits_trits_quints_table[range * 3 + 0];
            uint trits = CC.bits_trits_quints_table[range * 3 + 1];
            uint quints = CC.bits_trits_quints_table[range * 3 + 2];

            int count = 6;
            if (ci.hasAlpha)
            {
                count = 8;
            }

            if (trits == 1)
            {
                encode_trits(bits, numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], ref outputs, ref bitpos);
                encode_trits(bits, numbers[5], numbers[6], numbers[7], 0, 0, ref outputs, ref bitpos);
                bitpos = (uint)(((8 + 5 * bits) * count + 4) / 5);
            }
            else if (quints == 1)
            {
                encode_quints(bits, numbers[0], numbers[1], numbers[2], ref outputs, ref bitpos);
                encode_quints(bits, numbers[3], numbers[4], numbers[5], ref outputs, ref bitpos);
                encode_quints(bits, numbers[6], numbers[7], 0, ref outputs, ref bitpos);
                bitpos = (uint)(((7 + 3 * bits) * count + 2) / 3);
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    orbits8_ptr(ref outputs, ref bitpos, numbers[i], bits);
                }
            }
        }

        static void calculate_quantized_weights(
            CompressInfo ci,
            NativeArray<float4> texels,
            uint weight_range,
            float4 ep0,
            float4 ep1,
            ref uint[] weights)
        {
            float[] projw = new float[CC.X_GRIDS * CC.Y_GRIDS];
            calculate_normal_weights(ci, texels, ep0, ep1, ref projw);
            quantize_weights(projw, weight_range, ref weights);
        }

        static void bise_weights(uint[] nums, uint range, ref uint4 outputs)
        {
            uint bitpos = 0;
            uint bits = CC.bits_trits_quints_table[range * 3 + 0];
            uint trits = CC.bits_trits_quints_table[range * 3 + 1];
            uint quints = CC.bits_trits_quints_table[range * 3 + 2];

            if (trits == 1)
            {
                encode_trits(bits, nums[0], nums[1], nums[2], nums[3], nums[4], ref outputs, ref bitpos);
                encode_trits(bits, nums[5], nums[6], nums[7], nums[8], nums[9], ref outputs, ref bitpos);
                encode_trits(bits, nums[10], nums[11], nums[12], nums[13], nums[14], ref outputs,
                    ref bitpos);
                encode_trits(bits, nums[15], 0, 0, 0, 0, ref outputs, ref bitpos);
                bitpos = ((8 + 5 * bits) * 16 + 4) / 5;
            }
            else if (quints == 1)
            {
                encode_quints(bits, nums[0], nums[1], nums[2], ref outputs, ref bitpos);
                encode_quints(bits, nums[3], nums[4], nums[5], ref outputs, ref bitpos);
                encode_quints(bits, nums[6], nums[7], nums[8], ref outputs, ref bitpos);
                encode_quints(bits, nums[9], nums[10], nums[11], ref outputs, ref bitpos);
                encode_quints(bits, nums[12], nums[13], nums[14], ref outputs, ref bitpos);
                encode_quints(bits, nums[15], 0, 0, ref outputs, ref bitpos);
                bitpos = ((7 + 3 * bits) * 16 + 2) / 3;
            }
            else
            {
                for (int i = 0; i < 16; ++i)
                {
                    orbits8_ptr(ref outputs, ref bitpos, nums[i], bits);
                }
            }
        }

        static uint reverse_byte(uint p)
        {
            p = ((p & 0xF) << 4) | ((p >> 4) & 0xF);
            p = ((p & 0x33) << 2) | ((p >> 2) & 0x33);
            p = ((p & 0x55) << 1) | ((p >> 1) & 0x55);
            return p;
        }

        /**
 * Encode a group of 5 numbers using trits and bits.
 */
        static void encode_trits(uint bitcount,
            uint b0,
            uint b1,
            uint b2,
            uint b3,
            uint b4,
            ref uint4 outputs, ref uint outpos)
        {
            uint t0, t1, t2, t3, t4;
            uint m0, m1, m2, m3, m4;

            split_high_low(b0, bitcount, out t0, out m0);
            split_high_low(b1, bitcount, out t1, out m1);
            split_high_low(b2, bitcount, out t2, out m2);
            split_high_low(b3, bitcount, out t3, out m3);
            split_high_low(b4, bitcount, out t4, out m4);

            uint packhigh = CC.integer_from_trits[t4 * 81 + t3 * 27 + t2 * 9 + t1 * 3 + t0];

            orbits8_ptr(ref outputs, ref outpos, m0, bitcount);
            orbits8_ptr(ref outputs, ref outpos, packhigh & 3, 2);
            orbits8_ptr(ref outputs, ref outpos, m1, bitcount);
            orbits8_ptr(ref outputs, ref outpos, (packhigh >> 2) & 3, 2);
            orbits8_ptr(ref outputs, ref outpos, m2, bitcount);
            orbits8_ptr(ref outputs, ref outpos, (packhigh >> 4) & 1, 1);
            orbits8_ptr(ref outputs, ref outpos, m3, bitcount);
            orbits8_ptr(ref outputs, ref outpos, (packhigh >> 5) & 3, 2);
            orbits8_ptr(ref outputs, ref outpos, m4, bitcount);
            orbits8_ptr(ref outputs, ref outpos, (packhigh >> 7) & 1, 1);
        }

        /**
         * Encode a group of 3 numbers using quints and bits.
         */
        static void encode_quints(uint bitcount,
            uint b0,
            uint b1,
            uint b2,
            ref uint4 outputs, ref uint outpos)
        {
            uint q0, q1, q2;
            uint m0, m1, m2;

            split_high_low(b0, bitcount, out q0, out m0);
            split_high_low(b1, bitcount, out q1, out m1);
            split_high_low(b2, bitcount, out q2, out m2);

            uint packhigh = (uint)CC.integer_from_quints[q2 * 25 + q1 * 5 + q0];

            orbits8_ptr(ref outputs, ref outpos, m0, bitcount);
            orbits8_ptr(ref outputs, ref outpos, packhigh & 7, 3);
            orbits8_ptr(ref outputs, ref outpos, m1, bitcount);
            orbits8_ptr(ref outputs, ref outpos, (packhigh >> 3) & 3, 2);
            orbits8_ptr(ref outputs, ref outpos, m2, bitcount);
            orbits8_ptr(ref outputs, ref outpos, (packhigh >> 5) & 3, 2);
        }

        /// <summary>
        ///  把number的低bitcount位写到bytes的bitoffset偏移处开始的位置
        /// number must be <= 255; bitcount must be <= 8
        /// </summary>
        /// <param name="uint4"></param>
        static void orbits8_ptr(ref uint4 outputs, ref uint bitoffset, uint number, uint bitcount)
        {
            //bitcount = clamp(bitcount, 0, 8);
            //number &= (1 << bitcount) - 1;
            uint newpos = bitoffset + bitcount;

            uint nidx = newpos >> 5;
            uint uidx = bitoffset >> 5;
            uint bit_idx = bitoffset & 31;

            uint[] bytes = { outputs.x, outputs.y, outputs.z, outputs.w };
            bytes[uidx] |= (number << (int)bit_idx);
            bytes[uidx + 1] |= (uint)((nidx > uidx) ? ((int)number >> (int)(32 - bit_idx)) : 0);

            outputs.x = bytes[0];
            outputs.y = bytes[1];
            outputs.z = bytes[2];
            outputs.w = bytes[3];

            bitoffset = newpos;
        }


        static void calculate_normal_weights(CompressInfo ci, NativeArray<float4> texels,
            float4 ep0,
            float4 ep1,
            ref float[] projw)
        {
            int i = 0;
            float4 vec_k = ep1 - ep0;

            var c = CC.X_GRIDS * CC.Y_GRIDS;
            if (length(vec_k) < CC.SMALL_VALUE)
            {
                for (i = 0; i < c; ++i)
                {
                    projw[i] = 0;
                }
            }
            else
            {
                vec_k = normalize(vec_k);
                float minw = 1e31f;
                float maxw = -1e31f;

                if (ci.dim == ASTC_BLOCKSIZE.ASTC_6x6)
                {
                    for (i = 0; i < c; ++i)
                    {
                        float4 sum = sample_texel(texels, CC.idx_grids_6x6[i], CC.wt_grids_6x6[i]);
                        float w = dot(vec_k, sum - ep0);
                        minw = min(w, minw);
                        maxw = max(w, maxw);
                        projw[i] = w;
                    }
                }
                else if (ci.dim == ASTC_BLOCKSIZE.ASTC_5x5)
                {
                    for (i = 0; i < c; ++i)
                    {
                        float4 sum = sample_texel(texels, CC.idx_grids_5x5[i], CC.wt_grids_5x5[i]);
                        float w = dot(vec_k, sum - ep0);
                        minw = min(w, minw);
                        maxw = max(w, maxw);
                        projw[i] = w;
                    }
                }
                else
                {
                    // 使用 blockSize 控制最大迭代数
                    uint MAX_LOOPS = min(c, (uint)texels.Length);


                    for (i = 0; i < MAX_LOOPS; ++i)
                    {
                        float4 texel = texels[i];
                        float w = dot(vec_k, texel - ep0);
                        minw = min(w, minw);
                        maxw = max(w, maxw);
                        projw[i] = w;
                    }
                }


                float invlen = max(CC.SMALL_VALUE, maxw - minw);
                invlen = 1.0f / invlen;


                for (i = 0; i < c; ++i)
                {
                    // ✅ 加上边界判断防止越界
                    if (i >= c)
                        continue;

                    projw[i] = (projw[i] - minw) * invlen;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 sample_texel(NativeArray<float4> texels, uint4 index, float4 coff)
        {
            float4 sum = texels[(int)index.x] * coff.x;
            sum += texels[(int)index.y] * coff.y;
            sum += texels[(int)index.z] * coff.z;
            sum += texels[(int)index.w] * coff.w;
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void quantize_weights(float[] projw,
            uint weight_range,
            ref uint[] weights)
        {
            var c = CC.X_GRIDS * CC.Y_GRIDS;
            for (int i = 0; i < c; ++i)
            {
                weights[i] = quantize_weight(weight_range, projw[i]);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void split_high_low(uint n, uint i, out uint high, out uint low)
        {
            uint low_mask = (uint)((1 << (int)i) - 1);
            low = n & low_mask;
            high = (uint)((int)n >> (int)i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint quantize_weight(uint weight_range, float weight)
        {
            uint q = (uint)round(weight * weight_range);
            return clamp(q, 0, weight_range);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float dot(float4 a, float4 b)
        {
            return math.dot(a, b);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float round(float x)
        {
            return math.round(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 round(float4 x)
        {
            return math.round(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 clamp(float4 x, float min, float max)
        {
            return math.clamp(x, min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float clamp(float x, float min, float max)
        {
            return math.clamp(x, min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint clamp(uint x, uint min, uint max)
        {
            return math.clamp(x, min, max);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float length(float4 v)
        {
            return math.length(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 normalize(float4 v)
        {
            return math.normalize(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint min(uint a, uint b)
        {
            return math.min(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int min(int a, int b)
        {
            return math.min(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float min(float a, float b)
        {
            return math.min(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint max(uint a, uint b)
        {
            return math.max(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float max(float a, float b)
        {
            return math.max(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint4 max(uint4 a, uint4 b)
        {
            return math.max(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint4 min(uint4 a, uint4 b)
        {
            return math.min(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float mul(float4 a, float4 b)
        {
            return math.mul(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 mul(float4x4 a, float4 b)
        {
            return math.mul(a, b);
        }
    }
}