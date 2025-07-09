using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;

namespace Aviad
{
    public class AviadWindow : EditorWindow
    {
        [MenuItem("Window/Aviad AI")]
        public static void ShowWindow()
        {
            GetWindow<AviadWindow>("Aviad AI");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Reinstall Runtime Files"))
            {
                PluginInstaller.Install();
            }
        }
    }
}