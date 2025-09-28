using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MainThreadDispatcher;

namespace Aviad
{
    public static class EditorLifecycle
    {
        private static HashSet<ManagerEditorBase> _activeEditors = new HashSet<ManagerEditorBase>();
        private static bool _isRegistered = false;
        private static ModelRuntime _currentModelRuntime = null;
        private static ModelConfiguration _currentModel = null;
        public static ModelRuntime Runtime => _currentModelRuntime;

        public static void RegisterEditor(ManagerEditorBase editor)
        {
            _activeEditors.Add(editor);

            if (!_isRegistered)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                _isRegistered = true;
            }

            Dispatcher.StartEditorUpdate();

            EnsureManagerInitialized();
        }

        public static void UnregisterEditor(ManagerEditorBase editor)
        {
            _activeEditors.Remove(editor);

            // If no more editors are active, unregister the callback
            if (_activeEditors.Count == 0 && _isRegistered)
            {
                Dispatcher.StopEditorUpdate();
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                _isRegistered = false;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                CleanupManager();
            }
        }

        private static void EnsureManagerInitialized()
        {
            if (ModelInstanceManager.Instance == null)
            {
                ModelInstanceManager.Initialize();
            }
        }

        private static void CleanupManager()
        {
            try
            {
                ModelInstanceManager.Cleanup();
            }
            catch (System.Exception ex)
            {
                PackageLogger.Error($"Failed to cleanup AviadManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method for manual cleanup
        /// </summary>
        public static void ManualCleanup()
        {
            CleanupManager();
            _currentModelRuntime = null;
            _currentModel = null;
        }

        /// <summary>
        /// Initialize or update the model runtime with the specified model
        /// </summary>
        public static void InitializeWithModel(ModelConfiguration model, bool enableDownload = true)
        {
            if (model == null)
            {
                PackageLogger.Error("Cannot initialize with null AviadModel");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                PackageLogger.Warning("Cannot initialize AviadModel from AviadEditorBase during play mode.");
                return;
            }

            // Ensure base manager is initialized
            EnsureManagerInitialized();

            // If we have a different model, dispose the current runtime
            if (_currentModelRuntime != null && _currentModel != model)
            {
                _currentModelRuntime.Dispose();
                _currentModelRuntime = null;
                _currentModel = null;
            }

            // Create new runtime if needed
            if (_currentModelRuntime == null)
            {
                _currentModelRuntime = new ModelRuntime(model, enableDownload);
                _currentModel = model;
                _currentModelRuntime.Initialize();
            }
        }

        /// <summary>
        /// Check if the manager and model runtime are properly initialized
        /// </summary>
        public static bool IsFullyInitialized(ModelConfiguration requiredModel = null)
        {
            bool managerReady = ModelInstanceManager.Instance != null;
            bool modelReady = _currentModelRuntime != null && _currentModel != null;
            bool modelMatches = requiredModel == null || _currentModel == requiredModel;

            return managerReady && modelReady && modelMatches;
        }

        /// <summary>
        /// Get the currently loaded model
        /// </summary>
        public static ModelConfiguration GetCurrentModel()
        {
            return _currentModel;
        }

        /// <summary>
        /// Get the current model runtime
        /// </summary>
        public static ModelRuntime GetCurrentModelRuntime()
        {
            return _currentModelRuntime;
        }
    }

    /// <summary>
    /// Base class for editors that need AviadManager and persistent model selection
    /// </summary>
    public abstract class ManagerEditorBase : UnityEditor.Editor
    {
        private ModelConfiguration _editorAviadModel;
        private string _targetGuid;

        protected ModelConfiguration EditorAviadModel
        {
            get => _editorAviadModel;
            set
            {
                if (_editorAviadModel != value)
                {
                    _editorAviadModel = value;
                    if (!string.IsNullOrEmpty(_targetGuid))
                    {
                        ModelEditorHelperState.instance.SetModel(_targetGuid, value);
                    }
                }
            }
        }

        protected virtual void OnEnable()
        {
            try
            {
                var globalId = GlobalObjectId.GetGlobalObjectIdSlow(target);
                _targetGuid = globalId.ToString();
            }
            catch
            {
                return;
            }

            // Restore model from the helper
            if (!string.IsNullOrEmpty(_targetGuid))
            {
                _editorAviadModel = ModelEditorHelperState.instance.GetModel(_targetGuid);
            }

            EditorLifecycle.RegisterEditor(this);
        }

        protected virtual void OnDisable()
        {
            EditorLifecycle.ManualCleanup();
            EditorLifecycle.UnregisterEditor(this);
        }

        private string GetTargetGuid(Object target)
        {
            if (target == null)
                return null;

            string path = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        protected bool IsFullyInitialized()
        {
            return EditorLifecycle.IsFullyInitialized(EditorAviadModel);
        }

        protected void DrawAviadManagerControls()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            ModelConfiguration newModel = (ModelConfiguration)EditorGUILayout.ObjectField(
                "Aviad Model",
                EditorAviadModel,
                typeof(ModelConfiguration),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                EditorAviadModel = newModel; // setter handles saving
                Repaint();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool isFullyReady = IsFullyInitialized();
                bool hasModel = EditorAviadModel != null;
                bool managerExists = ModelInstanceManager.Instance != null;

                string statusText;
                Color statusColor;

                if (!hasModel)
                {
                    statusText = "Model: Required";
                    statusColor = Color.red;
                }
                else if (isFullyReady)
                {
                    statusText = "Status: Ready";
                    statusColor = Color.green;
                }
                else if (managerExists)
                {
                    statusText = "Model: Not Loaded";
                    statusColor = Color.yellow;
                }
                else
                {
                    statusText = "Manager: Not Loaded";
                    statusColor = Color.yellow;
                }

                Color originalColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(statusText, EditorStyles.miniLabel);
                GUI.color = originalColor;

                GUILayout.Space(10);

                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    Repaint();
                }

                GUILayout.Space(5);

                if (isFullyReady)
                {
                    if (GUILayout.Button("Unload Manager", GUILayout.Width(120)))
                    {
                        EditorLifecycle.ManualCleanup();
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(!hasModel))
                    {
                        if (GUILayout.Button("Load Manager", GUILayout.Width(120)))
                        {
                            EditorLifecycle.InitializeWithModel(EditorAviadModel, true);
                        }
                    }
                }
            }

            EditorGUILayout.Space();
        }

        protected virtual bool ShouldDrawManagerControls => true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!EditorApplication.isPlayingOrWillChangePlaymode && ShouldDrawManagerControls)
            {
                DrawAviadManagerControls();
            }
        }
    }
}