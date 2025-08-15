using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    // Enums matching llama.cpp definitions
    public enum LlamaRopeScalingType
    {
        Unspecified = -1,
        None = 0,
        Linear = 1,
        Yarn = 2,
        LongRope = 3
    }

    public enum LlamaPoolingType
    {
        Unspecified = -1,
        None = 0,
        Mean = 1,
        Cls = 2,
        Last = 3,
        Rank = 4
    }

    public enum LlamaAttentionType
    {
        Unspecified = -1,
        Causal = 0,
        NonCausal = 1
    }


    // Native struct matching llama_context_params_t
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeLlamaContextParams
    {
        public OptionalUInt32 n_ctx;
        public OptionalUInt32 n_batch;
        public OptionalUInt32 n_ubatch;
        public OptionalUInt32 n_seq_max;
        public OptionalInt32 n_threads;
        public OptionalInt32 n_threads_batch;
        public OptionalInt32 rope_scaling_type; // llama_rope_scaling_type enum as int
        public OptionalInt32 pooling_type; // llama_pooling_type enum as int
        public OptionalInt32 attention_type; // llama_attention_type enum as int
        public OptionalFloat rope_freq_base;
        public OptionalFloat rope_freq_scale;
        public OptionalFloat yarn_ext_factor;
        public OptionalFloat yarn_attn_factor;
        public OptionalFloat yarn_beta_fast;
        public OptionalFloat yarn_beta_slow;
        public OptionalUInt32 yarn_orig_ctx;
        public OptionalFloat defrag_thold;
        public OptionalBool embeddings;
        public OptionalBool offload_kqv;
        public OptionalBool flash_attn;
        public OptionalBool no_perf;
        public OptionalBool op_offload;
        public OptionalBool swa_full;
        [MarshalAs(UnmanagedType.I1)] public bool enable_abort_generation;
    }
}