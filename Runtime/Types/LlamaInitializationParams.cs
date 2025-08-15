using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    // Native struct matching llama_initialization_params_t
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NativeLlamaInitializationParams
    {
        [MarshalAs(UnmanagedType.LPStr)] public string model_path;
        public NativeLlamaModelParams model_params;
        public NativeLlamaContextParams context_params;
    }

    // Serializable Unity-friendly version
    [Serializable]
    public class LlamaInitializationParams
    {
        [SerializeField] public string modelPath = "";

        [SerializeField] public Optional<int> gpuLayers = new Optional<int>();
        [SerializeField] public Optional<int> splitMode = new Optional<int>();
        [SerializeField] public Optional<int> mainGpu = new Optional<int>();
        [SerializeField] public Optional<bool> vocabOnly = new Optional<bool>();
        [SerializeField] public Optional<bool> useMmap = new Optional<bool>();
        [SerializeField] public Optional<bool> useMlock = new Optional<bool>();
        [SerializeField] public Optional<bool> checkTensors = new Optional<bool>();
        [SerializeField] public bool enableAbortInit = false;

        [SerializeField] public Optional<uint> contextLength = new Optional<uint>();
        [SerializeField] public Optional<uint> batchSize = new Optional<uint>();
        [SerializeField] public Optional<uint> ubatchSize = new Optional<uint>();
        [SerializeField] public Optional<uint> maxSequences = new Optional<uint>();
        [SerializeField] public Optional<int> threads = new Optional<int>();
        [SerializeField] public Optional<int> threadsBatch = new Optional<int>();
        [SerializeField] public Optional<int> ropeScalingType = new Optional<int>();
        [SerializeField] public Optional<int> poolingType = new Optional<int>();
        [SerializeField] public Optional<int> attentionType = new Optional<int>();
        [SerializeField] public Optional<float> ropeFreqBase = new Optional<float>();
        [SerializeField] public Optional<float> ropeFreqScale = new Optional<float>();
        [SerializeField] public Optional<float> yarnExtFactor = new Optional<float>();
        [SerializeField] public Optional<float> yarnAttnFactor = new Optional<float>();
        [SerializeField] public Optional<float> yarnBetaFast = new Optional<float>();
        [SerializeField] public Optional<float> yarnBetaSlow = new Optional<float>();
        [SerializeField] public Optional<uint> yarnOrigCtx = new Optional<uint>();
        [SerializeField] public Optional<float> defragThreshold = new Optional<float>();
        [SerializeField] public Optional<bool> embeddings = new Optional<bool>();
        [SerializeField] public Optional<bool> offloadKqv = new Optional<bool>();
        [SerializeField] public Optional<bool> flashAttn = new Optional<bool>();
        [SerializeField] public Optional<bool> noPerf = new Optional<bool>();
        [SerializeField] public Optional<bool> opOffload = new Optional<bool>();
        [SerializeField] public Optional<bool> swaFull = new Optional<bool>();
        [SerializeField] public bool enableAbortGeneration = false;

        // Default constructor
        public LlamaInitializationParams()
        {
        }

        // Copy constructor
        public LlamaInitializationParams(LlamaInitializationParams other)
        {
            if (other == null) return;

            // Copy all properties
            modelPath = other.modelPath;
            gpuLayers = other.gpuLayers;
            splitMode = other.splitMode;
            mainGpu = other.mainGpu;
            vocabOnly = other.vocabOnly;
            useMmap = other.useMmap;
            useMlock = other.useMlock;
            checkTensors = other.checkTensors;
            enableAbortInit = other.enableAbortInit;

            contextLength = other.contextLength;
            batchSize = other.batchSize;
            ubatchSize = other.ubatchSize;
            maxSequences = other.maxSequences;
            threads = other.threads;
            threadsBatch = other.threadsBatch;
            ropeScalingType = other.ropeScalingType;
            poolingType = other.poolingType;
            attentionType = other.attentionType;
            ropeFreqBase = other.ropeFreqBase;
            ropeFreqScale = other.ropeFreqScale;
            yarnExtFactor = other.yarnExtFactor;
            yarnAttnFactor = other.yarnAttnFactor;
            yarnBetaFast = other.yarnBetaFast;
            yarnBetaSlow = other.yarnBetaSlow;
            yarnOrigCtx = other.yarnOrigCtx;
            defragThreshold = other.defragThreshold;
            embeddings = other.embeddings;
            offloadKqv = other.offloadKqv;
            flashAttn = other.flashAttn;
            noPerf = other.noPerf;
            opOffload = other.opOffload;
            swaFull = other.swaFull;
            enableAbortGeneration = other.enableAbortGeneration;
        }

        public NativeLlamaModelParams ToModelParamsStruct()
        {
            return new NativeLlamaModelParams()
            {
                n_gpu_layers = gpuLayers.HasValue ? OptionalInt32.Some(gpuLayers.value) : OptionalInt32.None,
                split_mode = splitMode.HasValue ? OptionalInt32.Some(splitMode.value) : OptionalInt32.None,
                main_gpu = mainGpu.HasValue ? OptionalInt32.Some(mainGpu.value) : OptionalInt32.None,
                vocab_only = vocabOnly.HasValue ? OptionalBool.Some(vocabOnly.value) : OptionalBool.None,
                use_mmap = useMmap.HasValue ? OptionalBool.Some(useMmap.value) : OptionalBool.None,
                use_mlock = useMlock.HasValue ? OptionalBool.Some(useMlock.value) : OptionalBool.None,
                check_tensors = checkTensors.HasValue ? OptionalBool.Some(checkTensors.value) : OptionalBool.None,
                enable_abort_init = enableAbortInit
            };
        }

        public NativeLlamaContextParams ToContextParamsStruct()
        {
            return new NativeLlamaContextParams()
            {
                n_ctx = contextLength.HasValue ? OptionalUInt32.Some(contextLength.value) : OptionalUInt32.None,
                n_batch = batchSize.HasValue ? OptionalUInt32.Some(batchSize.value) : OptionalUInt32.None,
                n_ubatch = ubatchSize.HasValue ? OptionalUInt32.Some(ubatchSize.value) : OptionalUInt32.None,
                n_seq_max = maxSequences.HasValue ? OptionalUInt32.Some(maxSequences.value) : OptionalUInt32.None,
                n_threads = threads.HasValue ? OptionalInt32.Some(threads.value) : OptionalInt32.None,
                n_threads_batch = threadsBatch.HasValue ? OptionalInt32.Some(threadsBatch.value) : OptionalInt32.None,
                rope_scaling_type = ropeScalingType.HasValue ? OptionalInt32.Some(ropeScalingType.value) : OptionalInt32.None,
                pooling_type = poolingType.HasValue ? OptionalInt32.Some(poolingType.value) : OptionalInt32.None,
                attention_type = attentionType.HasValue ? OptionalInt32.Some(attentionType.value) : OptionalInt32.None,
                rope_freq_base = ropeFreqBase.HasValue ? OptionalFloat.Some(ropeFreqBase.value) : OptionalFloat.None,
                rope_freq_scale = ropeFreqScale.HasValue ? OptionalFloat.Some(ropeFreqScale.value) : OptionalFloat.None,
                yarn_ext_factor = yarnExtFactor.HasValue ? OptionalFloat.Some(yarnExtFactor.value) : OptionalFloat.None,
                yarn_attn_factor = yarnAttnFactor.HasValue ? OptionalFloat.Some(yarnAttnFactor.value) : OptionalFloat.None,
                yarn_beta_fast = yarnBetaFast.HasValue ? OptionalFloat.Some(yarnBetaFast.value) : OptionalFloat.None,
                yarn_beta_slow = yarnBetaSlow.HasValue ? OptionalFloat.Some(yarnBetaSlow.value) : OptionalFloat.None,
                yarn_orig_ctx = yarnOrigCtx.HasValue ? OptionalUInt32.Some(yarnOrigCtx.value) : OptionalUInt32.None,
                defrag_thold = defragThreshold.HasValue ? OptionalFloat.Some(defragThreshold.value) : OptionalFloat.None,
                embeddings = embeddings.HasValue ? OptionalBool.Some(embeddings.value) : OptionalBool.None,
                offload_kqv = offloadKqv.HasValue ? OptionalBool.Some(offloadKqv.value) : OptionalBool.None,
                flash_attn = flashAttn.HasValue ? OptionalBool.Some(flashAttn.value) : OptionalBool.None,
                no_perf = noPerf.HasValue ? OptionalBool.Some(noPerf.value) : OptionalBool.None,
                op_offload = opOffload.HasValue ? OptionalBool.Some(opOffload.value) : OptionalBool.None,
                swa_full = swaFull.HasValue ? OptionalBool.Some(swaFull.value) : OptionalBool.None,
                enable_abort_generation = enableAbortGeneration
            };
        }

        public NativeLlamaInitializationParams ToStruct()
        {
            if (string.IsNullOrEmpty(modelPath))
            {
                throw new InvalidOperationException("Model path must be set before calling ToStruct(). Please call WithModelPath() to set it.");
            }

            return new NativeLlamaInitializationParams()
            {
                model_path = modelPath,
                model_params = ToModelParamsStruct(),
                context_params = ToContextParamsStruct()
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static LlamaInitializationParams FromJson(string json)
        {
            return JsonUtility.FromJson<LlamaInitializationParams>(json);
        }

        // Fluent builder methods
        public LlamaInitializationParams WithModelPath(string path)
        {
            modelPath = path;
            return this;
        }

        // Model Parameters
        public LlamaInitializationParams WithGpuLayers(int layers)
        {
            gpuLayers = new Optional<int>(layers);
            return this;
        }

        public LlamaInitializationParams WithSplitMode(int mode)
        {
            splitMode = new Optional<int>(mode);
            return this;
        }

        public LlamaInitializationParams WithSplitMode(LlamaSplitMode mode)
        {
            splitMode = new Optional<int>((int)mode);
            return this;
        }

        public LlamaInitializationParams WithMainGpu(int gpu)
        {
            mainGpu = new Optional<int>(gpu);
            return this;
        }

        public LlamaInitializationParams WithVocabOnly(bool vocabOnly = true)
        {
            this.vocabOnly = new Optional<bool>(vocabOnly);
            return this;
        }

        public LlamaInitializationParams WithMmap(bool useMmap = true)
        {
            this.useMmap = new Optional<bool>(useMmap);
            return this;
        }

        public LlamaInitializationParams WithMlock(bool useMlock = true)
        {
            this.useMlock = new Optional<bool>(useMlock);
            return this;
        }

        public LlamaInitializationParams WithCheckTensors(bool checkTensors = true)
        {
            this.checkTensors = new Optional<bool>(checkTensors);
            return this;
        }

        public LlamaInitializationParams WithAbortInit(bool enableAbortInit = true)
        {
            this.enableAbortInit = enableAbortInit;
            return this;
        }

        // Context Parameters
        public LlamaInitializationParams WithContextLength(uint length)
        {
            contextLength = new Optional<uint>(length);
            return this;
        }

        public LlamaInitializationParams WithBatchSize(uint size)
        {
            batchSize = new Optional<uint>(size);
            return this;
        }

        public LlamaInitializationParams WithUBatchSize(uint size)
        {
            ubatchSize = new Optional<uint>(size);
            return this;
        }

        public LlamaInitializationParams WithMaxSequences(uint maxSeq)
        {
            maxSequences = new Optional<uint>(maxSeq);
            return this;
        }

        public LlamaInitializationParams WithThreads(int threadCount)
        {
            threads = new Optional<int>(threadCount);
            return this;
        }

        public LlamaInitializationParams WithBatchThreads(int threadCount)
        {
            threadsBatch = new Optional<int>(threadCount);
            return this;
        }

        public LlamaInitializationParams WithRopeScaling(int scalingType)
        {
            ropeScalingType = new Optional<int>(scalingType);
            return this;
        }

        public LlamaInitializationParams WithRopeScaling(LlamaRopeScalingType scalingType)
        {
            ropeScalingType = new Optional<int>((int)scalingType);
            return this;
        }

        public LlamaInitializationParams WithPoolingType(int pooling)
        {
            poolingType = new Optional<int>(pooling);
            return this;
        }

        public LlamaInitializationParams WithPoolingType(LlamaPoolingType pooling)
        {
            poolingType = new Optional<int>((int)pooling);
            return this;
        }

        public LlamaInitializationParams WithAttentionType(int attention)
        {
            attentionType = new Optional<int>(attention);
            return this;
        }

        public LlamaInitializationParams WithAttentionType(LlamaAttentionType attention)
        {
            attentionType = new Optional<int>((int)attention);
            return this;
        }

        public LlamaInitializationParams WithRopeFreqBase(float freqBase)
        {
            ropeFreqBase = new Optional<float>(freqBase);
            return this;
        }

        public LlamaInitializationParams WithRopeFreqScale(float freqScale)
        {
            ropeFreqScale = new Optional<float>(freqScale);
            return this;
        }

        public LlamaInitializationParams WithYarnExtFactor(float factor)
        {
            yarnExtFactor = new Optional<float>(factor);
            return this;
        }

        public LlamaInitializationParams WithYarnAttnFactor(float factor)
        {
            yarnAttnFactor = new Optional<float>(factor);
            return this;
        }

        public LlamaInitializationParams WithYarnBetaFast(float beta)
        {
            yarnBetaFast = new Optional<float>(beta);
            return this;
        }

        public LlamaInitializationParams WithYarnBetaSlow(float beta)
        {
            yarnBetaSlow = new Optional<float>(beta);
            return this;
        }

        public LlamaInitializationParams WithYarnOrigCtx(uint ctx)
        {
            yarnOrigCtx = new Optional<uint>(ctx);
            return this;
        }

        public LlamaInitializationParams WithDefragThreshold(float threshold)
        {
            defragThreshold = new Optional<float>(threshold);
            return this;
        }

        public LlamaInitializationParams WithEmbeddings(bool enableEmbeddings = true)
        {
            embeddings = new Optional<bool>(enableEmbeddings);
            return this;
        }

        public LlamaInitializationParams WithOffloadKqv(bool offload = true)
        {
            offloadKqv = new Optional<bool>(offload);
            return this;
        }

        public LlamaInitializationParams WithFlashAttention(bool enableFlash = true)
        {
            flashAttn = new Optional<bool>(enableFlash);
            return this;
        }

        public LlamaInitializationParams WithPerformanceMetrics(bool enablePerf = true)
        {
            noPerf = new Optional<bool>(!enablePerf);
            return this;
        }

        public LlamaInitializationParams WithOpOffload(bool offload = true)
        {
            opOffload = new Optional<bool>(offload);
            return this;
        }

        public LlamaInitializationParams WithSwaFull(bool enableSwa = true)
        {
            swaFull = new Optional<bool>(enableSwa);
            return this;
        }

        public LlamaInitializationParams WithAbortGeneration(bool enableAbort = true)
        {
            enableAbortGeneration = enableAbort;
            return this;
        }
    }
}