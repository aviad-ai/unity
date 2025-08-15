using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Aviad.Editor
{
    [CustomPropertyDrawer(typeof(LlamaInitializationParams))]
    public class LlamaInitializationParamsDrawer : PropertyDrawer
    {
        // Track foldout states per property path to avoid overlap between multiple instances
        private static readonly Dictionary<string, FoldoutState> foldoutStates = new Dictionary<string, FoldoutState>();

        private class FoldoutState
        {
            public bool showModelParams = true;
            public bool showContextParams = true;
            public bool showAdvancedParams = false;
            public bool showRopeParams = false;
            public bool showYarnParams = false;
        }

        private FoldoutState GetFoldoutState(string key)
        {
            if (!foldoutStates.TryGetValue(key, out var state))
            {
                state = new FoldoutState();
                foldoutStates[key] = state;
            }
            return state;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Get foldout state unique for this property
            var foldoutState = GetFoldoutState(property.propertyPath);

            // Model Parameters Section
            foldoutState.showModelParams = EditorGUI.Foldout(rect, foldoutState.showModelParams, "Model Parameters", true);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            if (foldoutState.showModelParams)
            {
                EditorGUI.indentLevel++;

                rect = DrawOptionalIntField(rect, property, "gpuLayers", "GPU Layers");
                rect = DrawOptionalEnumField(rect, property, "splitMode", "Split Mode", typeof(LlamaSplitMode));
                rect = DrawOptionalIntField(rect, property, "mainGpu", "Main GPU");
                rect = DrawOptionalBoolField(rect, property, "vocabOnly", "Vocab Only");
                rect = DrawOptionalBoolField(rect, property, "useMmap", "Use Memory Mapping");
                rect = DrawOptionalBoolField(rect, property, "useMlock", "Use Memory Lock");
                rect = DrawOptionalBoolField(rect, property, "checkTensors", "Check Tensors");

                var enableAbortInitProp = property.FindPropertyRelative("enableAbortInit");
                if (enableAbortInitProp != null)
                {
                    EditorGUI.PropertyField(rect, enableAbortInitProp, new GUIContent("Enable Abort Init"));
                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                }

                EditorGUI.indentLevel--;
            }

            // Context Parameters Section
            foldoutState.showContextParams = EditorGUI.Foldout(rect, foldoutState.showContextParams, "Context Parameters", true);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            if (foldoutState.showContextParams)
            {
                EditorGUI.indentLevel++;

                rect = DrawOptionalUIntField(rect, property, "contextLength", "Context Length");
                rect = DrawOptionalUIntField(rect, property, "batchSize", "Batch Size");
                rect = DrawOptionalUIntField(rect, property, "ubatchSize", "Micro Batch Size");
                rect = DrawOptionalUIntField(rect, property, "maxSequences", "Max Sequences");
                rect = DrawOptionalIntField(rect, property, "threads", "Threads");
                rect = DrawOptionalIntField(rect, property, "threadsBatch", "Batch Threads");
                rect = DrawOptionalEnumField(rect, property, "poolingType", "Pooling Type", typeof(LlamaPoolingType));
                rect = DrawOptionalEnumField(rect, property, "attentionType", "Attention Type", typeof(LlamaAttentionType));
                rect = DrawOptionalBoolField(rect, property, "embeddings", "Embeddings");
                rect = DrawOptionalBoolField(rect, property, "offloadKqv", "Offload KQV");
                rect = DrawOptionalBoolField(rect, property, "flashAttn", "Flash Attention");

                var enableAbortGenProp = property.FindPropertyRelative("enableAbortGeneration");
                if (enableAbortGenProp != null)
                {
                    EditorGUI.PropertyField(rect, enableAbortGenProp, new GUIContent("Enable Abort Generation"));
                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                }

                EditorGUI.indentLevel--;
            }

            // RoPE Parameters Section
            foldoutState.showRopeParams = EditorGUI.Foldout(rect, foldoutState.showRopeParams, "RoPE Parameters", true);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            if (foldoutState.showRopeParams)
            {
                EditorGUI.indentLevel++;

                rect = DrawOptionalEnumField(rect, property, "ropeScalingType", "RoPE Scaling Type", typeof(LlamaRopeScalingType));
                rect = DrawOptionalFloatField(rect, property, "ropeFreqBase", "RoPE Frequency Base");
                rect = DrawOptionalFloatField(rect, property, "ropeFreqScale", "RoPE Frequency Scale");

                EditorGUI.indentLevel--;
            }

            // YARN Parameters Section
            foldoutState.showYarnParams = EditorGUI.Foldout(rect, foldoutState.showYarnParams, "YARN Parameters", true);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            if (foldoutState.showYarnParams)
            {
                EditorGUI.indentLevel++;

                rect = DrawOptionalFloatField(rect, property, "yarnExtFactor", "YARN Ext Factor");
                rect = DrawOptionalFloatField(rect, property, "yarnAttnFactor", "YARN Attention Factor");
                rect = DrawOptionalFloatField(rect, property, "yarnBetaFast", "YARN Beta Fast");
                rect = DrawOptionalFloatField(rect, property, "yarnBetaSlow", "YARN Beta Slow");
                rect = DrawOptionalUIntField(rect, property, "yarnOrigCtx", "YARN Original Context");

                EditorGUI.indentLevel--;
            }

            // Advanced Parameters Section
            foldoutState.showAdvancedParams = EditorGUI.Foldout(rect, foldoutState.showAdvancedParams, "Advanced Parameters", true);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            if (foldoutState.showAdvancedParams)
            {
                EditorGUI.indentLevel++;

                rect = DrawOptionalFloatField(rect, property, "defragThreshold", "Defrag Threshold");
                rect = DrawOptionalBoolField(rect, property, "noPerf", "No Performance Metrics");
                rect = DrawOptionalBoolField(rect, property, "opOffload", "Operation Offload");
                rect = DrawOptionalBoolField(rect, property, "swaFull", "SWA Full");

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var foldoutState = GetFoldoutState(property.propertyPath);

            float height = 0f;
            height += EditorGUIUtility.singleLineHeight + 2; // Model params foldout

            if (foldoutState.showModelParams)
            {
                height += (EditorGUIUtility.singleLineHeight + 2) * 8; // 8 model param fields
                if (property.FindPropertyRelative("enableAbortInit") != null)
                    height += EditorGUIUtility.singleLineHeight + 2;
            }

            height += EditorGUIUtility.singleLineHeight + 2; // Context params foldout

            if (foldoutState.showContextParams)
            {
                height += (EditorGUIUtility.singleLineHeight + 2) * 12; // 12 context param fields
                if (property.FindPropertyRelative("enableAbortGeneration") != null)
                    height += EditorGUIUtility.singleLineHeight + 2;
            }

            height += EditorGUIUtility.singleLineHeight + 2; // RoPE params foldout

            if (foldoutState.showRopeParams)
            {
                height += (EditorGUIUtility.singleLineHeight + 2) * 3; // 3 RoPE param fields
            }

            height += EditorGUIUtility.singleLineHeight + 2; // YARN params foldout

            if (foldoutState.showYarnParams)
            {
                height += (EditorGUIUtility.singleLineHeight + 2) * 5; // 5 YARN param fields
            }

            height += EditorGUIUtility.singleLineHeight + 2; // Advanced params foldout

            if (foldoutState.showAdvancedParams)
            {
                height += (EditorGUIUtility.singleLineHeight + 2) * 4; // 4 advanced param fields
            }

            return height;
        }

        private Rect DrawOptionalIntField(Rect rect, SerializedProperty property, string fieldName, string displayName)
        {
            var fieldProp = property.FindPropertyRelative(fieldName);
            if (fieldProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Field not found");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var hasValueProp = fieldProp.FindPropertyRelative("hasValue");
            var valueProp = fieldProp.FindPropertyRelative("value");

            if (hasValueProp == null || valueProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Property structure error");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var toggleRect = new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var fieldRect = new Rect(rect.x + EditorGUIUtility.singleLineHeight + 5, rect.y, rect.width - EditorGUIUtility.singleLineHeight - 5, rect.height);

            // Draw toggle - always hasValue to prevent editor bugs
            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, GUIContent.none, hasValueProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProp.boolValue = toggleValue;
            }

            // Draw value field - disabled when toggle is off
            using (new EditorGUI.DisabledScope(!hasValueProp.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                int fieldValue = EditorGUI.IntField(fieldRect, displayName, valueProp.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    valueProp.intValue = fieldValue;
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2;
            return rect;
        }

        private Rect DrawOptionalUIntField(Rect rect, SerializedProperty property, string fieldName, string displayName)
        {
            var fieldProp = property.FindPropertyRelative(fieldName);
            if (fieldProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Field not found");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var hasValueProp = fieldProp.FindPropertyRelative("hasValue");
            var valueProp = fieldProp.FindPropertyRelative("value");

            if (hasValueProp == null || valueProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Property structure error");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var toggleRect = new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var fieldRect = new Rect(rect.x + EditorGUIUtility.singleLineHeight + 5, rect.y, rect.width - EditorGUIUtility.singleLineHeight - 5, rect.height);

            // Draw toggle - always hasValue to prevent editor bugs
            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, GUIContent.none, hasValueProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProp.boolValue = toggleValue;
            }

            // Draw value field - disabled when toggle is off
            using (new EditorGUI.DisabledScope(!hasValueProp.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                uint fieldValue = (uint)Mathf.Max(0, EditorGUI.IntField(fieldRect, displayName, (int)valueProp.uintValue));
                if (EditorGUI.EndChangeCheck())
                {
                    valueProp.uintValue = fieldValue;
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2;
            return rect;
        }

        private Rect DrawOptionalFloatField(Rect rect, SerializedProperty property, string fieldName, string displayName)
        {
            var fieldProp = property.FindPropertyRelative(fieldName);
            if (fieldProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Field not found");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var hasValueProp = fieldProp.FindPropertyRelative("hasValue");
            var valueProp = fieldProp.FindPropertyRelative("value");

            if (hasValueProp == null || valueProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Property structure error");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var toggleRect = new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var fieldRect = new Rect(rect.x + EditorGUIUtility.singleLineHeight + 5, rect.y, rect.width - EditorGUIUtility.singleLineHeight - 5, rect.height);

            // Draw toggle - always hasValue to prevent editor bugs
            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, GUIContent.none, hasValueProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProp.boolValue = toggleValue;
            }

            // Draw value field - disabled when toggle is off
            using (new EditorGUI.DisabledScope(!hasValueProp.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                float fieldValue = EditorGUI.FloatField(fieldRect, displayName, valueProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    valueProp.floatValue = fieldValue;
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2;
            return rect;
        }

        private Rect DrawOptionalBoolField(Rect rect, SerializedProperty property, string fieldName, string displayName)
        {
            var fieldProp = property.FindPropertyRelative(fieldName);
            if (fieldProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Field not found");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var hasValueProp = fieldProp.FindPropertyRelative("hasValue");
            var valueProp = fieldProp.FindPropertyRelative("value");

            if (hasValueProp == null || valueProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Property structure error");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var toggleRect = new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var fieldRect = new Rect(rect.x + EditorGUIUtility.singleLineHeight + 5, rect.y, rect.width - EditorGUIUtility.singleLineHeight - 5, rect.height);

            // Draw toggle - always hasValue to prevent editor bugs
            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, GUIContent.none, hasValueProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProp.boolValue = toggleValue;
            }

            // Draw value field - disabled when toggle is off
            using (new EditorGUI.DisabledScope(!hasValueProp.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                bool fieldValue = EditorGUI.Toggle(fieldRect, displayName, valueProp.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    valueProp.boolValue = fieldValue;
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2;
            return rect;
        }

        private Rect DrawOptionalEnumField(Rect rect, SerializedProperty property, string fieldName, string displayName, System.Type enumType)
        {
            var fieldProp = property.FindPropertyRelative(fieldName);
            if (fieldProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Field not found");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var hasValueProp = fieldProp.FindPropertyRelative("hasValue");
            var valueProp = fieldProp.FindPropertyRelative("value");

            if (hasValueProp == null || valueProp == null)
            {
                EditorGUI.LabelField(rect, displayName, "Property structure error");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                return rect;
            }

            var toggleRect = new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var fieldRect = new Rect(rect.x + EditorGUIUtility.singleLineHeight + 5, rect.y, rect.width - EditorGUIUtility.singleLineHeight - 5, rect.height);

            // Draw toggle - always hasValue to prevent editor bugs
            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, GUIContent.none, hasValueProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProp.boolValue = toggleValue;
            }

            // Draw value field - disabled when toggle is off
            using (new EditorGUI.DisabledScope(!hasValueProp.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                var currentValue = valueProp.intValue;
                var enumValue = (System.Enum)System.Enum.ToObject(enumType, currentValue);
                var newEnumValue = EditorGUI.EnumPopup(fieldRect, displayName, enumValue);
                if (EditorGUI.EndChangeCheck())
                {
                    valueProp.intValue = System.Convert.ToInt32(newEnumValue);
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2;
            return rect;
        }
    }
}