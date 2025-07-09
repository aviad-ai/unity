using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeLlamaModelParams
    {
        [MarshalAs(UnmanagedType.LPStr)] public string model_path;
        public int max_context_length;
        public int gpu_layers;
        public int threads;
        public int max_batch_length;
    }

    // Serializable Unity-friendly version
    [Serializable]
    public class LlamaModelParams
    {
        [SerializeField] public string modelPath = "";
        [SerializeField] public int maxContextLength = 2048;
        [SerializeField] public int gpuLayers = 0;
        [SerializeField] public int threads = 4;
        [SerializeField] public int maxBatchLength = 512;


        // Default constructor
        public LlamaModelParams()
        {
        }

        // Constructor from struct
        public LlamaModelParams(NativeLlamaModelParams modelParams)
        {
            modelPath = modelParams.model_path;
            maxContextLength = modelParams.max_context_length;
            gpuLayers = modelParams.gpu_layers;
            threads = modelParams.threads;
            maxBatchLength = modelParams.max_batch_length;
        }

        // Constructor with parameters
        public LlamaModelParams(string modelPath, int maxContextLength = 2048, int gpuLayers = 0, int threads = 4,
            int maxBatchLength = 512)
        {
            this.modelPath = modelPath;
            this.maxContextLength = maxContextLength;
            this.gpuLayers = gpuLayers;
            this.threads = threads;
            this.maxBatchLength = maxBatchLength;
        }

        public NativeLlamaModelParams ToStruct()
        {
            return new NativeLlamaModelParams()
            {
                model_path = modelPath,
                max_context_length = maxContextLength,
                gpu_layers = gpuLayers,
                threads = threads,
                max_batch_length = maxBatchLength
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static LlamaModelParams FromJson(string json)
        {
            return JsonUtility.FromJson<LlamaModelParams>(json);
        }
    }
}