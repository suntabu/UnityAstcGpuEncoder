using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ASTCEncoder;
using LIBII;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace AstcCompressor.Scripts
{
    public class TestCompressorCPU : MonoBehaviour
    {
        public Texture2D m_Texture;

        public RawImage m_RawImage;
        public RawImage m_RawImage_Src;

        public ASTC_BLOCKSIZE m_BlockSize = ASTC_BLOCKSIZE.ASTC_4x4;

        private Texture2D tex;

        [ContextMenu("Start")]
        private IEnumerator Start()
        {
            Debug.Log(m_Texture);
            Compress();
            yield return null;
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 100, 30), "Compress"))
                Compress();
        }

        void Compress()
        {
            m_RawImage_Src.texture = m_Texture;
            var compressor = new CompressorASTC(m_Texture, m_BlockSize);

            var sw = Stopwatch.StartNew();
            var handle =compressor.Compress();

            handle.Complete();
            Debug.Log($"time:{sw.ElapsedMilliseconds}");

            var result = compressor.GetResult();
            Debug.Log($"time:{sw.ElapsedMilliseconds}");

            var bytes = result;

            Debug.Log($"time:{sw.ElapsedMilliseconds}");
            if (!tex)
            {
                DestroyImmediate(tex);
            }

            var tf = m_BlockSize switch
            {
                ASTC_BLOCKSIZE.ASTC_4x4 => TextureFormat.ASTC_4x4,
                ASTC_BLOCKSIZE.ASTC_5x5 => TextureFormat.ASTC_5x5,
                ASTC_BLOCKSIZE.ASTC_6x6 => TextureFormat.ASTC_6x6,
                _ => TextureFormat.ASTC_4x4
            };

            tex = new Texture2D(m_Texture.width, m_Texture.height, tf, false);
            tex.filterMode = FilterMode.Trilinear;
            tex.LoadRawTextureData(bytes.ToArray());
            tex.Apply();


            m_RawImage.texture = tex;
        }
    }
}