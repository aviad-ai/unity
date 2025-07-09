using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeLlamaGenerationConfig
    {
        [MarshalAs(UnmanagedType.LPStr)] public string chat_template;
        [MarshalAs(UnmanagedType.LPStr)] public string grammar_string;
        public bool use_grammar_string;
        public float temperature;
        public float top_p;
        public int max_tokens;
    }

    // Serializable Unity-friendly version
    [Serializable]
    public class LlamaGenerationConfig
    {
        [SerializeField] public string chatTemplate = "";
        [SerializeField] public string grammarString = "";
        [SerializeField] public float temperature = 0.7f;
        [SerializeField] public float topP = 0.9f;
        [SerializeField] public int maxTokens = 256;

        // Default constructor
        public LlamaGenerationConfig()
        {
        }

        // Constructor from struct
        public LlamaGenerationConfig(NativeLlamaGenerationConfig config)
        {
            chatTemplate = config.chat_template;
            grammarString = config.use_grammar_string ? config.grammar_string : "";
            temperature = config.temperature;
            topP = config.top_p;
            maxTokens = config.max_tokens;
        }

        // Constructor with parameters
        public LlamaGenerationConfig(string chatTemplate = "", string grammarString = "", 
            float temperature = 0.7f, float topP = 0.9f, int maxTokens = 256)
        {
            this.chatTemplate = chatTemplate;
            this.grammarString = grammarString;
            this.temperature = temperature;
            this.topP = topP;
            this.maxTokens = maxTokens;
        }

        public NativeLlamaGenerationConfig ToStruct()
        {
            return new NativeLlamaGenerationConfig()
            {
                chat_template = chatTemplate,
                grammar_string = grammarString,
                use_grammar_string = !string.IsNullOrEmpty(grammarString),
                temperature = temperature,
                top_p = topP,
                max_tokens = maxTokens
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static LlamaGenerationConfig FromJson(string json)
        {
            return JsonUtility.FromJson<LlamaGenerationConfig>(json);
        }
    }
}