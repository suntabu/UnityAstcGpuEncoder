using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ASTCEncoder
{
    public enum ASTC_BLOCKSIZE
    {
        ASTC_4x4 = 1,

        // ASTC_5x5 = 2,
        ASTC_6x6 = 3,
        // ASTC_8x8 = 4,
        // ASTC_10x10 = 5,
        // ASTC_12x12 = 6,
    }


    public class GPUTextureCompressor
    {
        private const string BLOCK_SIZE_4x4 = "BLOCK_SIZE_4x4";

        // private const string BLOCK_SIZE_5x5 = "BLOCK_SIZE_5x5";
        private const string BLOCK_SIZE_6x6 = "BLOCK_SIZE_6x6";
        // private const string BLOCK_SIZE_8x8 = "BLOCK_SIZE_8x8";
        // private const string BLOCK_SIZE_10x10 = "BLOCK_SIZE_10x10";
        // private const string BLOCK_SIZE_12x12 = "BLOCK_SIZE_12x12";


        // 指定ASTC的块大小，只有当UseASTC()返回true时才有效
        public ASTC_BLOCKSIZE ASTCBlockSize { get; private set; }

        [SerializeField] private Material m_CompressMaterial;
        private int m_TextureWidth, m_TextureHeight;

        private RenderTexture m_IntermediateTexture;
        private RenderTexture m_SourceTexture;
        private RenderTargetIdentifier m_IntermediateTextureId;
        private Mesh m_FullScreenMesh;

        private static int k_SourceTextureId = Shader.PropertyToID("_CompressSourceTexture");
        private static int k_SourceTextureMipLevelId = Shader.PropertyToID("_CompressSourceTexture_MipLevel");
        private static int k_ResultId = Shader.PropertyToID("_Result");
        private static int k_DestRectId = Shader.PropertyToID("_DestRect");
        private static readonly int ScrambleTable = Shader.PropertyToID("ScrambleTable");

        private static bool isValid = true;
        private static readonly int InputTex = Shader.PropertyToID("inputTex");
        private static readonly int OutputBuffer = Shader.PropertyToID("OutputBuffer");
        private static readonly int BlockCountX = Shader.PropertyToID("blockCountX");
        private static readonly int BlockCountY = Shader.PropertyToID("blockCountY");


        public void Prepare(Texture2D texture, ASTC_BLOCKSIZE astcBlockSize)
        {
            ASTCBlockSize = astcBlockSize;


            var w = texture.width;
            var h = texture.height;
            if (w % CompressBlockSize != 0 || h % CompressBlockSize != 0)
            {
                var l = w % CompressBlockSize;
                w += (CompressBlockSize - l);
                var t = h % CompressBlockSize;
                h += (CompressBlockSize - t);
            }

            m_TextureWidth = w;
            m_TextureHeight = h;

            // Shader.WarmupAllShaders();

            var shaderName = "Unlit/GPUTextureCompress";
            var compressShader = Shader.Find(shaderName);

            if (!compressShader.isSupported)
            {
                Debug.LogError($"GPUTextureCompressor: ASTC not supported {compressShader.isSupported}");
                isValid = false;
                return;
            }
            else
            {
                Debug.Log($"GPUTextureCompressor: ASTC supported {compressShader.isSupported}");
            }

            RecreateMaterial(compressShader, ASTCBlockSize, astcBlockSize);

            if (m_CompressMaterial.shader.name != shaderName)
            {
                Debug.LogError($"GPUTextureCompressor: shader compile failed: {m_CompressMaterial.shader.name}");

                isValid = false;
                return;
            }

            if (m_IntermediateTexture)
                RenderTexture.ReleaseTemporary(m_IntermediateTexture);

            int blockSize = CompressBlockSize;
            // Debug.Assert(srcWidth % blockSize == 0 && srcHeight % blockSize == 0);

            m_IntermediateTexture = RenderTexture.GetTemporary(
                m_TextureWidth / blockSize, m_TextureHeight / blockSize, 0,
                GraphicsFormat.R32G32B32A32_UInt, 1);
            m_IntermediateTexture.hideFlags = HideFlags.HideAndDontSave;
            m_IntermediateTexture.name = "GPU Compressor Intermediate Texture";
            m_IntermediateTexture.Create();
            m_IntermediateTextureId = m_IntermediateTexture;
            m_CompressMaterial.SetTexture(k_ResultId, m_IntermediateTexture);


            if (!m_FullScreenMesh)
            {
                m_FullScreenMesh = new Mesh
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    vertices = new[]
                    {
                        new Vector3(-1, -1, 0),
                        new Vector3(-1, 3, 0),
                        new Vector3(3, -1, 0),
                    },
                    triangles = new[] { 0, 1, 2 }
                };
                m_FullScreenMesh.RecalculateBounds();
            }
        }

        private int CompressBlockSize
        {
            get
            {
                switch (ASTCBlockSize)
                {
                    case ASTC_BLOCKSIZE.ASTC_4x4: return 4;
                    // case ASTC_BLOCKSIZE.ASTC_5x5: return 5;
                    case ASTC_BLOCKSIZE.ASTC_6x6: return 6;
                    // case ASTC_BLOCKSIZE.ASTC_8x8: return 8;
                    // case ASTC_BLOCKSIZE.ASTC_10x10: return 10;
                    // case ASTC_BLOCKSIZE.ASTC_12x12: return 12;
                    default: throw new System.ArgumentException("Invalid ASTC block size");
                }
            }
        }

        private void RecreateMaterial(Shader compressShader, ASTC_BLOCKSIZE prevBlocksize, ASTC_BLOCKSIZE blocksize)
        {
            if (m_CompressMaterial && m_CompressMaterial.shader == compressShader)
            {
                if (prevBlocksize != blocksize)
                {
                    // 仍然需要重新创建材质
                    UnityEngine.Object.Destroy(m_CompressMaterial);
                    m_CompressMaterial = null;
                }
                else
                {
                    return;
                }
            }

            m_CompressMaterial = new Material(compressShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            string keyword = blocksize switch
            {
                ASTC_BLOCKSIZE.ASTC_4x4 => BLOCK_SIZE_4x4,
                // ASTC_BLOCKSIZE.ASTC_5x5 => BLOCK_SIZE_5x5,
                ASTC_BLOCKSIZE.ASTC_6x6 => BLOCK_SIZE_6x6,
                // ASTC_BLOCKSIZE.ASTC_8x8 => BLOCK_SIZE_8x8,
                // ASTC_BLOCKSIZE.ASTC_10x10 => BLOCK_SIZE_10x10,
                // ASTC_BLOCKSIZE.ASTC_12x12 => BLOCK_SIZE_12x12,
                _ => BLOCK_SIZE_6x6
            };

            m_CompressMaterial.DisableKeyword(BLOCK_SIZE_4x4);
            // m_CompressMaterial.DisableKeyword(BLOCK_SIZE_5x5);
            m_CompressMaterial.DisableKeyword(BLOCK_SIZE_6x6);
            // m_CompressMaterial.DisableKeyword(BLOCK_SIZE_8x8);
            // m_CompressMaterial.DisableKeyword(BLOCK_SIZE_10x10);
            // m_CompressMaterial.DisableKeyword(BLOCK_SIZE_12x12);

            m_CompressMaterial.EnableKeyword(keyword);


            m_CompressMaterial.SetFloatArray(ScrambleTable, LIBII.AstcScrambleTable.ScrambleTable);
        }


        private void Dispose()
        {
            if (m_IntermediateTexture)
                RenderTexture.ReleaseTemporary(m_IntermediateTexture);
            RenderTexture.ReleaseTemporary(m_IntermediateTexture);
            m_IntermediateTexture = null;
            m_IntermediateTextureId = BuiltinRenderTextureType.None;

            if (m_FullScreenMesh)
                UnityEngine.Object.Destroy(m_FullScreenMesh);
            m_FullScreenMesh = null;

            if (m_CompressMaterial)
                UnityEngine.Object.Destroy(m_CompressMaterial);
            m_CompressMaterial = null;
        }

        private Texture2D CreateOutputTexture(int mipCount, bool srgb,
            GraphicsFormat noCompressFallback = GraphicsFormat.R8G8B8A8_UNorm)
        {
            TextureFormat format = ASTCBlockSize switch
            {
                ASTC_BLOCKSIZE.ASTC_4x4 => TextureFormat.ASTC_4x4,
                // ASTC_BLOCKSIZE.ASTC_5x5 => TextureFormat.ASTC_5x5,
                ASTC_BLOCKSIZE.ASTC_6x6 => TextureFormat.ASTC_6x6,
                // ASTC_BLOCKSIZE.ASTC_8x8 => TextureFormat.ASTC_8x8,
                // ASTC_BLOCKSIZE.ASTC_10x10 => TextureFormat.ASTC_10x10,
                // ASTC_BLOCKSIZE.ASTC_12x12 => TextureFormat.ASTC_12x12,
                _ => throw new ArgumentOutOfRangeException()
            };


            var gfxFormat = GraphicsFormatUtility.GetGraphicsFormat(format, srgb);
            if (!isValid)
            {
                gfxFormat = noCompressFallback;
            }

            Texture2D output;
            var flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            output = new Texture2D(m_TextureWidth, m_TextureHeight, gfxFormat, mipCount, flags);
            ((Texture2D)output).Apply(false, false); // 让贴图变成不可读，以卸载内存只保留显存
            output.filterMode = FilterMode.Trilinear;
            output.wrapMode = TextureWrapMode.Clamp;
            return output;
        }

        public NativeArray<byte> CompressTextureToBytes(Texture2D texture, int dstElement, int mipLevel,
            bool srgb = false)
        {
            ComputeBuffer outputBuffer = default;
            try
            {
                if (!isValid)
                {
                    return default;
                }

                // var targetTexture = CreateOutputTexture(1, srgb, GraphicsFormat.R8G8B8A8_SRGB);


                CommandBuffer cmd = CommandBufferPool.Get("GPU Texture Compress");

                cmd.SetRenderTarget(m_IntermediateTextureId);
                int rtWidth = m_IntermediateTexture.width >> mipLevel,
                    rtHeight = m_IntermediateTexture.height >> mipLevel;
                cmd.SetViewport(new Rect(0, 0, rtWidth, rtHeight));

                var sourceTexture = GetSourceTexture(cmd, texture, CompressBlockSize, mipLevel);


                if (QualitySettings.activeColorSpace == ColorSpace.Linear && srgb)
                    cmd.EnableShaderKeyword("_GPU_COMPRESS_SRGB");
                else
                    cmd.DisableShaderKeyword("_GPU_COMPRESS_SRGB");

                int destWidth = m_TextureWidth >> mipLevel, destHeight = m_TextureHeight >> mipLevel;
                cmd.SetGlobalVector(k_DestRectId,
                    new Vector4(destWidth, destHeight, 1.0f / destWidth, 1.0f / destHeight));
                cmd.SetGlobalTexture(k_SourceTextureId, sourceTexture);
                cmd.SetGlobalInt(k_SourceTextureMipLevelId, mipLevel);

                cmd.BeginSample("Compress");
                cmd.DrawMesh(m_FullScreenMesh, Matrix4x4.identity, m_CompressMaterial, 0, 0);
                cmd.EndSample("Compress");

                cmd.SetRenderTarget(BuiltinRenderTextureType.None);

                // cmd.BeginSample("CopyTexture");
                // cmd.CopyTexture(
                // m_IntermediateTextureId, 0, 0, 0, 0, rtWidth, rtHeight,
                // targetTexture, dstElement, mipLevel, 0, 0);
                // cmd.EndSample("CopyTexture");


                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);


                // 加载 Compute Shader
                ComputeShader computeShader = Resources.Load<ComputeShader>("Shaders/ReadASTC");

// 获取内核索引
                int kernelIndex = computeShader.FindKernel("CSMain");

                var blockCountX = (m_TextureWidth + CompressBlockSize - 1) / CompressBlockSize;
                var blockCountY = (m_TextureHeight + CompressBlockSize - 1) / CompressBlockSize;
                var totalBlocks = blockCountX * blockCountY;
                outputBuffer = new ComputeBuffer(totalBlocks, 16, ComputeBufferType.Default,
                    ComputeBufferMode.Immutable);
// 设置输入纹理
                computeShader.SetTexture(kernelIndex, InputTex, m_IntermediateTexture);
                computeShader.SetBuffer(kernelIndex, OutputBuffer, outputBuffer);
                computeShader.SetInt(BlockCountX, blockCountX);
                computeShader.SetInt(BlockCountY, blockCountY);

// 调用 Compute Shader
                computeShader.Dispatch(kernelIndex, blockCountX, blockCountY, 1);


                var request = AsyncGPUReadback.Request(outputBuffer, outputBuffer.count * 16, 0, OnAstcDataReady);
                AsyncGPUReadback.WaitAllRequests();

                var bytes = request.GetData<byte>();

                int sum = 0;
                for (int i = 0; i < bytes.Length; i += 256)
                {
                    sum += bytes[i];
                }

                if (sum == 0)
                {
                    return default;
                }

                return bytes;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return default;
            }
            finally
            {
                if (outputBuffer != default && outputBuffer.IsValid())
                {
                    outputBuffer.Release();
                }

                Dispose();
            }
        }

        public Texture2D CompressTexture(Texture2D texture, int dstElement, int mipLevel, bool srgb = false)
        {
            try
            {
                var bytes = CompressTextureToBytes(texture, dstElement, mipLevel, srgb);


                if (bytes == default)
                {
                    return texture;
                }

                var targetTexture = CreateOutputTexture(1, srgb, GraphicsFormat.R8G8B8A8_SRGB);

                targetTexture.LoadRawTextureData(bytes);
                targetTexture.Apply();
                bytes.Dispose();
                return targetTexture;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return texture;
            }
            finally
            {
                Dispose();
            }
        }

        private static void OnAstcDataReady(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback failed");
                Debug.LogError(
                    $"是否完成 {request.done} 层数据大小 {request.layerDataSize} 宽度 {request.width} 高度 {request.height}");
            }
            else
            {
                Debug.Log("GPU readback succeeded");
                Debug.Log($"是否完成 {request.done} 层数据大小 {request.layerDataSize} 宽度 {request.width} 高度 {request.height}");
                // var astcData = request.GetData<byte>();

                // var byteArray = new byte[astcData.Length];
                // astcData.CopyTo(byteArray);
            }
        }

        private RenderTargetIdentifier GetSourceTexture(CommandBuffer cmd, Texture2D texture, int blockSize,
            int mipLevel)
        {
            var w = texture.width;
            var h = texture.height;
            if (w % blockSize == 0 && h % blockSize == 0)
            {
                return texture;
            }

            if (!m_SourceTexture)
            {
                var l = w % blockSize;
                w += (blockSize - l);
                var t = h % blockSize;
                h += (blockSize - t);


                var graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(texture.format, true);
                m_SourceTexture = RenderTexture.GetTemporary(
                    w, h, 0,
                    graphicsFormat, 1);
                m_SourceTexture.hideFlags = HideFlags.HideAndDontSave;

                m_SourceTexture.name = "GPU Source Texture";
                m_SourceTexture.Create();
            }


            cmd.CopyTexture(texture, 0, 0, 0, 0, texture.width, texture.height, m_SourceTexture, 0, 0, 0, 0);
            return m_SourceTexture;
        }
    }
}