using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Aviad.Editor
{
    [CustomPropertyDrawer(typeof(LlamaGenerationConfig))]
    public class LlamaGenerationConfigDrawer : PropertyDrawer
    {
        private Dictionary<SamplerType, bool> samplerFoldoutStates = new Dictionary<SamplerType, bool>();

        // To prevent overlap when multiple MonoBehaviours with this serialized field are shown,
        // we use a static dictionary keyed by property.propertyPath to track foldout states independently.
        private static Dictionary<string, bool> basicParamsFoldoutStates = new Dictionary<string, bool>();
        private static Dictionary<string, Dictionary<SamplerType, bool>> samplerFoldoutsByProperty = new Dictionary<string, Dictionary<SamplerType, bool>>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float yPos = position.y;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Unique key for this property instance to track foldouts separately per field
            string key = property.propertyPath;

            if (!basicParamsFoldoutStates.ContainsKey(key))
                basicParamsFoldoutStates[key] = true;

            if (!samplerFoldoutsByProperty.ContainsKey(key))
                samplerFoldoutsByProperty[key] = new Dictionary<SamplerType, bool>();

            // Basic Parameters Section Foldout
            basicParamsFoldoutStates[key] = EditorGUI.Foldout(new Rect(position.x, yPos, position.width, lineHeight), basicParamsFoldoutStates[key], "Basic Parameters", true);
            yPos += lineHeight + spacing;

            if (basicParamsFoldoutStates[key])
            {
                EditorGUI.indentLevel++;
                DrawBasicParameters(position, property, ref yPos);
                EditorGUI.indentLevel--;
            }

            // Draw sampler chain property field
            var samplerChainProp = property.FindPropertyRelative("samplerChain");
            if (samplerChainProp != null)
            {
                var rect = new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(samplerChainProp));
                EditorGUI.PropertyField(rect, samplerChainProp, new GUIContent("Sampler Chain", "Order of samplers to apply during generation"), true);
                yPos += EditorGUI.GetPropertyHeight(samplerChainProp) + spacing;
            }

            // Draw dynamic sampler sections based on sampler chain
            var samplerTypes = GetSamplerTypesInChain(property);
            foreach (var samplerType in samplerTypes)
            {
                // Get or create foldout state for this sampler in this property instance
                var samplerFoldouts = samplerFoldoutsByProperty[key];
                if (!samplerFoldouts.ContainsKey(samplerType))
                    samplerFoldouts[samplerType] = false;

                bool isExpanded = samplerFoldouts[samplerType];

                // Draw foldout for sampler section
                string displayName = GetSamplerDisplayName(samplerType);
                isExpanded = EditorGUI.Foldout(new Rect(position.x, yPos, position.width, lineHeight), isExpanded, displayName, true);
                samplerFoldouts[samplerType] = isExpanded;
                yPos += lineHeight + spacing;

                if (isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawSamplerParameters(position, property, samplerType, ref yPos);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private void DrawBasicParameters(Rect position, SerializedProperty property, ref float yPos)
        {
            DrawIntField(position, property, "maxTokens", "Max Tokens", ref yPos,
                tooltip: "Maximum number of tokens to generate.");

            DrawIntField(position, property, "chunkSize", "Chunk Size", ref yPos,
                tooltip: "Chunk size to use for streaming callback. 0 = disabled.");

            SerializedProperty chatTemplateParamsProperty = property.FindPropertyRelative("chatTemplateParams");

            if (chatTemplateParamsProperty != null)
            {
                // Draw the nested properties of LlamaChatTemplateParams
                DrawStringField(position, chatTemplateParamsProperty, "chatTemplate", "Chat Template", ref yPos,
                    tooltip: "Custom chat template string. Leave empty to use model's default.");

                DrawBoolField(position, chatTemplateParamsProperty, "useModelsChatTemplate", "Use Model's Chat Template", ref yPos,
                    tooltip: "Whether to use the model's built-in chat template.");
            }
        }

        private SamplerType[] GetSamplerTypesInChain(SerializedProperty property)
        {
            var samplerChainProp = property.FindPropertyRelative("samplerChain");
            if (samplerChainProp == null) return new SamplerType[0];

            SamplerType[] types = new SamplerType[samplerChainProp.arraySize];
            for (int i = 0; i < samplerChainProp.arraySize; i++)
            {
                var element = samplerChainProp.GetArrayElementAtIndex(i);
                types[i] = (SamplerType)element.enumValueIndex;
            }
            return types;
        }

        private string GetSamplerDisplayName(SamplerType samplerType)
        {
            return samplerType switch
            {
                SamplerType.Greedy => "Greedy Sampling",
                SamplerType.Dist => "Distribution Sampling",
                SamplerType.TopK => "Top-K Sampling",
                SamplerType.TopP => "Top-P (Nucleus) Sampling",
                SamplerType.MinP => "Min-P Sampling",
                SamplerType.Typical => "Typical Sampling",
                SamplerType.Temperature => "Temperature Sampling",
                SamplerType.TemperatureExt => "Extended Temperature",
                SamplerType.XTC => "XTC Sampling",
                SamplerType.TopNSigma => "Top-N Sigma",
                SamplerType.Mirostat => "Mirostat",
                SamplerType.MirostatV2 => "Mirostat V2",
                SamplerType.Grammar => "Grammar Constraints",
                SamplerType.GrammarLazyPatterns => "Grammar Lazy Patterns",
                SamplerType.Penalties => "Repetition Penalties",
                SamplerType.Dry => "DRY (Don't Repeat Yourself)",
                SamplerType.LogitBias => "Logit Bias",
                SamplerType.Infill => "Infill",
                _ => samplerType.ToString(),
            };
        }

        private void DrawSamplerParameters(Rect position, SerializedProperty property, SamplerType samplerType, ref float yPos)
        {
            switch (samplerType)
            {
                case SamplerType.Temperature:
                    DrawFloatField(position, property, "temperature", "Temperature", ref yPos,
                        tooltip: "Controls randomness. Lower = more deterministic, higher = more random.");
                    break;

                case SamplerType.TemperatureExt:
                    DrawFloatField(position, property, "tempExtT", "Extended Temperature T", ref yPos);
                    DrawFloatField(position, property, "tempExtDelta", "Extended Temperature Delta", ref yPos);
                    DrawFloatField(position, property, "tempExtExponent", "Extended Temperature Exponent", ref yPos);
                    break;

                case SamplerType.TopK:
                    DrawIntField(position, property, "topK", "Top-K", ref yPos,
                        tooltip: "Limit to top K most likely tokens. 0 = disabled.");
                    break;

                case SamplerType.TopP:
                    DrawFloatField(position, property, "topP", "Top-P", ref yPos,
                        tooltip: "Nucleus sampling threshold. 1.0 = disabled.");
                    DrawUIntField(position, property, "topPMinKeep", "Top-P Min Keep", ref yPos);
                    break;

                case SamplerType.MinP:
                    DrawFloatField(position, property, "minP", "Min-P", ref yPos,
                        tooltip: "Minimum probability threshold relative to the most likely token.");
                    DrawUIntField(position, property, "minPMinKeep", "Min-P Min Keep", ref yPos);
                    break;

                case SamplerType.Typical:
                    DrawFloatField(position, property, "typicalP", "Typical-P", ref yPos,
                        tooltip: "Typical sampling parameter. 1.0 = disabled.");
                    DrawUIntField(position, property, "typicalMinKeep", "Typical Min Keep", ref yPos);
                    break;

                case SamplerType.TopNSigma:
                    DrawFloatField(position, property, "topNSigma", "Top-N Sigma", ref yPos);
                    break;

                case SamplerType.Mirostat:
                case SamplerType.MirostatV2:
                    DrawMirostatParameters(position, property, ref yPos);
                    break;

                case SamplerType.Grammar:
                case SamplerType.GrammarLazyPatterns:
                    DrawGrammarParameters(position, property, ref yPos);
                    break;

                case SamplerType.Penalties:
                    DrawPenaltyParameters(position, property, ref yPos);
                    break;

                case SamplerType.Dry:
                    DrawDryParameters(position, property, ref yPos);
                    break;

                case SamplerType.Dist:
                    DrawUIntField(position, property, "distSeed", "Distribution Seed", ref yPos);
                    break;

                case SamplerType.XTC:
                    DrawFloatField(position, property, "xtcP", "XTC P", ref yPos);
                    DrawFloatField(position, property, "xtcT", "XTC T", ref yPos);
                    DrawUIntField(position, property, "xtcMinKeep", "XTC Min Keep", ref yPos);
                    DrawUIntField(position, property, "xtcSeed", "XTC Seed", ref yPos);
                    break;

                default:
                    break;
            }
        }

        private void DrawMirostatParameters(Rect position, SerializedProperty property, ref float yPos)
        {
            DrawIntField(position, property, "mirostatNVocab", "Mirostat N Vocab", ref yPos);
            DrawUIntField(position, property, "mirostatSeed", "Mirostat Seed", ref yPos);
            DrawFloatField(position, property, "mirostatTau", "Mirostat Tau", ref yPos,
                tooltip: "Target entropy for Mirostat.");
            DrawFloatField(position, property, "mirostatEta", "Mirostat Eta", ref yPos,
                tooltip: "Learning rate for Mirostat.");
            DrawIntField(position, property, "mirostatM", "Mirostat M", ref yPos);

            DrawUIntField(position, property, "mirostatV2Seed", "Mirostat V2 Seed", ref yPos);
            DrawFloatField(position, property, "mirostatV2Tau", "Mirostat V2 Tau", ref yPos);
            DrawFloatField(position, property, "mirostatV2Eta", "Mirostat V2 Eta", ref yPos);
        }

        private void DrawGrammarParameters(Rect position, SerializedProperty property, ref float yPos)
        {
            DrawStringField(position, property, "grammarStr", "Grammar String", ref yPos,
                tooltip: "GBNF grammar string for constrained generation.");
            DrawStringField(position, property, "grammarRoot", "Grammar Root", ref yPos,
                tooltip: "Root rule name for the grammar.");

            var grammarPatternsProp = property.FindPropertyRelative("grammarTriggerPatterns");
            if (grammarPatternsProp != null)
            {
                var rect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(rect, grammarPatternsProp, new GUIContent("Grammar Trigger Patterns"), true);
                yPos += EditorGUI.GetPropertyHeight(grammarPatternsProp) + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private void DrawPenaltyParameters(Rect position, SerializedProperty property, ref float yPos)
        {
            DrawIntField(position, property, "penaltyLastN", "Penalty Last N", ref yPos);
            DrawFloatField(position, property, "penaltyRepeat", "Penalty Repeat", ref yPos);
            DrawFloatField(position, property, "penaltyMismatch", "Penalty Mismatch", ref yPos);
            DrawFloatField(position, property, "penaltyOverlap", "Penalty Overlap", ref yPos);
            DrawBoolField(position, property, "penaltyWord", "Penalty Word", ref yPos);
        }

        private void DrawDryParameters(Rect position, SerializedProperty property, ref float yPos)
        {
            DrawIntField(position, property, "dryWindowSize", "DRY Window Size", ref yPos);
            DrawFloatField(position, property, "dryRepeatPenalty", "DRY Repeat Penalty", ref yPos);
            DrawFloatField(position, property, "dryAlpha", "DRY Alpha", ref yPos);
            DrawBoolField(position, property, "dryIgnoreNewlines", "DRY Ignore Newlines", ref yPos);
        }

        private void DrawIntField(Rect position, SerializedProperty property, string fieldName, string label, ref float yPos, string tooltip = null)
        {
            var prop = property.FindPropertyRelative(fieldName);
            if (prop == null) return;
            var rect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            var content = new GUIContent(label, tooltip);
            EditorGUI.PropertyField(rect, prop, content);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void DrawUIntField(Rect position, SerializedProperty property, string fieldName, string label, ref float yPos, string tooltip = null)
        {
            var prop = property.FindPropertyRelative(fieldName);
            if (prop == null) return;
            var rect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            var content = new GUIContent(label, tooltip);
            EditorGUI.PropertyField(rect, prop, content);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void DrawFloatField(Rect position, SerializedProperty property, string fieldName, string label, ref float yPos, string tooltip = null)
        {
            var prop = property.FindPropertyRelative(fieldName);
            if (prop == null) return;
            var rect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            var content = new GUIContent(label, tooltip);
            EditorGUI.PropertyField(rect, prop, content);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void DrawBoolField(Rect position, SerializedProperty property, string fieldName, string label, ref float yPos, string tooltip = null)
        {
            var prop = property.FindPropertyRelative(fieldName);
            if (prop == null) return;
            var rect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            var content = new GUIContent(label, tooltip);
            EditorGUI.PropertyField(rect, prop, content);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void DrawStringField(Rect position, SerializedProperty property, string fieldName, string label, ref float yPos, string tooltip = null)
        {
            var prop = property.FindPropertyRelative(fieldName);
            if (prop == null) return;
            var rect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            var content = new GUIContent(label, tooltip);
            EditorGUI.PropertyField(rect, prop, content);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = 0f;

            string key = property.propertyPath;

            // Basic Parameters foldout
            if (!basicParamsFoldoutStates.ContainsKey(key))
                basicParamsFoldoutStates[key] = true;

            height += lineHeight + spacing; // Foldout line for basic parameters

            if (basicParamsFoldoutStates[key])
            {
                // 4 basic fields
                height += (lineHeight + spacing) * 4;
            }

            // Sampler Chain property height
            var samplerChainProp = property.FindPropertyRelative("samplerChain");
            if (samplerChainProp != null)
            {
                height += EditorGUI.GetPropertyHeight(samplerChainProp) + spacing;
            }

            // Sampler sections heights
            var samplerTypes = GetSamplerTypesInChain(property);
            if (!samplerFoldoutsByProperty.ContainsKey(key))
                samplerFoldoutsByProperty[key] = new Dictionary<SamplerType, bool>();
            var samplerFoldouts = samplerFoldoutsByProperty[key];

            foreach (var samplerType in samplerTypes)
            {
                // foldout line height
                height += lineHeight + spacing;

                bool isExpanded = samplerFoldouts.ContainsKey(samplerType) && samplerFoldouts[samplerType];

                if (isExpanded)
                {
                    height += GetSamplerParametersHeight(samplerType, property) + spacing;
                }
            }

            return height;
        }

        private float GetSamplerParametersHeight(SamplerType samplerType, SerializedProperty property)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            int paramCount = samplerType switch
            {
                SamplerType.Temperature => 1,

                SamplerType.TemperatureExt => 3,

                SamplerType.TopK => 1,

                SamplerType.TopP => 2,

                SamplerType.MinP => 2,

                SamplerType.Typical => 2,

                SamplerType.TopNSigma => 1,

                SamplerType.Mirostat => 8, // 4 + 4 params (V1 and V2)

                SamplerType.MirostatV2 => 8, // same as Mirostat

                SamplerType.Grammar => 3, // 2 strings + 1 list

                SamplerType.GrammarLazyPatterns => 3,

                SamplerType.Penalties => 5,

                SamplerType.Dry => 4,

                SamplerType.Dist => 1,

                SamplerType.XTC => 4,

                _ => 0,
            };

            // Each param line + spacing
            return paramCount * (lineHeight + spacing);
        }
    }
}