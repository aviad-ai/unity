using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeLlamaEmbeddingParams
    {
        [MarshalAs(UnmanagedType.U4)] public uint max_embeddings_size;
    }

    [Serializable]
    public class LlamaEmbeddingParams
    {
        [SerializeField] public uint maxEmbeddingsSize = 4096;

        // Default constructor
        public LlamaEmbeddingParams()
        {
        }

        public NativeLlamaEmbeddingParams ToStruct()
        {
            return new NativeLlamaEmbeddingParams()
            {
                max_embeddings_size = maxEmbeddingsSize,
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static LlamaChatTemplateParams FromJson(string json)
        {
            return JsonUtility.FromJson<LlamaChatTemplateParams>(json);
        }
    }
}