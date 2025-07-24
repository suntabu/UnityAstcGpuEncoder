using Unity.Mathematics;

namespace ASTCEncoder
{
    public enum ASTC_BLOCKSIZE
    {
        ASTC_4x4 = 4,

        // ASTC_5x5 = 5,
        ASTC_6x6 = 6,
        // ASTC_8x8 = 8,
        // ASTC_10x10 = 10,
        // ASTC_12x12 = 12,
    }

    public struct CompressInfo
    {
        public ASTC_BLOCKSIZE dim;
        public int blockSize;
        public bool hasAlpha;
        public int textureWidth;
        public int textureHeight;


        public int BlockCountX => textureWidth / blockSize + (textureWidth % blockSize == 0 ? 0 : 1);

        public int BlockCountY => textureHeight / blockSize + (textureHeight % blockSize == 0 ? 0 : 1);

        public int BlockCount => BlockCountX * BlockCountY;
    }

    public class CC
    {
        public static float SMALL_VALUE = 0.00001f;
        public static uint Y_GRIDS = 4;
        public static uint X_GRIDS = 4;
        public static uint CEM_LDR_RGB_DIRECT = 8;
        public static uint CEM_LDR_RGBA_DIRECT = 12;
        public const int k_WEIGHT_QUANTIZE_NUM = 32;
        public const int k_BLOCK_BYTES = 32;


        public const int QUANT_2 = 0;
        public const int QUANT_3 = 1;
        public const int QUANT_4 = 2;
        public const int QUANT_5 = 3;
        public const int QUANT_6 = 4;
        public const int QUANT_8 = 5;
        public const int QUANT_10 = 6;
        public const int QUANT_12 = 7;
        public const int QUANT_16 = 8;
        public const int QUANT_20 = 9;
        public const int QUANT_24 = 10;
        public const int QUANT_32 = 11;
        public const int QUANT_40 = 12;
        public const int QUANT_48 = 13;
        public const int QUANT_64 = 14;
        public const int QUANT_80 = 15;
        public const int QUANT_96 = 16;
        public const int QUANT_128 = 17;
        public const int QUANT_160 = 18;
        public const int QUANT_192 = 19;
        public const int QUANT_256 = 20;
        public const int QUANT_MAX = 21;

        public static readonly uint[] bits_trits_quints_table =
        {
            1, 0, 0, // RANGE_2
            0, 1, 0, // RANGE_3
            2, 0, 0, // RANGE_4
            0, 0, 1, // RANGE_5
            1, 1, 0, // RANGE_6
            3, 0, 0, // RANGE_8
            1, 0, 1, // RANGE_10
            2, 1, 0, // RANGE_12
            4, 0, 0, // RANGE_16
            2, 0, 1, // RANGE_20
            3, 1, 0, // RANGE_24
            5, 0, 0, // RANGE_32
            3, 0, 1, // RANGE_40
            4, 1, 0, // RANGE_48
            6, 0, 0, // RANGE_64
            4, 0, 1, // RANGE_80
            5, 1, 0, // RANGE_96
            7, 0, 0, // RANGE_128
            5, 0, 1, // RANGE_160
            6, 1, 0, // RANGE_192
            8, 0, 0 // RANGE_256
        };

        public static readonly uint[] integer_from_trits =
        {
            0, 1, 2, 4, 5, 6, 8, 9, 10,
            16, 17, 18, 20, 21, 22, 24, 25, 26,
            3, 7, 15, 19, 23, 27, 12, 13, 14,
            32, 33, 34, 36, 37, 38, 40, 41, 42,
            48, 49, 50, 52, 53, 54, 56, 57, 58,
            35, 39, 47, 51, 55, 59, 44, 45, 46,
            64, 65, 66, 68, 69, 70, 72, 73, 74,
            80, 81, 82, 84, 85, 86, 88, 89, 90,
            67, 71, 79, 83, 87, 91, 76, 77, 78,

            128, 129, 130, 132, 133, 134, 136, 137, 138,
            144, 145, 146, 148, 149, 150, 152, 153, 154,
            131, 135, 143, 147, 151, 155, 140, 141, 142,
            160, 161, 162, 164, 165, 166, 168, 169, 170,
            176, 177, 178, 180, 181, 182, 184, 185, 186,
            163, 167, 175, 179, 183, 187, 172, 173, 174,
            192, 193, 194, 196, 197, 198, 200, 201, 202,
            208, 209, 210, 212, 213, 214, 216, 217, 218,
            195, 199, 207, 211, 215, 219, 204, 205, 206,

            96, 97, 98, 100, 101, 102, 104, 105, 106,
            112, 113, 114, 116, 117, 118, 120, 121, 122,
            99, 103, 111, 115, 119, 123, 108, 109, 110,
            224, 225, 226, 228, 229, 230, 232, 233, 234,
            240, 241, 242, 244, 245, 246, 248, 249, 250,
            227, 231, 239, 243, 247, 251, 236, 237, 238,
            28, 29, 30, 60, 61, 62, 92, 93, 94,
            156, 157, 158, 188, 189, 190, 220, 221, 222,
            31, 63, 127, 159, 191, 255, 252, 253, 254
        };

        public static readonly int[] integer_from_quints = new[]
        {
            0, 1, 2, 3, 4, 8, 9, 10, 11, 12, 16, 17, 18, 19, 20, 24, 25, 26, 27, 28, 5, 13, 21, 29, 6,
            32, 33, 34, 35, 36, 40, 41, 42, 43, 44, 48, 49, 50, 51, 52, 56, 57, 58, 59, 60, 37, 45, 53, 61, 14,
            64, 65, 66, 67, 68, 72, 73, 74, 75, 76, 80, 81, 82, 83, 84, 88, 89, 90, 91, 92, 69, 77, 85, 93, 22,
            96, 97, 98, 99, 100, 104, 105, 106, 107, 108, 112, 113, 114, 115, 116, 120, 121, 122, 123, 124, 101, 109,
            117,
            125, 30,
            102, 103, 70, 71, 38, 110, 111, 78, 79, 46, 118, 119, 86, 87, 54, 126, 127, 94, 95, 62, 39, 47, 55, 63, 31
        };

        public static uint4[] idx_grids = new uint4[]
        {
            new uint4(0, 1, 6, 7),
            new uint4(1, 2, 7, 8),
            new uint4(3, 4, 9, 10),
            new uint4(4, 5, 10, 11),
            new uint4(6, 7, 12, 13),
            new uint4(7, 8, 13, 14),
            new uint4(9, 10, 15, 16),
            new uint4(10, 11, 16, 17),
            new uint4(18, 19, 24, 25),
            new uint4(19, 20, 25, 26),
            new uint4(21, 22, 27, 28),
            new uint4(22, 23, 28, 29),
            new uint4(24, 25, 30, 31),
            new uint4(25, 26, 31, 32),
            new uint4(27, 28, 33, 34),
            new uint4(28, 29, 34, 35),
        };

        public static float4[] wt_grids =
        {
            new float4(0.444f, 0.222f, 0.222f, 0.111f),
            new float4(0.222f, 0.444f, 0.111f, 0.222f),
            new float4(0.444f, 0.222f, 0.222f, 0.111f),
            new float4(0.222f, 0.444f, 0.111f, 0.222f),
            new float4(0.222f, 0.111f, 0.444f, 0.222f),
            new float4(0.111f, 0.222f, 0.222f, 0.444f),
            new float4(0.222f, 0.111f, 0.444f, 0.222f),
            new float4(0.111f, 0.222f, 0.222f, 0.444f),
            new float4(0.444f, 0.222f, 0.222f, 0.111f),
            new float4(0.222f, 0.444f, 0.111f, 0.222f),
            new float4(0.444f, 0.222f, 0.222f, 0.111f),
            new float4(0.222f, 0.444f, 0.111f, 0.222f),
            new float4(0.222f, 0.111f, 0.444f, 0.222f),
            new float4(0.111f, 0.222f, 0.222f, 0.444f),
            new float4(0.222f, 0.111f, 0.444f, 0.222f),
            new float4(0.111f, 0.222f, 0.222f, 0.444f),
        };


        public const uint WEIGHT_QUANTIZE_NUM = 32;

        /// <summary>
        /// ASTC 加扰表(Scramble Table)
        /// 复制自 ASTC_Table.hlsl
        /// 
        /// 注意：由于 Unity 的 HLSLcc 编译器在 GLES3/Vulkan 下无法正确处理大型常量数组，
        /// 这里将加扰表从 Shader 移到 C# 端，通过 StructuredBuffer<float4> 上传到 GPU。
        /// 
        /// 技术背景：
        /// - 原始 HLSL 中 12*32=384 个常量的数组会导致编译警告：
        ///   "HLSLcc: Large chunk of constant data detected"
        /// - 小规模数组(数量暂时不清楚，暂定少于384个元素)不会触发此问题
        /// </summary>
        public static readonly uint[] ScrambleTable = new uint[384]
        {
            // quantization method 0, range 0..1
            //{
            0, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 1, range 0..2
            //{
            0, 1, 2,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 2, range 0..3
            //{
            0, 1, 2, 3,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 3, range 0..4
            //{
            0, 1, 2, 3, 4,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 4, range 0..5
            //{
            0, 2, 4, 5, 3, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 5, range 0..7
            //{
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 6, range 0..9
            //{
            0, 2, 4, 6, 8, 9, 7, 5, 3, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 7, range 0..11
            //{
            0, 4, 8, 2, 6, 10, 11, 7, 3, 9, 5, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 8, range 0..15
            //{
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 9, range 0..19
            //{
            0, 4, 8, 12, 16, 2, 6, 10, 14, 18, 19, 15, 11, 7, 3, 17, 13, 9, 5, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 10, range 0..23
            //{
            0, 8, 16, 2, 10, 18, 4, 12, 20, 6, 14, 22, 23, 15, 7, 21, 13, 5, 19,
            11, 3, 17, 9, 1, 0, 0, 0, 0, 0, 0, 0, 0,
            //},
            // quantization method 11, range 0..31
            //{
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
            20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            //}
        };
    }
}