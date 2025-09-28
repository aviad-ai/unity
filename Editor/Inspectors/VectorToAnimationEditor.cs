using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Aviad
{
    [CustomEditor(typeof(VectorToAnimation))]
    public class VectorToAnimationEditor : UnityEditor.Editor
    {
        private VectorToAnimation vectorToAnimation;
        private Vector2 mappingScrollPosition;
        private List<string> availableTriggers = new List<string>();
        private bool showMappings = true;
        private bool showLegacyWarning = false;

        private void OnEnable()
        {
            vectorToAnimation = (VectorToAnimation)target;
            RefreshAvailableTriggers();
            CheckForLegacyUsage();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw the default inspector for basic properties
            DrawDefaultInspectorExcludingMappings();

            EditorGUILayout.Space(10);

            // Show legacy warning if needed
            if (showLegacyWarning)
            {
                EditorGUILayout.HelpBox(
                    "This component is using obsolete methods. Please update to use the new tag-based animation mapping system.", 
                    MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            // Animation Controller Analysis
            DrawAnimationControllerInfo();

            EditorGUILayout.Space(10);

            // Cluster Tags → Animation Mapping
            DrawTagAnimationMappings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultInspectorExcludingMappings()
        {
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    // Skip the script reference and the mappings array
                    if (prop.name != "m_Script" && prop.name != "tagAnimationMappings")
                    {
                        EditorGUILayout.PropertyField(prop, true);
                    }
                }
                while (prop.NextVisible(false));
            }
        }

        private void DrawAnimationControllerInfo()
        {
            EditorGUILayout.LabelField("Animation Controller Analysis", EditorStyles.boldLabel);

            if (vectorToAnimation.AnimatorComponent == null)
            {
                EditorGUILayout.HelpBox("No Animator component found. Please assign an Animator.", MessageType.Warning);
                return;
            }

            if (vectorToAnimation.AnimatorComponent.runtimeAnimatorController == null)
            {
                EditorGUILayout.HelpBox("No Animator Controller assigned to the Animator component.", MessageType.Warning);
                return;
            }

            // Refresh triggers button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Available Triggers ({availableTriggers.Count}):", EditorStyles.miniLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshAvailableTriggers();
            }
            EditorGUILayout.EndHorizontal();

            if (availableTriggers.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (string trigger in availableTriggers)
                {
                    EditorGUILayout.LabelField($"• {trigger}", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("No trigger parameters found in the Animator Controller.", MessageType.Info);
            }
        }

        private void DrawTagAnimationMappings()
        {
            showMappings = EditorGUILayout.Foldout(showMappings, "Cluster Tags → Animation Mapping", true);
            if (!showMappings) return;

            EditorGUI.indentLevel++;

            // Validation
            if (vectorToAnimation.ClustersComponent == null)
            {
                EditorGUILayout.HelpBox("No Clusters component assigned. Please assign a Clusters component to configure mappings.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            if (availableTriggers.Count == 0)
            {
                EditorGUILayout.HelpBox("No animation triggers available. Please ensure the Animator Controller has trigger parameters.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            // Sync button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Synchronize with Clusters component:", EditorStyles.miniLabel);
            if (GUILayout.Button("Sync Now", GUILayout.Width(80)))
            {
                vectorToAnimation.SyncWithClusters();
                EditorUtility.SetDirty(vectorToAnimation);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Check if we have any clusters
            var clusterTags = vectorToAnimation.ClustersComponent.GetClusterTags().ToList();
            if (clusterTags.Count == 0)
            {
                EditorGUILayout.HelpBox("No clusters found in the Clusters component. Please add clusters first.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            // Draw mappings
            mappingScrollPosition = EditorGUILayout.BeginScrollView(mappingScrollPosition, GUILayout.MaxHeight(400));

            foreach (string clusterTag in clusterTags)
            {
                DrawTagMapping(clusterTag);
            }

            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }

        private void DrawTagMapping(string clusterTag)
        {
            // Find or create tag mapping
            var tagMapping = vectorToAnimation.TagAnimationMappings.FirstOrDefault(t => t.ClusterTag == clusterTag);
            if (tagMapping == null)
            {
                tagMapping = new TagAnimationMapping(clusterTag);
                vectorToAnimation.TagAnimationMappings.Add(tagMapping);
            }

            EditorGUILayout.BeginVertical("box");

            // Tag label
            EditorGUILayout.LabelField($"Tag: \"{clusterTag}\"", EditorStyles.boldLabel);

            // Animation triggers selection
            EditorGUILayout.LabelField("Animation Triggers:", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            bool mappingChanged = false;
            var currentTriggers = tagMapping.AnimationTriggers ?? new List<string>();

            foreach (string trigger in availableTriggers)
            {
                bool isSelected = currentTriggers.Contains(trigger);
                bool newSelection = EditorGUILayout.Toggle(trigger, isSelected);

                if (newSelection != isSelected)
                {
                    mappingChanged = true;
                    if (newSelection)
                    {
                        if (!currentTriggers.Contains(trigger))
                            currentTriggers.Add(trigger);
                    }
                    else
                    {
                        currentTriggers.Remove(trigger);
                    }
                }
            }

            if (mappingChanged)
            {
                tagMapping.AnimationTriggers = currentTriggers;
                EditorUtility.SetDirty(vectorToAnimation);
            }

            EditorGUI.indentLevel--;

            // Show selected triggers summary
            if (currentTriggers.Count > 0)
            {
                string selectedTriggersText = string.Join(", ", currentTriggers);
                EditorGUILayout.LabelField($"Selected: {selectedTriggersText}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No triggers selected", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void RefreshAvailableTriggers()
        {
            if (vectorToAnimation != null)
            {
                availableTriggers = vectorToAnimation.GetAvailableAnimationTriggers();
            }
            else
            {
                availableTriggers.Clear();
            }
        }

        private void CheckForLegacyUsage()
        {
            // This is a simple check - in a real implementation you might want to check for actual usage
            // For now, we'll assume legacy usage if the obsolete field is still visible in the inspector
            showLegacyWarning = false; // We've marked the field as obsolete, so assume new system
        }
    }
}
