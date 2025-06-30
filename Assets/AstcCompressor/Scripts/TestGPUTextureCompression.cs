using System.IO;
using ASTCEncoder;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class TestGPUTextureCompression : MonoBehaviour
{
    public RawImage m_RawImage;

    public Shader m_CompressShader;
    public Texture2D m_SourceTexture;

    private GPUTextureCompressor m_TextureCompressor;
    [SerializeField] private Texture2D m_TargetTexture;
    private bool m_SRGB = true;
    private int m_SelectFormat = 2;
    private int m_EncodeCountPerFrame = 1;

    private void Awake()
    {
        m_TextureCompressor = new GPUTextureCompressor();
    }

    public void Start()
    {
    }

    private void OnGUI()

    {
        int newFormat = GUILayout.SelectionGrid(
            m_SelectFormat,
            new[] { "Original", "ASTC 4x4", "ASTC 6x6", }, 2,
            new GUIStyle(GUI.skin.button) { fontSize = 50 });

        GUILayout.Space(50);
        m_EncodeCountPerFrame = (int)GUILayout.HorizontalSlider(m_EncodeCountPerFrame, 1, 1000);
        GUILayout.Label($"{m_EncodeCountPerFrame}", new GUIStyle(GUI.skin.label) { fontSize = 50 });

        if (m_SelectFormat != newFormat)
        {
            m_SelectFormat = newFormat;
            Update1();
        }
    }

    private void Update1()
    {
        if (m_SelectFormat >= 1 && m_SelectFormat <= 2)
        {
            var blockSize = m_SelectFormat switch
            {
                1 => ASTC_BLOCKSIZE.ASTC_4x4,
                2 => ASTC_BLOCKSIZE.ASTC_6x6,
                _ => throw new System.NotImplementedException()
            };

            DestroyImmediate(m_TargetTexture);
            m_TextureCompressor.Prepare(m_SourceTexture, blockSize);
            m_TargetTexture = m_TextureCompressor.CompressTexture(m_SourceTexture, 0, 0, m_SRGB);
            
            Debug.Log($"===> {SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_UNorm,FormatUsage.ReadPixels)}");
            Debug.Log($"===> {SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_SRGB,FormatUsage.ReadPixels)}");
            Debug.Log($"===> {SystemInfo.IsFormatSupported(GraphicsFormat.RGBA_ASTC4X4_SRGB,FormatUsage.ReadPixels)}");
            Debug.Log($"===> {SystemInfo.IsFormatSupported(GraphicsFormat.RGBA_ASTC4X4_UNorm,FormatUsage.ReadPixels)}");

            // File.WriteAllBytes($"{m_TargetTexture.name}.png", m_TargetTexture.EncodeToPNG());
        }
        else
        {
            DestroyImmediate(m_TargetTexture);
            m_TargetTexture = null;
        }


        m_RawImage.texture = m_TargetTexture != null ? m_TargetTexture : m_SourceTexture;
        GetComponent<MeshRenderer>().material.mainTexture = m_TargetTexture != null ? m_TargetTexture : m_SourceTexture;
    }

    private void OnDestroy()
    {
        DestroyImmediate(m_TargetTexture);
    }
}