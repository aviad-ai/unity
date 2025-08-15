using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{

    public enum SamplerType
    {
        Greedy = 0,
        Dist = 1,
        TopK = 2,
        TopP = 3,
        MinP = 4,
        Typical = 5,
        Temperature = 6,
        TemperatureExt = 7,
        XTC = 8,
        TopNSigma = 9,
        Mirostat = 10,
        MirostatV2 = 11,
        Grammar = 12,
        GrammarLazyPatterns = 13,
        Penalties = 14,
        Dry = 15,
        LogitBias = 16,
        Infill = 17
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeLlamaSamplerParameters
    {
        // dist
        [MarshalAs(UnmanagedType.U4)] public uint dist_seed;

        // top_k
        [MarshalAs(UnmanagedType.I4)] public int top_k;

        // top_p, min_p, typical
        [MarshalAs(UnmanagedType.R4)] public float top_p;
        [MarshalAs(UnmanagedType.U4)] public uint top_p_min_keep;

        [MarshalAs(UnmanagedType.R4)] public float min_p;
        [MarshalAs(UnmanagedType.U4)] public uint min_p_min_keep;

        [MarshalAs(UnmanagedType.R4)] public float typical_p;
        [MarshalAs(UnmanagedType.U4)] public uint typical_min_keep;

        // temperature
        [MarshalAs(UnmanagedType.R4)] public float temperature;

        // temperature_ext
        [MarshalAs(UnmanagedType.R4)] public float temp_ext_t;
        [MarshalAs(UnmanagedType.R4)] public float temp_ext_delta;
        [MarshalAs(UnmanagedType.R4)] public float temp_ext_exponent;

        // xtc
        [MarshalAs(UnmanagedType.R4)] public float xtc_p;
        [MarshalAs(UnmanagedType.R4)] public float xtc_t;
        [MarshalAs(UnmanagedType.U4)] public uint xtc_min_keep;
        [MarshalAs(UnmanagedType.U4)] public uint xtc_seed;

        // top_n_sigma
        [MarshalAs(UnmanagedType.R4)] public float top_n_sigma;

        // mirostat
        [MarshalAs(UnmanagedType.I4)] public int mirostat_n_vocab;
        [MarshalAs(UnmanagedType.U4)] public uint mirostat_seed;
        [MarshalAs(UnmanagedType.R4)] public float mirostat_tau;
        [MarshalAs(UnmanagedType.R4)] public float mirostat_eta;
        [MarshalAs(UnmanagedType.I4)] public int mirostat_m;

        // mirostat v2
        [MarshalAs(UnmanagedType.U4)] public uint mirostat_v2_seed;
        [MarshalAs(UnmanagedType.R4)] public float mirostat_v2_tau;
        [MarshalAs(UnmanagedType.R4)] public float mirostat_v2_eta;

        // grammar
        [MarshalAs(UnmanagedType.LPStr)] public string grammar_str;
        [MarshalAs(UnmanagedType.LPStr)] public string grammar_root;

        // grammar_lazy_patterns
        public IntPtr grammar_trigger_patterns; // const char**
        [MarshalAs(UnmanagedType.U4)] public uint grammar_num_trigger_patterns;

        // penalties
        [MarshalAs(UnmanagedType.I4)] public int penalty_last_n;
        [MarshalAs(UnmanagedType.R4)] public float penalty_repeat;
        [MarshalAs(UnmanagedType.R4)] public float penalty_freq;
        [MarshalAs(UnmanagedType.R4)] public float penalty_present;

        // dry
        [MarshalAs(UnmanagedType.I4)] public int dry_n_ctx_train;
        [MarshalAs(UnmanagedType.R4)] public float dry_multiplier;
        [MarshalAs(UnmanagedType.R4)] public float dry_base;
        [MarshalAs(UnmanagedType.I4)] public int dry_allowed_length;
        [MarshalAs(UnmanagedType.I4)] public int dry_penalty_last_n;
        public IntPtr dry_seq_breakers; // const char**
        [MarshalAs(UnmanagedType.U4)] public uint dry_num_breakers;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeLlamaSamplerChain
    {
        public IntPtr types; // sampler_type_t*
        [MarshalAs(UnmanagedType.U4)] public uint count;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeLlamaGenerationParams
    {
        public NativeLlamaSamplerParameters sampler_params;
        public NativeLlamaSamplerChain sampler_chain;
        public NativeChatTemplateParams template_params;
        [MarshalAs(UnmanagedType.U4)] public uint max_tokens;
        [MarshalAs(UnmanagedType.U4)] public uint chunk_size;
    }

    [Serializable]
    public class LlamaGenerationConfig
    {
        [SerializeField] public LlamaChatTemplateParams chatTemplateParams = new LlamaChatTemplateParams();
        [SerializeField] public uint maxTokens = 256;
        [SerializeField] public uint chunkSize = 1;
        [SerializeField] public SamplerType[] samplerChain = { SamplerType.Temperature, SamplerType.Dist };

        // Sampler parameters
        [SerializeField] public uint distSeed = 0;
        [SerializeField] public int topK = 40;
        [SerializeField] public float topP = 0.9f;
        [SerializeField] public uint topPMinKeep = 1;
        [SerializeField] public float minP = 0.05f;
        [SerializeField] public uint minPMinKeep = 1;
        [SerializeField] public float typicalP = 1.0f;
        [SerializeField] public uint typicalMinKeep = 1;
        [SerializeField] public float temperature = 0.7f;
        [SerializeField] public float tempExtT = 1.0f;
        [SerializeField] public float tempExtDelta = 0.0f;
        [SerializeField] public float tempExtExponent = 1.0f;
        [SerializeField] public float xtcP = 0.0f;
        [SerializeField] public float xtcT = 0.0f;
        [SerializeField] public uint xtcMinKeep = 1;
        [SerializeField] public uint xtcSeed = 0;
        [SerializeField] public float topNSigma = 1.0f;
        [SerializeField] public int mirostatNVocab = 0;
        [SerializeField] public uint mirostatSeed = 0;
        [SerializeField] public float mirostatTau = 5.0f;
        [SerializeField] public float mirostatEta = 0.1f;
        [SerializeField] public int mirostatM = 100;
        [SerializeField] public uint mirostatV2Seed = 0;
        [SerializeField] public float mirostatV2Tau = 5.0f;
        [SerializeField] public float mirostatV2Eta = 0.1f;
        [SerializeField] public string grammarStr = "";
        [SerializeField] public string grammarRoot = "";
        [SerializeField] public string[] grammarTriggerPatterns = new string[0];
        [SerializeField] public int penaltyLastN = 64;
        [SerializeField] public float penaltyRepeat = 1.0f;
        [SerializeField] public float penaltyFreq = 0.0f;
        [SerializeField] public float penaltyPresent = 0.0f;
        [SerializeField] public int dryNCtxTrain = 1024;
        [SerializeField] public float dryMultiplier = 0.0f;
        [SerializeField] public float dryBase = 1.75f;
        [SerializeField] public int dryAllowedLength = 2;
        [SerializeField] public int dryPenaltyLastN = -1;
        [SerializeField] public string[] drySeqBreakers = new string[0];

        // Default constructor
        public LlamaGenerationConfig() {}

        public NativeLlamaGenerationConfigWrapper ToNative()
        {
            return new NativeLlamaGenerationConfigWrapper(this);
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

    // Wrapper class to handle memory management for native generation config
    public class NativeLlamaGenerationConfigWrapper : IDisposable
    {
        private NativeLlamaGenerationParams _nativeConfig;
        private IntPtr[] _grammarPatternPtrs;
        private IntPtr[] _drySeqBreakerPtrs;
        private IntPtr _samplerChainPtr;
        private IntPtr _grammarPatternArrayPtr;
        private IntPtr _drySeqBreakerArrayPtr;
        private bool _disposed = false;

        public NativeLlamaGenerationParams Native => _nativeConfig;

        public NativeLlamaGenerationConfigWrapper(LlamaGenerationConfig config)
        {
            // Allocate sampler chain
            int chainCount = config.samplerChain.Length;
            _samplerChainPtr = Marshal.AllocHGlobal(sizeof(int) * chainCount);

            for (int i = 0; i < chainCount; i++)
            {
                Marshal.WriteInt32(_samplerChainPtr, i * sizeof(int), (int)config.samplerChain[i]);
            }

            // Allocate grammar trigger patterns
            int grammarPatternCount = config.grammarTriggerPatterns.Length;
            _grammarPatternPtrs = new IntPtr[grammarPatternCount];

            for (int i = 0; i < grammarPatternCount; i++)
            {
                _grammarPatternPtrs[i] = Marshal.StringToHGlobalAnsi(config.grammarTriggerPatterns[i]);
            }

            if (grammarPatternCount > 0)
            {
                _grammarPatternArrayPtr = Marshal.AllocHGlobal(IntPtr.Size * grammarPatternCount);
                for (int i = 0; i < grammarPatternCount; i++)
                {
                    Marshal.WriteIntPtr(_grammarPatternArrayPtr, i * IntPtr.Size, _grammarPatternPtrs[i]);
                }
            }

            // Allocate dry sequence breakers
            int dryBreakerCount = config.drySeqBreakers.Length;
            _drySeqBreakerPtrs = new IntPtr[dryBreakerCount];

            for (int i = 0; i < dryBreakerCount; i++)
            {
                _drySeqBreakerPtrs[i] = Marshal.StringToHGlobalAnsi(config.drySeqBreakers[i]);
            }

            if (dryBreakerCount > 0)
            {
                _drySeqBreakerArrayPtr = Marshal.AllocHGlobal(IntPtr.Size * dryBreakerCount);
                for (int i = 0; i < dryBreakerCount; i++)
                {
                    Marshal.WriteIntPtr(_drySeqBreakerArrayPtr, i * IntPtr.Size, _drySeqBreakerPtrs[i]);
                }
            }

            // Build the native config
            _nativeConfig = new NativeLlamaGenerationParams
            {
                sampler_params = new NativeLlamaSamplerParameters
                {
                    dist_seed = config.distSeed,
                    top_k = config.topK,
                    top_p = config.topP,
                    top_p_min_keep = config.topPMinKeep,
                    min_p = config.minP,
                    min_p_min_keep = config.minPMinKeep,
                    typical_p = config.typicalP,
                    typical_min_keep = config.typicalMinKeep,
                    temperature = config.temperature,
                    temp_ext_t = config.tempExtT,
                    temp_ext_delta = config.tempExtDelta,
                    temp_ext_exponent = config.tempExtExponent,
                    xtc_p = config.xtcP,
                    xtc_t = config.xtcT,
                    xtc_min_keep = config.xtcMinKeep,
                    xtc_seed = config.xtcSeed,
                    top_n_sigma = config.topNSigma,
                    mirostat_n_vocab = config.mirostatNVocab,
                    mirostat_seed = config.mirostatSeed,
                    mirostat_tau = config.mirostatTau,
                    mirostat_eta = config.mirostatEta,
                    mirostat_m = config.mirostatM,
                    mirostat_v2_seed = config.mirostatV2Seed,
                    mirostat_v2_tau = config.mirostatV2Tau,
                    mirostat_v2_eta = config.mirostatV2Eta,
                    grammar_str = config.grammarStr,
                    grammar_root = config.grammarRoot,
                    grammar_trigger_patterns = _grammarPatternArrayPtr,
                    grammar_num_trigger_patterns = (uint)grammarPatternCount,
                    penalty_last_n = config.penaltyLastN,
                    penalty_repeat = config.penaltyRepeat,
                    penalty_freq = config.penaltyFreq,
                    penalty_present = config.penaltyPresent,
                    dry_n_ctx_train = config.dryNCtxTrain,
                    dry_multiplier = config.dryMultiplier,
                    dry_base = config.dryBase,
                    dry_allowed_length = config.dryAllowedLength,
                    dry_penalty_last_n = config.dryPenaltyLastN,
                    dry_seq_breakers = _drySeqBreakerArrayPtr,
                    dry_num_breakers = (uint)dryBreakerCount
                },
                sampler_chain = new NativeLlamaSamplerChain
                {
                    types = _samplerChainPtr,
                    count = (uint)chainCount
                },
                template_params = config.chatTemplateParams.ToNative(),
                max_tokens = config.maxTokens,
                chunk_size = config.chunkSize,
            };
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Free sampler chain
            if (_samplerChainPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_samplerChainPtr);
                _samplerChainPtr = IntPtr.Zero;
            }

            // Free grammar pattern strings
            if (_grammarPatternPtrs != null)
            {
                foreach (var ptr in _grammarPatternPtrs)
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
                _grammarPatternPtrs = null;
            }

            // Free grammar pattern array
            if (_grammarPatternArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_grammarPatternArrayPtr);
                _grammarPatternArrayPtr = IntPtr.Zero;
            }

            // Free dry sequence breaker strings
            if (_drySeqBreakerPtrs != null)
            {
                foreach (var ptr in _drySeqBreakerPtrs)
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
                _drySeqBreakerPtrs = null;
            }

            // Free dry sequence breaker array
            if (_drySeqBreakerArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_drySeqBreakerArrayPtr);
                _drySeqBreakerArrayPtr = IntPtr.Zero;
            }

            _disposed = true;
        }

        ~NativeLlamaGenerationConfigWrapper()
        {
            Dispose();
        }
    }
}