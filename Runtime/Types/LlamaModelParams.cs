using System.Runtime.InteropServices;

namespace Aviad
{
    public enum LlamaSplitMode
    {
        None = 0,
        Layer = 1,
        Row = 2
    }

    // Native struct matching llama_model_params_t
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeLlamaModelParams
    {
        public OptionalInt32 n_gpu_layers;
        public OptionalInt32 split_mode; // llama_split_mode enum as int
        public OptionalInt32 main_gpu;
        public OptionalBool vocab_only;
        public OptionalBool use_mmap;
        public OptionalBool use_mlock;
        public OptionalBool check_tensors;
        [MarshalAs(UnmanagedType.I1)] public bool enable_abort_init;
    }

}