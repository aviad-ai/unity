using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using AOT;

namespace Aviad
{
//#if UNITY_WEBGL && !UNITY_EDITOR
    public class AviadGenerationWebgl : IAviadGeneration, IWebGLUtilities
    {
        public enum CallbackReturnType
        {
            Bool,
            String,
            LlamaMessageSequence,
        }

        private static Dictionary<string, CallbackReturnType> _callbackReturnTypes =
            new Dictionary<string, CallbackReturnType>();

        private static Dictionary<string, Action<bool>> _boolCallbacks = new Dictionary<string, Action<bool>>();
        private static Dictionary<string, Action<string>> _stringCallbacks = new Dictionary<string, Action<string>>();

        private static Dictionary<string, Action<LlamaMessageSequence>> _msgSeqCallbacks =
            new Dictionary<string, Action<LlamaMessageSequence>>();

        // Track callbackId -> List[callbackId] :: Which callbackIds to cleanup when callbackId is called for the first time.
        private static Dictionary<string, List<string>> _cleanupTracking = new Dictionary<string, List<string>>();
        private int _callbackCounter = 0;

        private static HashSet<string> _activeModelIds = new HashSet<string>();


        [DllImport("__Internal")]
        private static extern bool AviadStartWebWorker(string callbackId, Action<string, string> callback);

        [DllImport("__Internal")]
        private static extern void AviadDebug(string eventType, string message, string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadSetLoggingEnabled(string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadInitContext(
            string contextKey,
            string messagesJson,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadGetContext(
            string contextKey,
            int maxTurnCount,
            int maxStringLength,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadAddTurnToContext(
            string contextKey,
            string role,
            string content,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadAppendToContext(
            string contextKey,
            string content,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadCopyContext(
            string sourceContextKey,
            string targetContextKey,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadInitializeModel(
            string modelId,
            string modelParamsJson,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadAbortInitializeModel(
            string modelId,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadShutdownModel(
            string modelId,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadUnloadActiveContext(
            string modelId,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadLoadContext(
            string modelId,
            string contextKey,
            string templateParamsJson,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadCacheContext(
            string modelId,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadGenerateResponse(
            string modelId,
            string contextKey,
            string outContextKey,
            string generationParmsJson,
            string onTokenCallbackId,
            string onDoneCallbackId);

        [DllImport("__Internal")]
        private static extern void AviadAbortGeneration(
            string modelId,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadDownloadFile(
            string url,
            string targetPath,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadComputeEmbeddings(
            string modelId,
            string context,
            string embeddingParamsJson,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadGetEmbeddingsSize(
            string modelId,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadFreeContext(
            string contextKey,
            string callbackId);

        private static void CallbackCleanup(string callbackId)
        {
            var cleanupItems = _cleanupTracking.TryGetValue(callbackId, out var items) ? items : new List<string>();
            foreach (var callbackIdToCleanup in cleanupItems)
            {
                _callbackReturnTypes.Remove(callbackIdToCleanup);
                _boolCallbacks.Remove(callbackIdToCleanup);
                _stringCallbacks.Remove(callbackIdToCleanup);
                _msgSeqCallbacks.Remove(callbackIdToCleanup);
            }

            _cleanupTracking.Remove(callbackId);
        }

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        private static void OnCallback(string callbackId, string message)
        {
            if (!_callbackReturnTypes.TryGetValue(callbackId, out var returnType))
            {
                AviadLogger.Warning($"Unknown callback ID: {callbackId}");
                return;
            }

            switch (returnType)
            {
                case CallbackReturnType.Bool:
                    if (_boolCallbacks.TryGetValue(callbackId, out var boolCallback))
                    {
                        if (bool.TryParse(message, out var boolResult))
                        {
                            boolCallback?.Invoke(boolResult);
                        }
                        else
                        {
                            AviadLogger.Warning($"Failed to parse '{message}' as bool for callback ID: {callbackId}");
                        }
                    }

                    break;

                case CallbackReturnType.String:
                    if (_stringCallbacks.TryGetValue(callbackId, out var stringCallback))
                    {
                        stringCallback?.Invoke(message);
                    }

                    break;

                case CallbackReturnType.LlamaMessageSequence:
                    if (_msgSeqCallbacks.TryGetValue(callbackId, out var msgSeqCallback))
                    {
                        try
                        {
                            msgSeqCallback?.Invoke(LlamaMessageSequence.FromJson(message));
                        }
                        catch (Exception e)
                        {
                            AviadLogger.Warning($"Failed to parse '{message}' as LlamaMessageSequence for callback ID: {callbackId}. {e.Message}");
                        }
                    }

                    break;

                default:
                    AviadLogger.Warning($"Unhandled return type '{returnType}' for callback ID: {callbackId}");
                    break;
            }

            CallbackCleanup(callbackId);
        }

        // TODO: type parameters
        private string AddBoolCallback(Action<bool> callback, bool cleanupSelf = true,
            List<string> additionalCleanupIds = null)
        {
            string callbackId = $"{_callbackCounter}";
            _callbackCounter += 1;
            _callbackReturnTypes[callbackId] = CallbackReturnType.Bool;
            _boolCallbacks[callbackId] = callback;
            var cleanupItems = new List<string>();
            if (cleanupSelf) cleanupItems.Add(callbackId);
            if (additionalCleanupIds != null) cleanupItems.AddRange(additionalCleanupIds);
            _cleanupTracking[callbackId] = cleanupItems;
            return callbackId;
        }

        private string AddStringCallback(Action<string> callback, bool cleanupSelf = true,
            List<string> additionalCleanupIds = null)
        {
            string callbackId = $"{_callbackCounter}";
            _callbackCounter += 1;
            _callbackReturnTypes[callbackId] = CallbackReturnType.String;
            _stringCallbacks[callbackId] = callback;
            var cleanupItems = new List<string>();
            if (cleanupSelf) cleanupItems.Add(callbackId);
            if (additionalCleanupIds != null) cleanupItems.AddRange(additionalCleanupIds);
            _cleanupTracking[callbackId] = cleanupItems;
            return callbackId;
        }

        private string AddMessageSequenceCallback(Action<LlamaMessageSequence> callback, bool cleanupSelf = true,
            List<string> additionalCleanupIds = null)
        {
            string callbackId = $"{_callbackCounter}";
            _callbackCounter += 1;
            _callbackReturnTypes[callbackId] = CallbackReturnType.LlamaMessageSequence;
            _msgSeqCallbacks[callbackId] = callback;
            var cleanupItems = new List<string>();
            if (cleanupSelf) cleanupItems.Add(callbackId);
            if (additionalCleanupIds != null) cleanupItems.AddRange(additionalCleanupIds);
            _cleanupTracking[callbackId] = cleanupItems;
            return callbackId;
        }

        public void SetLoggingEnabled(Action<bool> onDone = null)
        {
            try
            {
                AviadSetLoggingEnabled(AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] SetLoggingEnabled failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void GetContext(
            string contextKey,
            int maxTurnCount,
            int maxStringLength,
            Action<LlamaMessageSequence> onResult = null)
        {
            try
            {
                AviadGetContext(contextKey, maxTurnCount, maxStringLength, AddMessageSequenceCallback(onResult));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] GetContext failed: {ex.Message}");
                onResult?.Invoke(null);
            }
        }

        public void InitContext(
            string contextKey,
            LlamaMessageSequence messages,
            Action<bool> onDone = null)
        {
            try
            {
                AviadInitContext(contextKey, messages.ToJson(), AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] InitContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void AddTurnToContext(
            string contextKey,
            string role,
            string content,
            Action<bool> onDone = null)
        {
            try
            {
                AviadAddTurnToContext(contextKey, role, content, AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] AddTurnToContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void AppendToContext(string contextKey, string content, Action<bool> onDone = null)
        {
            try
            {
                AviadAppendToContext(contextKey, content, AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] AppendToContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void CopyContext(string sourceContextKey, string targetContextKey, Action<bool> onDone = null)
        {
            try
            {
                AviadCopyContext(sourceContextKey, targetContextKey, AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] AviadCopyContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void FreeContext(string contextKey, Action<bool> onDone = null)
        {
            try
            {
                AviadFreeContext(contextKey, AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] FreeContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void InitializeModel(string modelId, LlamaInitializationParams modelParams, Action<bool> onComplete)
        {
            try
            {
                // Track this model ID as active
                _activeModelIds.Add(modelId);

                Action<bool> wrappedCallback = success =>
                {
                    if (!success)
                    {
                        // If initialization failed, remove from active models
                        _activeModelIds.Remove(modelId);
                    }

                    onComplete?.Invoke(success);
                };

                AviadInitializeModel(modelId, modelParams.ToJson(), AddBoolCallback(wrappedCallback));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] InitializeModel failed: {ex.Message}");
                _activeModelIds.Remove(modelId);
                onComplete?.Invoke(false);
            }
        }

        public void AbortInitializeModel(string modelId, Action<bool> onComplete)
        {
            try
            {
                Action<bool> wrappedCallback = success =>
                {
                    // Remove from active models regardless of success
                    _activeModelIds.Remove(modelId);
                    onComplete?.Invoke(success);
                };

                AviadAbortInitializeModel(modelId, AddBoolCallback(wrappedCallback));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] AbortInitializeModel failed: {ex.Message}");
                _activeModelIds.Remove(modelId);
                onComplete?.Invoke(false);
            }
        }

        public void ShutdownModel(string modelId, Action<bool> onComplete)
        {
            try
            {
                Action<bool> wrappedCallback = success =>
                {
                    // Remove from active models regardless of success
                    _activeModelIds.Remove(modelId);
                    onComplete?.Invoke(success);
                };

                AviadShutdownModel(modelId, AddBoolCallback(wrappedCallback));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] ShutdownModel failed: {ex.Message}");
                _activeModelIds.Remove(modelId);
                onComplete?.Invoke(false);
            }
        }

        public void UnloadActiveContext(string modelId, Action<bool> onDone = null)
        {
            try
            {
                AviadUnloadActiveContext(modelId, AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] UnloadActiveContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void LoadContext(string modelId, string contextKey, string templateString,
            Action<bool> onDone = null)
        {
            try
            {
                var templateParams = new LlamaChatTemplateParams(templateString);
                AviadLoadContext(modelId, contextKey, templateParams.ToJson(), AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] LoadContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void CacheContext(string modelId, Action<bool> onDone = null)
        {
            try
            {
                AviadCacheContext(modelId, AddBoolCallback(onDone));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] CacheContext failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void GenerateResponse(
            string modelId,
            string contextKey,
            string outContextKey,
            LlamaGenerationConfig config,
            Action<string> onToken,
            Action<bool> onDone)
        {
            try
            {
                string onTokenCallbackId = AddStringCallback(onToken, cleanupSelf: false);
                AviadGenerateResponse(
                    modelId,
                    contextKey,
                    outContextKey,
                    config.ToJson(),
                    onTokenCallbackId,
                    AddBoolCallback(onDone, additionalCleanupIds: new List<string> { onTokenCallbackId }));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] GenerateResponse failed: {ex.Message}");
                onDone?.Invoke(false);
            }
        }

        public void AbortGeneration(string modelId, Action<bool> onComplete)
        {
            try
            {
                AviadAbortGeneration(modelId, AddBoolCallback(onComplete));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] AbortGeneration failed: {ex.Message}");
                onComplete?.Invoke(false);
            }
        }

        [Serializable]
        public class FloatArrayWrapper
        {
            public float[] floats;
        }

        public void ComputeEmbeddings(string modelId, string context, LlamaEmbeddingParams embeddingParams,
            Action<float[]> onComplete)
        {
            try
            {
                // Note: This would need a specialized callback for float array results
                // For now, using string callback and parsing on the JS side
                AviadComputeEmbeddings(modelId, context, embeddingParams.ToJson(), AddStringCallback(result =>
                {
                    // Parse the JSON array of floats
                    try
                    {
                        var floatArrayWrapper = JsonUtility.FromJson<FloatArrayWrapper>(result);
                        onComplete?.Invoke(floatArrayWrapper.floats);
                    }
                    catch (Exception ex)
                    {
                        AviadLogger.Error($"Failed to parse embeddings result: {ex.Message}");
                        onComplete?.Invoke(null);
                    }
                }));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] ComputeEmbeddings failed: {ex.Message}");
                onComplete?.Invoke(null);
            }
        }

        public void GetEmbeddingsSize(string modelId, Action<uint> onComplete)
        {
            try
            {
                AviadGetEmbeddingsSize(modelId, AddStringCallback(result =>
                {
                    if (uint.TryParse(result, out var size))
                    {
                        onComplete?.Invoke(size);
                    }
                    else
                    {
                        AviadLogger.Error($"Failed to parse embeddings size: {result}");
                        onComplete?.Invoke(0);
                    }
                }));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] GetEmbeddingsSize failed: {ex.Message}");
                onComplete?.Invoke(0);
            }
        }

        public void DownloadFile(string url, string targetPath, Action<bool> onComplete)
        {
            try
            {
                AviadDownloadFile(url, targetPath, AddBoolCallback(onComplete));
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] DownloadFile failed: {ex.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void Start(Action<bool> onComplete)
        {
            try
            {
                AviadStartWebWorker(AddBoolCallback(onComplete), OnCallback);
            }
            catch (Exception ex)
            {
                AviadLogger.Error($"[AviadGenerationWebgl] Start failed: {ex.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void Dispose()
        {
            AviadLogger.Debug($"[AviadGenerationWebgl] Cleanup started. Active models: {_activeModelIds.Count}");

            if (_activeModelIds.Count == 0)
            {
                AviadLogger.Debug("[AviadGenerationWebgl] No active models to cleanup.");
                return;
            }

            // Create a copy of the active model IDs to iterate over
            var modelsToShutdown = new List<string>(_activeModelIds);
            int remainingModels = modelsToShutdown.Count;

            foreach (var modelId in modelsToShutdown)
            {
                Action<bool> onShutdown = success =>
                {
                    AviadLogger.DebugFormat("[AviadGenerationWebgl] Model {0} shutdown. Success: {1}", modelId,
                        success);
                    remainingModels--;

                    if (remainingModels == 0)
                    {
                        AviadLogger.Debug("[AviadGenerationWebgl] All models have been shutdown.");

                        // Clear all callback dictionaries
                        _callbackReturnTypes.Clear();
                        _boolCallbacks.Clear();
                        _stringCallbacks.Clear();
                        _msgSeqCallbacks.Clear();
                        _cleanupTracking.Clear();
                    }
                };

                ShutdownModel(modelId, onShutdown);
            }
        }

        // Helper method to get active model IDs (useful for debugging)
        public static HashSet<string> GetActiveModelIds()
        {
            return new HashSet<string>(_activeModelIds);
        }

        // Helper method to check if a specific model is active
        public static bool IsModelActive(string modelId)
        {
            return _activeModelIds.Contains(modelId);
        }
    }
//#endif
}