using System;
using System.Threading.Tasks;
using UnityEngine;
using MainThreadDispatcher;

namespace Aviad
{
    public class BackgroundNativeLibrary : ITextGeneration
    {
        private readonly NativeLibrary nativeLibrary;

        public BackgroundNativeLibrary(NativeLibrary nativeLibrary)
        {
            this.nativeLibrary = nativeLibrary ?? throw new ArgumentNullException(nameof(nativeLibrary));
        }

        // Helper for operations with Action<bool> callbacks
        private void RunAsync(Action<Action<bool>> operation, Action<bool> onDone)
        {
            Task.Run(() => operation(result => Dispatcher.Enqueue(() => onDone?.Invoke(result))));
        }

        // Helper for operations with generic result callbacks
        private void RunAsync<T>(Action<Action<T>> operation, Action<T> onResult)
        {
            Task.Run(() => operation(result =>
            {
                Dispatcher.Enqueue(() => onResult?.Invoke(result));
            }));
        }

        // Helper for operations with two callbacks (1-arg each)
        private void RunAsync<T1, T2>(Action<Action<T1>, Action<T2>> operation, Action<T1> callback1, Action<T2> callback2)
        {
            Task.Run(() => operation(
                result1 => Dispatcher.Enqueue(() => callback1?.Invoke(result1)),
                result2 => Dispatcher.Enqueue(() => callback2?.Invoke(result2))
            ));
        }

        // Helper for operations where first callback has two args (e.g., TTS audio) and second is 1-arg
        private void RunAsync<TA, TB, TC>(Action<Action<TA, TB>, Action<TC>> operation, Action<TA, TB> callback1, Action<TC> callback2)
        {
            Task.Run(() => operation(
                (a, b) => Dispatcher.Enqueue(() => callback1?.Invoke(a, b)),
                c => Dispatcher.Enqueue(() => callback2?.Invoke(c))
            ));
        }

        // Simple sync operations
        public void SetLoggingEnabled(Action<bool> onDone = null)
        {
            nativeLibrary.SetLoggingEnabled(onDone);
        }

        // Operations with callbacks
        public void GetContext(string contextKey, int maxTurnCount, int maxStringLength, Action<LlamaMessageSequence> onResult)
        {
            RunAsync(callback => nativeLibrary.GetContext(contextKey, maxTurnCount, maxStringLength, callback), onResult);
        }

        public void InitContext(string contextKey, LlamaMessageSequence messages, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.InitContext(contextKey, messages, callback), onDone);
        }

        public void AddTurnToContext(string contextKey, string role, string content, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.AddTurnToContext(contextKey, role, content, callback), onDone);
        }

        public void AppendToContext(string contextKey, string content, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.AppendToContext(contextKey, content, callback), onDone);
        }

        public void CopyContext(string sourceContextKey, string targetContextKey, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.CopyContext(sourceContextKey, targetContextKey, callback), onDone);
        }

        public void FreeContext(string contextKey, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.FreeContext(contextKey, callback), onDone);
        }

        public void InitializeModel(string modelId, LlamaInitializationParams initializationParams, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.InitializeModel(modelId, initializationParams, callback), onDone);
        }

        public void AbortInitializeModel(string modelId, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.AbortInitializeModel(modelId, callback), onDone);
        }

        public void ShutdownModel(string modelId, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.ShutdownModel(modelId, callback), onDone);
        }

        public void UnloadActiveContext(string modelId, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.UnloadActiveContext(modelId, callback), onDone);
        }

        public void LoadContext(string modelId, string contextKey, string templateString, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.LoadContext(modelId, contextKey, templateString, callback), onDone);
        }

        public void CacheContext(string modelId, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.CacheContext(modelId, callback), onDone);
        }

        public void GenerateResponse(string modelId, string contextKey, string outContextKey, LlamaGenerationConfig config, Action<string> onToken, Action<bool> onDone)
        {
            RunAsync((tokenCallback, doneCallback) => nativeLibrary.GenerateResponse(modelId, contextKey, outContextKey, config, tokenCallback, doneCallback), onToken, onDone);
        }

        public void ComputeEmbeddings(string modelId, string context, LlamaEmbeddingParams embeddingParams, Action<float[]> onResult = null)
        {
            RunAsync(callback => nativeLibrary.ComputeEmbeddings(modelId, context, embeddingParams, callback), onResult);
        }

        public void AbortGeneration(string modelId, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.AbortGeneration(modelId, callback), onDone);
        }

        // --- TTS ---
        public void LoadTTS(TTSParams ttsParams, Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.LoadTTS(ttsParams, callback), onDone);
        }

        public void GenerateTTS(string text, Action<bool> onDone = null)
        {
            const int DefaultTtsMaxSamples = 240000;
            GenerateTTS(text, DefaultTtsMaxSamples, null, onDone);
        }

        public void GenerateTTS(string text, int maxLength, Action<float[]> onAudio, Action<bool> onDone = null)
        {
            RunAsync((audioCb, doneCb) => nativeLibrary.GenerateTTS(text, maxLength, audioCb, doneCb), onAudio, onDone);
        }

        public void UnloadTTS(Action<bool> onDone = null)
        {
            RunAsync(callback => nativeLibrary.UnloadTTS(callback), onDone);
        }

        public void Dispose()
        {
            Task.Run(() => nativeLibrary?.Dispose());
        }

        public bool SafeEnsureLoaded()
        {
            // Intentionally not on another thread;
            return nativeLibrary.SafeEnsureLoaded();
        }

        public void EnsureLoaded()
        {
            // Intentionally not on another thread;
            nativeLibrary.EnsureLoaded();
        }
    }
}
