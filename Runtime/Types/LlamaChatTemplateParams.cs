using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeChatTemplateParams
    {
        [MarshalAs(UnmanagedType.LPStr)] public string chat_template;
        public bool use_models_chat_template;
    }

    [Serializable]
    public class LlamaChatTemplateParams
    {
        [SerializeField] public string chatTemplate = "";
        [SerializeField] public bool useModelsChatTemplate = true;

        // Default constructor
        public LlamaChatTemplateParams() {}

        public LlamaChatTemplateParams(string templateString)
        {
            chatTemplate = templateString;
            useModelsChatTemplate = string.IsNullOrEmpty(templateString);
        }

        public NativeChatTemplateParams ToNative()
        {
            return new NativeChatTemplateParams
            {
                chat_template = chatTemplate,
                use_models_chat_template = useModelsChatTemplate
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static LlamaChatTemplateParams FromJson(string json)
        {
            return JsonUtility.FromJson<LlamaChatTemplateParams>(json);
        }
    }
}