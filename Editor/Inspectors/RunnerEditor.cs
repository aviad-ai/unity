using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Aviad
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor
    {
        private Runner runner;
        private SerializedObject globalSettingsSerializedObject;

        private void OnEnable()
        {
            runner = (Runner)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Model Configuration fields at the top
            EditorGUILayout.Space(5);

            // Other model configuration fields
            var saveToStreamingProp = serializedObject.FindProperty("saveToStreamingAssets");
            if (saveToStreamingProp != null)
                EditorGUILayout.PropertyField(saveToStreamingProp);

            var continueConversationProp = serializedObject.FindProperty("continueConversationAfterGeneration");
            if (continueConversationProp != null)
                EditorGUILayout.PropertyField(continueConversationProp);

            EditorGUILayout.Space(5);

            // Initialization Parameters (using the custom drawer)
            var initParamsProp = serializedObject.FindProperty("initializationParams");
            if (initParamsProp != null)
                EditorGUILayout.PropertyField(initParamsProp);

            EditorGUILayout.Space(10);

            // Generation Configuration Section
            var generationConfigProp = serializedObject.FindProperty("generationConfig");
            if (generationConfigProp != null)
            {
                EditorGUILayout.PropertyField(generationConfigProp);
            }

            EditorGUILayout.Space(10);

            // Draw any remaining properties that aren't model or generation config related
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.name != "m_Script" &&
                        prop.name != "saveToStreamingAssets" &&
                        prop.name != "continueConversationAfterGeneration" &&
                        prop.name != "initializationParams" &&
                        prop.name != "generationConfig")
                    {
                        EditorGUILayout.PropertyField(prop, true);
                    }
                }
                while (prop.NextVisible(false));
            }

            serializedObject.ApplyModifiedProperties();

            DrawGlobalSettingsSection();
        }

        private void DrawGlobalSettingsSection()
        {
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var settings = PackageSettings.GetSettings();
                string loggingStatus = settings.EnableNativeLogging ? "Enabled" : "Disabled";
                EditorGUILayout.LabelField($"Logging: {loggingStatus}", EditorStyles.miniLabel);

                if (GUILayout.Button("Open Project Settings", GUILayout.Width(150)))
                {
                    SettingsService.OpenProjectSettings("Project/Aviad/Global Settings");
                }
            }
        }

        private void OnDisable()
        {
            globalSettingsSerializedObject?.Dispose();
        }
    }
}