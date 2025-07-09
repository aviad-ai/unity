using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using AOT;

namespace Aviad
{
#if UNITY_WEBGL && !UNITY_EDITOR
    public class AviadGenerationWebgl : IAviadGeneration, IWebGLUtilities
    {
        public enum CallbackReturnType
        {
            Bool,
            String,
            LlamaMessageSequence,
        }

        private static Dictionary<string, CallbackReturnType> _callbackReturnTypes = new Dictionary<string, CallbackReturnType>();
        private static Dictionary<string, Action<bool>> _boolCallbacks = new Dictionary<string, Action<bool>>();
        private static Dictionary<string, Action<string>> _stringCallbacks = new Dictionary<string, Action<string>>();
        private static Dictionary<string, Action<LlamaMessageSequence>> _msgSeqCallbacks = new Dictionary<string, Action<LlamaMessageSequence>>();
        // Track callbackId -> List[callbackId] :: Which callbackIds to cleanup when callbackId is called for the first time.
        private static Dictionary<string, List<string>> _cleanupTracking = new Dictionary<string, List<string>>();
        private int _callbackCounter = 0;

        [DllImport("__Internal")]
        private static extern bool AviadStartWebWorker(string callbackId, Action<string, string> callback);

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
                Debug.LogWarning($"Unknown callback ID: {callbackId}");
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
                            Debug.LogWarning($"Failed to parse '{message}' as bool for callback ID: {callbackId}");
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
                        msgSeqCallback?.Invoke(LlamaMessageSequence.FromJson(message));
                    }
                    break;

                default:
                    Debug.LogWarning($"Unhandled return type '{returnType}' for callback ID: {callbackId}");
                    break;
            }
            CallbackCleanup(callbackId);
        }

        [DllImport("__Internal")]
        private static extern void AviadDebug(string eventType, string message, string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadSetLoggingEnabled();

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
        private static extern void AviadInitializeGenerationModel(
            string modelParamsJson,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadShutdownGenerationModel(
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadUnloadActiveContext(
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadLoadContext(
            string contextKey,
            string templateString,
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadCacheContext(
            string callbackId);

        [DllImport("__Internal")]
        private static extern void AviadGenerateResponseStreaming(
            string contextKey,
            string outContextKey,
            string generationParmsJson,
            int chunkSize,
            string onTokenCallbackId,
            string onDoneCallbackId);

        [DllImport("__Internal")]
        private static extern void AviadDownloadFile(
            string url,
            string targetPath,
            string callbackId);

        // TODO: type parameters
        private string AddBoolCallback(Action<bool> callback, bool cleanupSelf = true, List<string> additionalCleanupIds = null) {
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

        private string AddStringCallback(Action<string> callback, bool cleanupSelf = true, List<string> additionalCleanupIds = null) {
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

        private string AddMessageSequenceCallback(Action<LlamaMessageSequence> callback, bool cleanupSelf = true, List<string> additionalCleanupIds = null) {
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

        public bool SetLoggingEnabled()
        {
            try
            {
                AviadSetLoggingEnabled();
                // TODO: Capture success correctly.
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] SetLoggingEnabled failed: {ex.Message}");
                return false;
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
                Debug.LogError($"[AviadGenerationWebgl] GetContext failed: {ex.Message}");
            }
        }

        public bool InitContext(
            string contextKey,
            LlamaMessageSequence messages,
            Action<bool> onDone = null)
        {
            try
            {
                AviadInitContext(contextKey, messages.ToJson(), AddBoolCallback(onDone));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] InitContext failed: {ex.Message}");
                return false;
            }
        }

        public bool AddTurnToContext(
            string contextKey,
            string role,
            string content,
            Action<bool> onDone = null)
        {
            try
            {
                AviadAddTurnToContext(contextKey, role, content, AddBoolCallback(onDone));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] AddTurnToContext failed: {ex.Message}");
                return false;
            }
        }

        public bool AppendToContext(string contextKey, string content, Action<bool> onDone = null)
        {
            try
            {
                AviadAppendToContext(contextKey, content, AddBoolCallback(onDone));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] AppendToContext failed: {ex.Message}");
                return false;
            }
        }

        public bool CopyContext(string sourceContextKey, string targetContextKey, Action<bool> onDone = null)
        {
            try
            {
                AviadCopyContext(sourceContextKey, targetContextKey, AddBoolCallback(onDone));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] AviadCopyContext failed: {ex.Message}");
                return false;
            }
        }

        public bool InitializeModel(LlamaModelParams modelParams, Action<bool> onComplete)
        {
            try
            {
                AviadInitializeGenerationModel(modelParams.ToJson(), AddBoolCallback(onComplete));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] InitializeModel failed: {ex.Message}");
                return false;
            }
        }

        public bool ShutdownModel(Action<bool> onComplete)
        {
            try
            {
                AviadShutdownGenerationModel(AddBoolCallback(onComplete));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] ShutdownModel  failed: {ex.Message}");
                return false;
            }
        }

        public bool UnloadActiveContext(Action<bool> onDone = null)
        {
            try
            {
                AviadUnloadActiveContext(AddBoolCallback(onDone));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] UnloadActiveContext failed: {ex.Message}");
                return false;
            }
        }

        public bool LoadContext(string contextKey, string templateString, Action<bool> onDone = null)
        {
            try
            {
                AviadLoadContext(contextKey, templateString, AddBoolCallback(onDone));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] LoadContext failed: {ex.Message}");
                return false;
            }
        }

        public bool CacheContext(Action<bool> onDone = null)
        {
            try
            {
                AviadCacheContext(AddBoolCallback(onDone));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] CacheContext failed: {ex.Message}");
                return false;
            }
        }

        public bool GenerateResponseStreaming(
            string contextKey,
            string outContextKey,
            LlamaGenerationConfig config,
            Action<string> onToken,
            Action<bool> onDone,
            int chunkSize)
        {
            try
            {
                string onTokenCallbackId = AddStringCallback(onToken, cleanupSelf: false);
                AviadGenerateResponseStreaming(
                    contextKey,
                    outContextKey,
                    config.ToJson(),
                    chunkSize,
                    onTokenCallbackId,
                    AddBoolCallback(onDone, additionalCleanupIds: new List<string> { onTokenCallbackId }));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AviadGenerationWebgl] GenerateResponseStreaming failed: {ex.Message}");
                return false;
            }
        }

        public void DownloadFile(string url, string targetPath, Action<bool> onComplete) {
            AviadDownloadFile(url, targetPath, AddBoolCallback(onComplete));
        }

        public void Start(Action<bool> onComplete)
        {
            AviadStartWebWorker(AddBoolCallback(onComplete), OnCallback);
        }

        public void Cleanup() {
            Action<bool> onShutdown = success =>
            {
                Debug.LogFormat("[AviadGenerationWebgl] Model has been shutdown. Success: {0}", success);
            };
            ShutdownModel(onShutdown);
        }
    }
#endif
}