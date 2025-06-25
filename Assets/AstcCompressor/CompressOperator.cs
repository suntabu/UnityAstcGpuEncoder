using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace LIBII
{
    public class AstcCompressOperator
    {
        private const string BLOCK_SIZE_4x4 = "BLOCK_SIZE_4x4";
        private const string BLOCK_SIZE_5x5 = "BLOCK_SIZE_5x5";
        private const string BLOCK_SIZE_6x6 = "BLOCK_SIZE_6x6";
        private const string BLOCK_SIZE_8x8 = "BLOCK_SIZE_8x8";
        private const string BLOCK_SIZE_10x10 = "BLOCK_SIZE_10x10";
        private const string BLOCK_SIZE_12x12 = "BLOCK_SIZE_12x12";


        // private static readonly int Result = Shader.PropertyToID("Result");
        private static readonly int BaseTexture = Shader.PropertyToID("_BaseTexture");
        private static readonly int CompressSize = Shader.PropertyToID("CompressSize");
        private static Texture2D baseTexture;

        private static ComputeShader compressASTCCS = Resources.Load<ComputeShader>("ASTC_Compress");

        // private static RenderTexture compressTempRT;
        // private static Texture2D TestRT;
        private static CommandBuffer cmd;

        private static ComputeBuffer outputBuffer;
        private static int totalBlocks; // 全局变量用于调试
        private static readonly int AstcBlock = Shader.PropertyToID("astcBlock");
        private static readonly int BlockCountX = Shader.PropertyToID("blockCountX");
        private static readonly int BlockCountY = Shader.PropertyToID("blockCountY");
        private static readonly int IsLinear = Shader.PropertyToID("isLinear");
        private static readonly int OutputBuffer = Shader.PropertyToID("OutputBuffer");
        private static readonly int ScrambleTable = Shader.PropertyToID("ScrambleTable");

        private static ComputeBuffer scrambleTableBuffer;

        private static Action<bool, byte[]> _onGetRawDataFinished;

        private static bool IsComputeShaderValid
        {
            get
            {
                bool isValid = SystemInfo.supportsComputeShaders &&
                               SystemInfo.graphicsShaderLevel >= 50 &&
                               SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_6x6) &&
                               IsGLSLVersionValid();
                return isValid;
            }
        }

        /// <summary>
        /// 准备
        /// </summary>
        private static void Prepare(Texture2D tex, int astcBlock, Action<bool, byte[]> onGetRawDataFinished)
        {
            baseTexture = tex;
            _onGetRawDataFinished = onGetRawDataFinished;

            if (!IsComputeShaderValid)
            {
                Debug.LogError("Compute Shader not support");
                return;
            }

            cmd = new CommandBuffer() { name = "Compress" };

            // var textureFormat = astcBlock switch
            // {
            //     4 => TextureFormat.ASTC_4x4,
            //     5 => TextureFormat.ASTC_5x5,
            //     6 => TextureFormat.ASTC_6x6,
            //     8 => TextureFormat.ASTC_8x8,
            //     10 => TextureFormat.ASTC_10x10,
            //     12 => TextureFormat.ASTC_12x12,
            //     _ => baseTexture.format
            // };
            // TestRT = new Texture2D(baseTexture.width, baseTexture.height, textureFormat, false);
            // TestRT.Apply(false, true);
            //
            // compressTempRT = new RenderTexture(baseTexture.width / astcBlock, baseTexture.height / astcBlock, 0)
            // {
            //     graphicsFormat = GraphicsFormat.R32G32B32A32_UInt,
            //     enableRandomWrite = true,
            // };
            // compressTempRT.Create();

            var blockCountX = (baseTexture.width + astcBlock - 1) / astcBlock;
            var blockCountY = (baseTexture.height + astcBlock - 1) / astcBlock;
            totalBlocks = blockCountX * blockCountY;

            Debug.Log($"[Prepare] DIM={astcBlock}, width={baseTexture.width}, height={baseTexture.height}");
            Debug.Log($"blockCountX={blockCountX}, blockCountY={blockCountY}");
            Debug.Log($"Effective Width={blockCountX * astcBlock}, Effective Height={blockCountY * astcBlock}");

            // var effectiveWidth = blockCountX * astcBlock;
            // var effectiveHeight = blockCountY * astcBlock;

            // TestRT = new Texture2D(effectiveWidth, effectiveHeight, textureFormat, false);
            // TestRT.Apply(false, true);
            //
            // compressTempRT = new RenderTexture(blockCountX, blockCountY, 0)
            // {
            //     format = RenderTextureFormat.ARGBFloat,
            //     enableRandomWrite = true,
            // };
            // // compressTempRT.Create();

            // 每个 ASTC Block 占 16 字节
            outputBuffer = new ComputeBuffer(totalBlocks, 16, ComputeBufferType.Default,
                ComputeBufferMode.Immutable); // stride = 16 bytes per block
        }

        static bool IsGLSLVersionValid()
        {
            string deviceVersion = SystemInfo.graphicsDeviceVersion.ToLower();
            Debug.Log($"[IsGLSLVersionValid] deviceVersion={deviceVersion}");
            // 检查是否使用 Vulkan API（通常支持 Compute Shader）
            if (deviceVersion.Contains("vulkan"))
            {
                // Vulkan 通常支持 Compute Shader，但需确保 API 版本足够新
                // 例如 Android 上的 Vulkan 1.0+ 通常支持
                return true;
            }

            // iOS 的 Metal API（iOS 9+ 支持 Compute Shader）
            if (deviceVersion.Contains("metal"))
            {
                return true;
            }

            // 检查 OpenGL ES 3.1+（支持 Compute Shader）
            if (
                // deviceVersion.Contains("opengl es 3.1") ||
                deviceVersion.Contains("opengl es 3.2"))
            {
                return true;
            }


            return false;
        }

        private static void Process(int astcBlock)
        {
            if (!IsComputeShaderValid)
            {
                if (_onGetRawDataFinished != null)
                {
                    var byteArray = baseTexture.GetRawTextureData();

                    _onGetRawDataFinished(false, byteArray);
                }

                return;
            }

            cmd.Clear();

            var computeShader = compressASTCCS;
            string keyword = astcBlock switch
            {
                4 => BLOCK_SIZE_4x4,
                5 => BLOCK_SIZE_5x5,
                6 => BLOCK_SIZE_6x6,
                8 => BLOCK_SIZE_8x8,
                10 => BLOCK_SIZE_10x10,
                12 => BLOCK_SIZE_12x12,
                _ => BLOCK_SIZE_6x6
            };

            computeShader.DisableKeyword(BLOCK_SIZE_4x4);
            computeShader.DisableKeyword(BLOCK_SIZE_5x5);
            computeShader.DisableKeyword(BLOCK_SIZE_6x6);
            computeShader.DisableKeyword(BLOCK_SIZE_8x8);
            computeShader.DisableKeyword(BLOCK_SIZE_10x10);
            computeShader.DisableKeyword(BLOCK_SIZE_12x12);

            computeShader.EnableKeyword(keyword);
            Debug.Log($"{keyword} 被激活");
            // computeShader.SetTexture(0, Result, compressTempRT);
            computeShader.SetTexture(0, BaseTexture, baseTexture);

            computeShader.SetInt(AstcBlock, astcBlock);
            // 每个线程处理一个 Block
            var blockCountX = (baseTexture.width + astcBlock - 1) / astcBlock;
            var blockCountY = (baseTexture.height + astcBlock - 1) / astcBlock;
            computeShader.SetInt(BlockCountX, blockCountX);
            computeShader.SetInt(BlockCountY, blockCountY);
            computeShader.SetInt(IsLinear, (int)QualitySettings.activeColorSpace);
            computeShader.SetVector(CompressSize, new Vector4(baseTexture.width, baseTexture.height, 0, 0));
            // 绑定buffer
            computeShader.SetBuffer(0, OutputBuffer, outputBuffer);

            // 创建 ComputeBuffer（每个元素为 int）
            int stride = sizeof(int);
            scrambleTableBuffer = new ComputeBuffer(AstcScrambleTable.ScrambleTable.Length, stride);

            // 上传数据
            scrambleTableBuffer.SetData(AstcScrambleTable.ScrambleTable);

            // 绑定到 ComputeShader
            int kernelHandle = computeShader.FindKernel("CSMain");
            if (kernelHandle < 0)
            {
                Debug.LogError("Kernel 'CSMain' not found or invalid.");
                return;
            }

            computeShader.SetBuffer(kernelHandle, ScrambleTable, scrambleTableBuffer);

            // 调度 blockCountX × blockCountY 个线程
            cmd.DispatchCompute(computeShader, kernelHandle, blockCountX, blockCountY, 1);

            // cmd.CopyTexture(compressTempRT, 0, 0, 0, 0, compressTempRT.width, compressTempRT.height, TestRT, 0, 0, 0,0);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (_onGetRawDataFinished != null)
            {
                // 异步读取 GPU 数据
                AsyncGPUReadback.Request(outputBuffer, outputBuffer.count * 16, 0, OnAstcDataReady);
            }
        }

        private static void OnAstcDataReady(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback failed");
                Debug.LogError(
                    $"是否完成 {request.done} 层数据大小 {request.layerDataSize} 宽度 {request.width} 高度 {request.height}");

                var byteArray = baseTexture.GetRawTextureData();

                _onGetRawDataFinished(false, byteArray);
            }
            else
            {
                Debug.Log("GPU readback succeeded");
                Debug.Log($"是否完成 {request.done} 层数据大小 {request.layerDataSize} 宽度 {request.width} 高度 {request.height}");
                var astcData = request.GetData<byte>();
                Debug.Log($"Expected size: {totalBlocks * 16}, Actual size: {astcData.Length}");

                var byteArray = new byte[astcData.Length];
                astcData.CopyTo(byteArray);

                _onGetRawDataFinished(true, byteArray);
            }

            if (outputBuffer.IsValid()) outputBuffer.Release();
            outputBuffer = null;
        }

        private static void Dispose()
        {
            if (IsComputeShaderValid)
            {
                cmd.Release();
                cmd = null;

                // compressTempRT.Release();
                // compressTempRT = null;

                scrambleTableBuffer?.Release();
                scrambleTableBuffer = null;
            }
        }

        /// <summary>
        /// 压缩纹理
        /// </summary>
        // public static Texture2D CompressRetTexture2D(Texture2D tex, int astcBlock = 6)
        // {
        //     if (!IsComputeShaderValid)
        //     {
        //         return baseTexture;
        //     }
        //
        //     Prepare(tex, astcBlock, null);
        //     Process(astcBlock);
        //     Dispose();
        //
        //
        //     return TestRT;
        // }

        /// <summary>
        /// 压缩纹理并获取CPU下的RawData数据
        /// </summary>
        public static void CompressRetRawData(Texture2D tex, Action<bool, byte[]> onFinished, int astcBlock = 6)
        {
            Prepare(tex, astcBlock, onFinished);
            Process(astcBlock);
            Dispose();
        }
    }
}