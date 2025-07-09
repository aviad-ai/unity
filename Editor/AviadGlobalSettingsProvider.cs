using UnityEngine;
using UnityEditor;

namespace Aviad
{
    public class AviadGlobalSettingsProvider : SettingsProvider
    {
        private const string k_SettingsPath = "Project/Aviad/Global Settings";
        private SerializedObject m_SerializedSettings;
        private SerializedProperty m_EnableNativeLogging;

        public AviadGlobalSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            var settings = AviadGlobalSettings.GetOrCreateSettings();
            m_SerializedSettings = new SerializedObject(settings);
            m_EnableNativeLogging = m_SerializedSettings.FindProperty("enableNativeLogging");
        }

        public override void OnGUI(string searchContext)
        {
            if (m_SerializedSettings == null)
                return;

            EditorGUILayout.Space();

            GUILayout.Label("Aviad Global Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_EnableNativeLogging, new GUIContent("Enable Native Logging", "Enable or disable logging for the native Aviad library"));

                if (changeCheck.changed)
                {
                    m_SerializedSettings.ApplyModifiedProperties();
                    var settings = m_SerializedSettings.targetObject as AviadGlobalSettings;
                    settings.SaveSettings();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Settings Location:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(AviadGlobalSettings.k_SettingsPath, EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Ping Settings File", GUILayout.Width(120)))
            {
                var settings = AviadGlobalSettings.GetOrCreateSettings();
                EditorGUIUtility.PingObject(settings);
            }
        }

        public override void OnDeactivate()
        {
            m_SerializedSettings?.Dispose();
        }

        [SettingsProvider]
        public static SettingsProvider CreateAviadGlobalSettingsProvider()
        {
            var provider = new AviadGlobalSettingsProvider(k_SettingsPath, SettingsScope.Project);
            provider.keywords = new[] { "Aviad", "Settings", "Logging" };
            return provider;
        }
    }
}