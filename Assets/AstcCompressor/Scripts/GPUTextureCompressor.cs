using System;
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

            var compressShader = Shader.Find("Unlit/GPUTextureCompress");
            RecreateMaterial(compressShader, ASTCBlockSize, astcBlockSize);

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

        public Texture2D CreateOutputTexture(int mipCount, bool srgb,
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

            Texture2D output;
            var flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            output = new Texture2D(m_TextureWidth, m_TextureHeight, gfxFormat, mipCount, flags);
            ((Texture2D)output).Apply(false, true); // 让贴图变成不可读，以卸载内存只保留显存
            output.filterMode = FilterMode.Trilinear;
            output.wrapMode = TextureWrapMode.Clamp;
            return output;
        }

        public Texture2D CompressTexture(Texture2D texture, int dstElement, int mipLevel, bool srgb = false)
        {
            try
            {
                var targetTexture = CreateOutputTexture(1, srgb, GraphicsFormat.R8G8B8A8_SRGB);

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

                cmd.BeginSample("CopyTexture");
                cmd.CopyTexture(
                    m_IntermediateTextureId, 0, 0, 0, 0, rtWidth, rtHeight,
                    targetTexture, dstElement, mipLevel, 0, 0);
                cmd.EndSample("CopyTexture");

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                return targetTexture;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
            finally
            {
                Dispose();
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