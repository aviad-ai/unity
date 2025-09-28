using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Aviad
{
    public class Runner : MonoBehaviour
    {
        [SerializeField] public bool saveToStreamingAssets = true;
        [SerializeField] public bool continueConversationAfterGeneration = true;

        [Header("Model Configuration")]
        [SerializeField] public ModelConfiguration modelAsset;

        public ModelRuntime runtime;

        private string _inputContextKey = "input_context";
        private string _outputContextKey = "output_context";
        private bool _isGenerating = false;
        private OperationQueue _operationQueue = new OperationQueue();

        public bool EnableNativeLogging => PackageSettings.IsNativeLoggingEnabled;
        public string ModelUrl => modelAsset.modelUrl;
        public bool IsPluginAvailable => ModelInstanceManager.Instance != null;
        public bool IsDownloaded => runtime != null && runtime.IsDownloaded;
        public bool IsAvailable => runtime != null && runtime.IsAvailable;
        public bool IsGenerating => _isGenerating;

        private void OnValidate()
        {
            if (!saveToStreamingAssets && modelAsset != null &&
                !System.IO.File.Exists(ModelRuntime.GetExpectedModelPath(modelAsset.modelUrl)))
            {
                PackageLogger.Warning(
                    $"The model asset '{modelAsset.name}' is missing. Open the AviadModel '{modelAsset.name}' and click 'DownloadModel' to retrieve it."
                );
            }
        }

        private void Start() {
            runtime = new ModelRuntime(modelAsset, saveToStreamingAssets);
            runtime.onStateUpdate += OnRuntimeStateChange;

            foreach (var handler in _pendingRuntimeStateSubscribers)
            {
                runtime.onStateUpdate += handler;
            }
            _pendingRuntimeStateSubscribers.Clear();

            runtime.Initialize();
        }

        private List<Action> _pendingRuntimeStateSubscribers = new List<Action>();

        public void SubscribeToRuntimeStateUpdate(Action handler)
        {
            if (runtime != null)
            {
                runtime.onStateUpdate += handler;
            }
            else
            {
                _pendingRuntimeStateSubscribers.Add(handler);
            }
        }

        protected void OnRuntimeStateChange()
        {
            if (runtime.IsAvailable) InitializeContext();
        }

        private void InitializeContext()
        {
            if (ModelInstanceManager.Instance == null) return;
            var emptySequence = new LlamaMessageSequence();
            ModelInstanceManager.Instance.InitContext(_inputContextKey, emptySequence);
        }

        public void Reset()
        {
            if (ModelInstanceManager.Instance == null) return;
            if (!runtime.IsAvailable) return;
            ModelInstanceManager.Instance.UnloadActiveContext(runtime.ModelId, (success) => {
                if (!success) return;
                InitializeContext();
            });
        }

        private void OnDisable()
        {
            if (runtime != null) runtime.onStateUpdate -= OnRuntimeStateChange;
            if (ModelInstanceManager.Instance != null) ModelInstanceManager.Cleanup();
        }

        private void OnGenerationSuccess(bool success)
        {
            if (ModelInstanceManager.Instance == null)
            {
                PackageLogger.Warning("Aviad manager instance is null after generation.");
                return;
            }
            _isGenerating = false;
            if (success && continueConversationAfterGeneration)
            {
                ModelInstanceManager.Instance.CopyContext(_outputContextKey, _inputContextKey);
            }
        }

        // Public wrapper methods using OperationQueue
        // AviadRunner ensures that public operations are not executed until all previous calls have completed.
        // This helps prevent buggy code from being written.

        public void AddTurnToContext(string role, string content, Action<bool> onDone = null)
        {
            var operationId = _operationQueue.GetNewId();
            var wrappedCallback = _operationQueue.WrapAction<bool>(operationId, onDone);
            _operationQueue.HandleOrderedAction(operationId, () => AddTurnToContextInternal(role, content, wrappedCallback));
        }

        public void Generate(Action<string> onUpdate, Action<bool> onDone)
        {
            var operationId = _operationQueue.GetNewId();
            var wrappedCallback = _operationQueue.WrapAction<bool>(operationId, onDone);
            _operationQueue.HandleOrderedAction(operationId, () => GenerateInternal(onUpdate, wrappedCallback));
        }

        public void GetEmbeddings(string context, Action<float[]> onResult)
        {
            var operationId = _operationQueue.GetNewId();
            var wrappedCallback = _operationQueue.WrapAction<float[]>(operationId, onResult);
            _operationQueue.HandleOrderedAction(operationId, () => GetEmbeddingsInternal(context, wrappedCallback));
        }

        public void GetInputContext(Action<LlamaMessageSequence> onResult)
        {
            var operationId = _operationQueue.GetNewId();
            var wrappedCallback = _operationQueue.WrapAction<LlamaMessageSequence>(operationId, onResult);
            _operationQueue.HandleOrderedAction(operationId, () => GetInputContextInternal(wrappedCallback));
        }

        public void GetOutputContext(Action<LlamaMessageSequence> onResult)
        {
            var operationId = _operationQueue.GetNewId();
            var wrappedCallback = _operationQueue.WrapAction<LlamaMessageSequence>(operationId, onResult);
            _operationQueue.HandleOrderedAction(operationId, () => GetOutputContextInternal(wrappedCallback));
        }

        private void AddTurnToContextInternal(string role, string content, Action<bool> onDone)
        {
            if (ModelInstanceManager.Instance == null) return;
            if (!runtime.IsAvailable || _isGenerating) return;
            ModelInstanceManager.Instance.AddTurnToContext(_inputContextKey, role, content, onDone);
        }

        private void GenerateInternal(Action<string> onUpdate, Action<bool> onDone)
        {
            if (ModelInstanceManager.Instance == null) return;
            if (!runtime.IsAvailable || _isGenerating)
            {
                PackageLogger.Warning($" Runner aborting Generation due to busy status. Runtime available?: {runtime.IsAvailable}, IsGenerating?:{IsGenerating}.");
                return;
            }

            var config = modelAsset.generationConfig;

            void WrappedOnDone(bool success)
            {
                // We need to call the local function first to free up the resources. Otherwise we have a race condition for other generation users
                OnGenerationSuccess(success);
                onDone?.Invoke(success);
            }

            _isGenerating = true;
            ModelInstanceManager.Instance.GenerateResponse(
                runtime.ModelId,
                _inputContextKey,
                _outputContextKey,
                config,
                onUpdate,
                WrappedOnDone
            );
        }

        private void GetEmbeddingsInternal(string context, Action<float[]> onResult)
        {
            ModelInstanceManager.Instance.ComputeEmbeddings(runtime.ModelId,context, modelAsset.embeddingParams, onResult);
        }

        private void GetInputContextInternal(Action<LlamaMessageSequence> onResult)
        {
            ModelInstanceManager.Instance.GetContext(_inputContextKey, 16, 128, onResult);
        }

        private void GetOutputContextInternal(Action<LlamaMessageSequence> onResult)
        {
            ModelInstanceManager.Instance.GetContext(_outputContextKey, 16, 128, onResult);
        }
    }
}