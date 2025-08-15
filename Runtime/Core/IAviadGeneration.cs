using System;
using UnityEngine;

namespace Aviad
{
    /*
     * Interface to Aviad's C++ plugin.
     * C# side is responsible for allocation.
     * Callbacks enable consistency between async and sync.
     * Return value of false indicates failure.
     */
    public interface IAviadGeneration : IDisposable
    {
        void SetLoggingEnabled(
            Action<bool> onDone = null);

        // Context Management
        void GetContext(
            string contextKey,
            int maxTurnCount,
            int maxStringLength,
            Action<LlamaMessageSequence> onResult);
        void InitContext(
            string contextKey,
            LlamaMessageSequence messages,
            Action<bool> onDone = null);
        void AddTurnToContext(
            string contextKey,
            string role,
            string content,
            Action<bool> onDone = null);
        void AppendToContext(
            string contextKey,
            string content,
            Action<bool> onDone = null);
        void CopyContext(
            string sourceContextKey,
            string targetContextKey,
            Action<bool> onDone = null);
        void FreeContext(
            string contextKey,
            Action<bool> onDone = null);

        // Llama.cpp Interaction
        void InitializeModel(
            string modelId,
            LlamaInitializationParams initializationParams,
            Action<bool> onDone = null);
        void AbortInitializeModel(
            string modelId,
            Action<bool> onDone = null);
        void ShutdownModel(
            string modelId,
            Action<bool> onDone = null);
        void UnloadActiveContext(
            string modelId,
            Action<bool> onDone = null);
        void LoadContext(
            string modelId,
            string contextKey,
            string templateString,
            Action<bool> onDone = null);
        void CacheContext(
            string modelId,
            Action<bool> onDone = null);
        void GenerateResponse(
            string modelId,
            string contextKey,
            string outContextKey,
            LlamaGenerationConfig config,
            Action<String> onToken,
            Action<bool> onDone);
        void ComputeEmbeddings(
            string modelId,
            string context,
            LlamaEmbeddingParams embeddingParams,
            Action<float[]> onResult = null);
        void AbortGeneration(
            string modelId,
            Action<bool> onDone = null);
    }
}