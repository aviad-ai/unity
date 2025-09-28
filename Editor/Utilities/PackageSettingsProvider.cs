using UnityEngine;
using UnityEditor;

namespace Aviad
{
    public class PackageSettingsProvider : SettingsProvider
    {
        private const string k_SettingsPath = "Project/Aviad/Global Settings";

        private SerializedObject m_SerializedSettings;
        private SerializedProperty m_EnableNativeLogging;
        private SerializedProperty m_LogLevel;
        private SerializedProperty m_AlwaysOpenAviadWindow;

        public PackageSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            var settings = PackageSettings.GetOrCreateSettings();

            // Load hardcoded defaults if settings haven't been initialized
            if (!settings.HasBeenInitialized())
            {
                ApplyHardcodedDefaults(settings);
                settings.SaveSettings();
            }

            m_SerializedSettings = new SerializedObject(settings);
            m_EnableNativeLogging = m_SerializedSettings.FindProperty("enableNativeLogging");
            m_LogLevel = m_SerializedSettings.FindProperty("logLevel");
            m_AlwaysOpenAviadWindow = m_SerializedSettings.FindProperty("alwaysOpenAviadWindow");
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
                EditorGUILayout.PropertyField(m_EnableNativeLogging,
                    new GUIContent("Enable Native Logging", "Enable or disable logging for the native Aviad library"));

                EditorGUILayout.PropertyField(m_LogLevel,
                    new GUIContent("Log Level", "Set the minimum log level for native logging"));

                EditorGUILayout.PropertyField(m_AlwaysOpenAviadWindow,
                    new GUIContent("Always Open Aviad Window", "If enabled, the Aviad AI window will open every time Unity starts"));

                if (changeCheck.changed)
                {
                    m_SerializedSettings.ApplyModifiedProperties();
                    var settings = m_SerializedSettings.targetObject as PackageSettings;
                    settings.SaveSettings();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Current Settings:", EditorStyles.miniBoldLabel);
                var settings = m_SerializedSettings.targetObject as PackageSettings;
                EditorGUILayout.LabelField($"Native Logging: {(settings.EnableNativeLogging ? "Enabled" : "Disabled")}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Log Level: {settings.LogLevel}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Always Open Aviad Window: {(settings.AlwaysOpenAviadWindow ? "Enabled" : "Disabled")}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Settings Location:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(PackageSettings.k_SettingsPath, EditorStyles.miniLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping Settings File", GUILayout.Width(140)))
                {
                    var settings = PackageSettings.GetOrCreateSettings();
                    EditorGUIUtility.PingObject(settings);
                }

                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                {
                    var settings = m_SerializedSettings.targetObject as PackageSettings;
                    ApplyHardcodedDefaults(settings);
                    m_SerializedSettings.Update();
                    settings.SaveSettings();
                }
            }
        }

        public override void OnDeactivate()
        {
            m_SerializedSettings?.Dispose();
        }

        private void ApplyHardcodedDefaults(PackageSettings settings)
        {
            settings.EnableNativeLogging = true;
            settings.LogLevel = LogLevel.Info;
            settings.ShouldOpenAviadWindow = true;   // hidden internal default
            settings.AlwaysOpenAviadWindow = false;  // user-facing default
        }

        [SettingsProvider]
        public static SettingsProvider CreateAviadGlobalSettingsProvider()
        {
            var provider = new PackageSettingsProvider(k_SettingsPath, SettingsScope.Project);
            provider.keywords = new[] { "Aviad", "Settings", "Logging", "Log Level", "Window" };
            return provider;
        }
    }
}