using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Aviad
{
    [CustomEditor(typeof(Aviad.TTSRunner))]
    public class TTSRunnerEditor : UnityEditor.Editor
    {
        private SerializedProperty directoryProp;
        private SerializedProperty directoryAssetProp;
        private SerializedProperty refExamplesProp;
        private SerializedProperty initializeOnAwakeProp;
        private SerializedProperty autoUnloadOnDestroyProp;

        private readonly List<int> availableIndices = new List<int>();
        private Vector2 scroll;

        private void OnEnable()
        {
            directoryProp = serializedObject.FindProperty("directory");
            directoryAssetProp = serializedObject.FindProperty("directoryAsset");
            refExamplesProp = serializedObject.FindProperty("refExamples");
            initializeOnAwakeProp = serializedObject.FindProperty("initializeOnAwake");
            autoUnloadOnDestroyProp = serializedObject.FindProperty("autoUnloadOnDestroy");

            RefreshAvailableIndices();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("TTS Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            // Draw directory selectors
            if (directoryAssetProp != null)
            {
                EditorGUILayout.PropertyField(
                    directoryAssetProp,
                    new GUIContent("Directory Asset", "Select a folder from Assets to use as the TTS directory."));
            }

            if (directoryProp != null)
            {
                EditorGUILayout.PropertyField(
                    directoryProp,
                    new GUIContent("Directory", "Asset-relative folder path (e.g., Assets/MyFolder)."));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                RefreshAvailableIndices();
                serializedObject.Update();
            }

            EditorGUILayout.Space();
            DrawRefExamplesSelector();

            EditorGUILayout.Space();
            if (initializeOnAwakeProp != null) EditorGUILayout.PropertyField(initializeOnAwakeProp);
            if (autoUnloadOnDestroyProp != null) EditorGUILayout.PropertyField(autoUnloadOnDestroyProp);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRefExamplesSelector()
        {
            EditorGUILayout.LabelField("Reference Examples", EditorStyles.boldLabel);

            var path = GetDirectoryPath();

            if (string.IsNullOrEmpty(path))
            {
                EditorGUILayout.HelpBox("Select a directory under Assets/ to enable selecting reference example indices.", MessageType.Info);
                return;
            }

            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorGUILayout.HelpBox($"Path is not a valid folder: {path}", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
            {
                RefreshAvailableIndices();
            }

            EditorGUI.BeginDisabledGroup(availableIndices.Count == 0);
            if (GUILayout.Button("Select All"))
            {
                SetRefExamples(availableIndices);
            }

            if (GUILayout.Button("Select None"))
            {
                SetRefExamples(Array.Empty<int>());
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            var currentSelection = GetRefExamplesSet();

            // Warn for any missing selections no longer present
            var missing = currentSelection.Except(availableIndices).OrderBy(i => i).ToList();
            if (missing.Count > 0)
            {
                EditorGUILayout.HelpBox("Some selected indices are missing in the directory: " + string.Join(", ", missing), MessageType.Warning);
                if (GUILayout.Button("Remove Missing"))
                {
                    var filtered = currentSelection.Intersect(availableIndices).OrderBy(i => i).ToArray();
                    SetRefExamples(filtered);
                    serializedObject.ApplyModifiedProperties();
                    currentSelection = GetRefExamplesSet();
                }
            }

            if (availableIndices.Count == 0)
            {
                EditorGUILayout.HelpBox("No valid indices found. The directory must contain pairs named {index}_encoded.txt and {index}_transcript.txt.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Indices", EditorStyles.miniBoldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(300));
            EditorGUI.indentLevel++;
            bool changed = false;
            var selected = new HashSet<int>(currentSelection);

            foreach (var idx in availableIndices)
            {
                bool isSelected = selected.Contains(idx);
                bool newSelected = EditorGUILayout.ToggleLeft($"Index {idx}", isSelected);
                if (newSelected != isSelected)
                {
                    changed = true;
                    if (newSelected) selected.Add(idx);
                    else selected.Remove(idx);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();

            if (changed)
            {
                SetRefExamples(selected.OrderBy(i => i).ToArray());
            }

            EditorGUILayout.LabelField($"Selected: {refExamplesProp.arraySize} / {availableIndices.Count}");
        }

        private void RefreshAvailableIndices()
        {
            availableIndices.Clear();

            var path = GetDirectoryPath();
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            // Find all TextAssets under the folder and detect matching pairs
            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { path });

            var encoded = new HashSet<int>();
            var transcript = new HashSet<int>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(assetPath); // e.g., "12_encoded"
                int usIdx = fileName.IndexOf('_');
                if (usIdx <= 0)
                    continue;

                var prefix = fileName.Substring(0, usIdx);
                if (!int.TryParse(prefix, out int index))
                    continue;

                var suffix = fileName.Substring(usIdx + 1);
                if (string.Equals(suffix, "encoded", StringComparison.OrdinalIgnoreCase))
                {
                    encoded.Add(index);
                }
                else if (string.Equals(suffix, "transcript", StringComparison.OrdinalIgnoreCase))
                {
                    transcript.Add(index);
                }
            }

            availableIndices.AddRange(encoded.Where(i => transcript.Contains(i)));
            availableIndices.Sort();
        }

        private string GetDirectoryPath()
        {
            // Prefer the asset reference if present; otherwise fall back to the string path
            string path = null;

            if (directoryAssetProp != null && directoryAssetProp.objectReferenceValue != null)
            {
                path = AssetDatabase.GetAssetPath(directoryAssetProp.objectReferenceValue);
            }

            if (string.IsNullOrEmpty(path) && directoryProp != null)
            {
                path = directoryProp.stringValue;
            }

            return path;
        }

        private HashSet<int> GetRefExamplesSet()
        {
            var set = new HashSet<int>();
            if (refExamplesProp == null) return set;

            for (int i = 0; i < refExamplesProp.arraySize; i++)
            {
                set.Add(refExamplesProp.GetArrayElementAtIndex(i).intValue);
            }
            return set;
        }

        private void SetRefExamples(IEnumerable<int> values)
        {
            if (refExamplesProp == null) return;

            var arr = values?.ToArray() ?? Array.Empty<int>();
            refExamplesProp.arraySize = arr.Length;
            for (int i = 0; i < arr.Length; i++)
            {
                refExamplesProp.GetArrayElementAtIndex(i).intValue = arr[i];
            }
        }
    }
}