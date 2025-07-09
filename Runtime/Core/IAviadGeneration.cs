using System;
using UnityEngine;

namespace Aviad
{
    public interface IAviadGeneration
    {
        void Cleanup();

        // Logging
        bool SetLoggingEnabled();

        // Context Management
        void GetContext(
            string contextKey,
            int maxTurnCount,
            int maxStringLength,
            Action<LlamaMessageSequence> onResult);
        bool InitContext(string contextKey, LlamaMessageSequence messages, Action<bool> onDone = null);
        bool AddTurnToContext(string contextKey, string role, string content, Action<bool> onDone = null);
        bool AppendToContext(string contextKey, string content, Action<bool> onDone = null);
        bool CopyContext(string sourceContextKey, string targetContextKey, Action<bool> onDone = null);

        // Llama.cpp Interaction
        bool InitializeModel(LlamaModelParams modelParams, Action<bool> onComplete);
        bool ShutdownModel(Action<bool> onComplete);
        bool UnloadActiveContext(Action<bool> onDone = null);
        bool LoadContext(string contextKey, string templateString, Action<bool> onDone = null);
        bool CacheContext(Action<bool> onDone = null);
        bool GenerateResponseStreaming(
            string contextKey,
            string outContextKey,
            LlamaGenerationConfig config,
            Action<String> onToken,
            Action<bool> onDone,
            int chunkSize);
    }
}