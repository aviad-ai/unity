using System;
using UnityEngine;

namespace Aviad
{
    [CreateAssetMenu(fileName = "New Aviad Model", menuName = "Aviad/Model Configuration")]
    public class ModelConfiguration : ScriptableObject
    {
        [Header("Model Configuration")]
        [SerializeField] public string modelUrl = "";

        [Header("Model Parameters")]
        [SerializeField] public LlamaInitializationParams modelParams = new LlamaInitializationParams();

        [Header("Generation Configuration")]
        [SerializeField] public LlamaGenerationConfig generationConfig = new LlamaGenerationConfig();

        [Header("Embedding Parameters")]
        [SerializeField] public LlamaEmbeddingParams embeddingParams = new LlamaEmbeddingParams();
    }
}