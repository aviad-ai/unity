import createModule from './aviad_main.js';

////////////////////////////////////
// common                         //
////////////////////////////////////

function isNumber(x) {
    return typeof x === 'number' && Number.isFinite(x);
}
function isInt32(x) {
    return isNumber(x) && x >= -2147483648 && x <= 2147483647 && Number.isInteger(x);
}
function isUInt32(x) {
    return isNumber(x) && x >= 0 && x <= 0xFFFFFFFF && Number.isInteger(x);
}
function isFloat(x) {
    return isNumber(x);
}
function isBoolish(x) {
    return typeof x === 'boolean' || x === 0 || x === 1;
}
function toBool(x) {
    return !!x;
}
function asInt32(x) {
    return (x | 0);
}
function asUInt32(x) {
    return (x >>> 0);
}
function asFloat(x) {
    return Number(x);
}
function validateString(val, fieldName) {
    if (val === null || val === undefined) return '';
    if (typeof val !== 'string') {
        throw new Error(`${fieldName} must be a string`);
    }
    return val;
}

function makeAllocHelpers(Module) {
    const allocations = [];

    function malloc(size) {
        const ptr = Module._malloc(size);
        if (!ptr) throw new Error(`_malloc failed for size ${size}`);
        allocations.push({ ptr, size, type: 'block' });
        return ptr;
    }

    function setI32(addr, val) {
        Module.setValue(addr, val | 0, 'i32');
    }

    function setU32(addr, val) {
        Module.setValue(addr, val >>> 0, 'i32');
    }

    function setF32(addr, val) {
        Module.setValue(addr, +val, 'float');
    }

    function setI8(addr, val) {
        Module.setValue(addr, (val ? 1 : 0) | 0, 'i8');
    }

    function allocCString(str) {
        // ANSI/ASCII expected by native; non-ASCII chars will be truncated to 1 byte.
        if (str === null || str === undefined) return 0;
        const s = String(str);
        const len = s.length;
        const ptr = malloc(len + 1);
        for (let i = 0; i < len; i++) {
            const code = s.charCodeAt(i) & 0xFF;
            Module.setValue(ptr + i, code, 'i8');
        }
        Module.setValue(ptr + len, 0, 'i8'); // NUL terminator
        allocations.push({ ptr, type: 'cstring' });
        return ptr;
    }

    function allocCStringArray(arr) {
        const n = Array.isArray(arr) ? arr.length : 0;
        if (n === 0) {
            return { base: 0, elements: [] };
        }
        const ptrs = new Array(n);
        for (let i = 0; i < n; i++) {
            ptrs[i] = allocCString(arr[i]);
        }
        const base = malloc(n * 4);
        for (let i = 0; i < n; i++) {
            setU32(base + i * 4, ptrs[i] >>> 0);
        }
        allocations.push({ ptr: base, type: 'ptr_array', count: n });
        return { base, elements: ptrs };
    }

    function freeAll() {
        // NOTE: In Emscripten, free() is not always exported by default; users should free via Module._free if available.
        if (typeof Module._free === 'function') {
            // Free pointer arrays/blocks in reverse order
            for (let i = allocations.length - 1; i >= 0; i--) {
                try {
                    Module._free(allocations[i].ptr);
                } catch (_) {
                    // ignore
                }
            }
        }
    }

    return { malloc, setI32, setU32, setF32, setI8, allocCString, allocCStringArray, freeAll, allocations };
}

////////////////////////////////////
// llama_generation_params        //
////////////////////////////////////

/*
  Utilities to validate JSON produced by LlamaGenerationConfig.cs and to allocate
  a WebAssembly-compatible llama_generation_params_t structure using an Emscripten
  Module providing _malloc and setValue.

  This assumes 32-bit pointers (Emscripten), C default alignment, and the following C layout:

  typedef struct {
      uint32_t dist_seed;
      int32_t  top_k;
      float    top_p;
      uint32_t top_p_min_keep;
      float    min_p;
      uint32_t min_p_min_keep;
      float    typical_p;
      uint32_t typical_min_keep;
      float    temperature;
      float    temp_ext_t;
      float    temp_ext_delta;
      float    temp_ext_exponent;
      float    xtc_p;
      float    xtc_t;
      uint32_t xtc_min_keep;
      uint32_t xtc_seed;
      float    top_n_sigma;
      int32_t  mirostat_n_vocab;
      uint32_t mirostat_seed;
      float    mirostat_tau;
      float    mirostat_eta;
      int32_t  mirostat_m;
      uint32_t mirostat_v2_seed;
      float    mirostat_v2_tau;
      float    mirostat_v2_eta;
      const char*  grammar_str;
      const char*  grammar_root;
      const char** grammar_trigger_patterns;
      uint32_t     grammar_num_trigger_patterns;
      int32_t  penalty_last_n;
      float    penalty_repeat;
      float    penalty_freq;
      float    penalty_present;
      int32_t  dry_n_ctx_train;
      float    dry_multiplier;
      float    dry_base;
      int32_t  dry_allowed_length;
      int32_t  dry_penalty_last_n;
      const char** dry_seq_breakers;
      uint32_t     dry_num_breakers;
  } llama_sampler_parameters_t; // size: 160

  typedef struct {
      int*     types; // sampler_type_t* (i32)
      uint32_t count;
  } llama_sampler_chain_t; // size: 8

  typedef struct {
      const char* chat_template;
      bool        use_models_chat_template; // i8, 3 bytes padding
  } chat_template_params_t; // size: 8

  typedef struct {
      llama_sampler_parameters_t sampler_params; // offset 0
      llama_sampler_chain_t      sampler_chain;  // offset 160
      chat_template_params_t     template_params;// offset 168
      uint32_t                   max_tokens;     // offset 176
      uint32_t                   chunk_size;     // offset 180
  } llama_generation_params_t; // size: 184
*/

const GENERAITON_OFFSETS = Object.freeze({
    // llama_sampler_parameters_t
    sp: {
        dist_seed: 0,
        top_k: 4,
        top_p: 8,
        top_p_min_keep: 12,
        min_p: 16,
        min_p_min_keep: 20,
        typical_p: 24,
        typical_min_keep: 28,
        temperature: 32,
        temp_ext_t: 36,
        temp_ext_delta: 40,
        temp_ext_exponent: 44,
        xtc_p: 48,
        xtc_t: 52,
        xtc_min_keep: 56,
        xtc_seed: 60,
        top_n_sigma: 64,
        mirostat_n_vocab: 68,
        mirostat_seed: 72,
        mirostat_tau: 76,
        mirostat_eta: 80,
        mirostat_m: 84,
        mirostat_v2_seed: 88,
        mirostat_v2_tau: 92,
        mirostat_v2_eta: 96,
        grammar_str: 100,
        grammar_root: 104,
        grammar_trigger_patterns: 108,
        grammar_num_trigger_patterns: 112,
        penalty_last_n: 116,
        penalty_repeat: 120,
        penalty_freq: 124,
        penalty_present: 128,
        dry_n_ctx_train: 132,
        dry_multiplier: 136,
        dry_base: 140,
        dry_allowed_length: 144,
        dry_penalty_last_n: 148,
        dry_seq_breakers: 152,
        dry_num_breakers: 156,
        SIZE: 160
    },
    // llama_sampler_chain_t
    sc: {
        types: 0,
        count: 4,
        SIZE: 8
    },
    // chat_template_params_t
    ct: {
        chat_template: 0,
        use_models_chat_template: 4, // i8 + 3 bytes padding to 8
        SIZE: 8
    },
    // llama_generation_params_t
    gp: {
        sampler_params: 0,
        sampler_chain: 160,
        template_params: 168,
        max_tokens: 176,
        chunk_size: 180,
        SIZE: 184
    }
});

const SAMPLER_TYPE_MIN = 0;
const SAMPLER_TYPE_MAX = 17;


function ensureArray(val) {
    return Array.isArray(val) ? val : [];
}

function validateSamplerChain(arr) {
    const chain = ensureArray(arr).map((v) => {
        if (!isNumber(v)) throw new Error('samplerChain must contain numbers');
        const iv = v | 0;
        if (iv < SAMPLER_TYPE_MIN || iv > SAMPLER_TYPE_MAX) {
            throw new Error(`samplerChain contains invalid sampler type ${iv}`);
        }
        return iv;
    });
    return chain;
}
function validateStringArray(arr, fieldName) {
    const a = ensureArray(arr);
    for (let i = 0; i < a.length; i++) {
        if (typeof a[i] !== 'string') {
            throw new Error(`${fieldName}[${i}] must be a string`);
        }
    }
    return a;
}

function validateChatTemplateParams(obj) {
    if (obj === null || obj === undefined) return { chatTemplate: '', useModelsChatTemplate: false };
    if (typeof obj !== 'object') throw new Error('chatTemplateParams must be an object');
    const chatTemplate = validateString(obj.chatTemplate, 'chatTemplateParams.chatTemplate');
    const useModelsChatTemplate = !!obj.useModelsChatTemplate;
    return { chatTemplate, useModelsChatTemplate };
}

// Validate and normalize JSON produced by LlamaGenerationConfig.cs
function validateLlamaGenerationConfigJson(json) {
    const cfg = typeof json === 'string' ? JSON.parse(json) : json;
    if (typeof cfg !== 'object' || !cfg) {
        throw new Error('Input must be a JSON object or JSON string');
    }

    const out = {};

    // Top-level fields
    out.maxTokens = isUInt32(cfg.maxTokens) ? asUInt32(cfg.maxTokens) : 256 >>> 0;
    out.chunkSize = isUInt32(cfg.chunkSize) ? asUInt32(cfg.chunkSize) : 1 >>> 0;

    out.samplerChain = validateSamplerChain(cfg.samplerChain);

    // Sampler parameters
    out.distSeed = isUInt32(cfg.distSeed) ? asUInt32(cfg.distSeed) : 0;
    out.topK = isInt32(cfg.topK) ? asInt32(cfg.topK) : 40 | 0;
    out.topP = isFloat(cfg.topP) ? asFloat(cfg.topP) : 0.9;
    out.topPMinKeep = isUInt32(cfg.topPMinKeep) ? asUInt32(cfg.topPMinKeep) : 1 >>> 0;

    out.minP = isFloat(cfg.minP) ? asFloat(cfg.minP) : 0.05;
    out.minPMinKeep = isUInt32(cfg.minPMinKeep) ? asUInt32(cfg.minPMinKeep) : 1 >>> 0;

    out.typicalP = isFloat(cfg.typicalP) ? asFloat(cfg.typicalP) : 1.0;
    out.typicalMinKeep = isUInt32(cfg.typicalMinKeep) ? asUInt32(cfg.typicalMinKeep) : 1 >>> 0;

    out.temperature = isFloat(cfg.temperature) ? asFloat(cfg.temperature) : 0.7;

    out.tempExtT = isFloat(cfg.tempExtT) ? asFloat(cfg.tempExtT) : 1.0;
    out.tempExtDelta = isFloat(cfg.tempExtDelta) ? asFloat(cfg.tempExtDelta) : 0.0;
    out.tempExtExponent = isFloat(cfg.tempExtExponent) ? asFloat(cfg.tempExtExponent) : 1.0;

    out.xtcP = isFloat(cfg.xtcP) ? asFloat(cfg.xtcP) : 0.0;
    out.xtcT = isFloat(cfg.xtcT) ? asFloat(cfg.xtcT) : 0.0;
    out.xtcMinKeep = isUInt32(cfg.xtcMinKeep) ? asUInt32(cfg.xtcMinKeep) : 1 >>> 0;
    out.xtcSeed = isUInt32(cfg.xtcSeed) ? asUInt32(cfg.xtcSeed) : 0 >>> 0;

    out.topNSigma = isFloat(cfg.topNSigma) ? asFloat(cfg.topNSigma) : 1.0;

    out.mirostatNVocab = isInt32(cfg.mirostatNVocab) ? asInt32(cfg.mirostatNVocab) : 0 | 0;
    out.mirostatSeed = isUInt32(cfg.mirostatSeed) ? asUInt32(cfg.mirostatSeed) : 0 >>> 0;
    out.mirostatTau = isFloat(cfg.mirostatTau) ? asFloat(cfg.mirostatTau) : 5.0;
    out.mirostatEta = isFloat(cfg.mirostatEta) ? asFloat(cfg.mirostatEta) : 0.1;
    out.mirostatM = isInt32(cfg.mirostatM) ? asInt32(cfg.mirostatM) : 100 | 0;

    out.mirostatV2Seed = isUInt32(cfg.mirostatV2Seed) ? asUInt32(cfg.mirostatV2Seed) : 0 >>> 0;
    out.mirostatV2Tau = isFloat(cfg.mirostatV2Tau) ? asFloat(cfg.mirostatV2Tau) : 5.0;
    out.mirostatV2Eta = isFloat(cfg.mirostatV2Eta) ? asFloat(cfg.mirostatV2Eta) : 0.1;

    out.grammarStr = validateString(cfg.grammarStr, 'grammarStr');
    out.grammarRoot = validateString(cfg.grammarRoot, 'grammarRoot');
    out.grammarTriggerPatterns = validateStringArray(cfg.grammarTriggerPatterns, 'grammarTriggerPatterns');

    out.penaltyLastN = isInt32(cfg.penaltyLastN) ? asInt32(cfg.penaltyLastN) : 64 | 0;
    out.penaltyRepeat = isFloat(cfg.penaltyRepeat) ? asFloat(cfg.penaltyRepeat) : 1.0;
    out.penaltyFreq = isFloat(cfg.penaltyFreq) ? asFloat(cfg.penaltyFreq) : 0.0;
    out.penaltyPresent = isFloat(cfg.penaltyPresent) ? asFloat(cfg.penaltyPresent) : 0.0;

    out.dryNCtxTrain = isInt32(cfg.dryNCtxTrain) ? asInt32(cfg.dryNCtxTrain) : 1024 | 0;
    out.dryMultiplier = isFloat(cfg.dryMultiplier) ? asFloat(cfg.dryMultiplier) : 0.0;
    out.dryBase = isFloat(cfg.dryBase) ? asFloat(cfg.dryBase) : 1.75;
    out.dryAllowedLength = isInt32(cfg.dryAllowedLength) ? asInt32(cfg.dryAllowedLength) : 2 | 0;
    out.dryPenaltyLastN = isInt32(cfg.dryPenaltyLastN) ? asInt32(cfg.dryPenaltyLastN) : -1 | 0;
    out.drySeqBreakers = validateStringArray(cfg.drySeqBreakers, 'drySeqBreakers');

    // Chat template params
    out.chatTemplateParams = validateChatTemplateParams(cfg.chatTemplateParams);

    return out;
}

// Allocate and populate a llama_generation_params_t and all nested buffers.
// Returns { ptr, free: () => void, allocations, normalized }
function allocLlamaGenerationParams(Module, json) {
    if (!Module || typeof Module.setValue !== 'function' || typeof Module._malloc !== 'function') {
        throw new Error('Module with setValue and _malloc is required');
    }

    const cfg = validateLlamaGenerationConfigJson(json);
    const { malloc, setI32, setU32, setF32, setI8, allocCString, allocCStringArray, freeAll, allocations } = makeAllocHelpers(Module);

    // Pre-allocate strings/arrays needed by the struct
    const samplerTypesCount = cfg.samplerChain.length >>> 0;
    const samplerTypesPtr = samplerTypesCount > 0 ? malloc(samplerTypesCount * 4) : 0;
    for (let i = 0; i < samplerTypesCount; i++) {
        setI32(samplerTypesPtr + i * 4, cfg.samplerChain[i] | 0);
    }
    if (samplerTypesPtr) allocations.push({ ptr: samplerTypesPtr, type: 'i32_array', count: samplerTypesCount });

    const grammarStrPtr = allocCString(cfg.grammarStr); // allocate even if empty to mimic .NET marshalling
    const grammarRootPtr = allocCString(cfg.grammarRoot); // same

    const { base: grammarPatternsPtr } = allocCStringArray(cfg.grammarTriggerPatterns);

    const { base: drySeqBreakersPtr } = allocCStringArray(cfg.drySeqBreakers);

    const chatTemplatePtr = allocCString(cfg.chatTemplateParams.chatTemplate);
    const chatTemplateUseModel = !!cfg.chatTemplateParams.useModelsChatTemplate;

    // Allocate the top-level llama_generation_params_t
    const base = malloc(GENERAITON_OFFSETS.gp.SIZE);

    // Zero any implicit paddings
    for (let i = 0; i < GENERAITON_OFFSETS.gp.SIZE; i++) {
        Module.setValue(base + i, 0, 'i8');
    }

    // Fill sampler_params (offset 0)
    const sp = base + GENERAITON_OFFSETS.gp.sampler_params;
    setU32(sp + GENERAITON_OFFSETS.sp.dist_seed, cfg.distSeed);
    setI32(sp + GENERAITON_OFFSETS.sp.top_k, cfg.topK);
    setF32(sp + GENERAITON_OFFSETS.sp.top_p, cfg.topP);
    setU32(sp + GENERAITON_OFFSETS.sp.top_p_min_keep, cfg.topPMinKeep);

    setF32(sp + GENERAITON_OFFSETS.sp.min_p, cfg.minP);
    setU32(sp + GENERAITON_OFFSETS.sp.min_p_min_keep, cfg.minPMinKeep);

    setF32(sp + GENERAITON_OFFSETS.sp.typical_p, cfg.typicalP);
    setU32(sp + GENERAITON_OFFSETS.sp.typical_min_keep, cfg.typicalMinKeep);

    setF32(sp + GENERAITON_OFFSETS.sp.temperature, cfg.temperature);

    setF32(sp + GENERAITON_OFFSETS.sp.temp_ext_t, cfg.tempExtT);
    setF32(sp + GENERAITON_OFFSETS.sp.temp_ext_delta, cfg.tempExtDelta);
    setF32(sp + GENERAITON_OFFSETS.sp.temp_ext_exponent, cfg.tempExtExponent);

    setF32(sp + GENERAITON_OFFSETS.sp.xtc_p, cfg.xtcP);
    setF32(sp + GENERAITON_OFFSETS.sp.xtc_t, cfg.xtcT);
    setU32(sp + GENERAITON_OFFSETS.sp.xtc_min_keep, cfg.xtcMinKeep);
    setU32(sp + GENERAITON_OFFSETS.sp.xtc_seed, cfg.xtcSeed);

    setF32(sp + GENERAITON_OFFSETS.sp.top_n_sigma, cfg.topNSigma);

    setI32(sp + GENERAITON_OFFSETS.sp.mirostat_n_vocab, cfg.mirostatNVocab);
    setU32(sp + GENERAITON_OFFSETS.sp.mirostat_seed, cfg.mirostatSeed);
    setF32(sp + GENERAITON_OFFSETS.sp.mirostat_tau, cfg.mirostatTau);
    setF32(sp + GENERAITON_OFFSETS.sp.mirostat_eta, cfg.mirostatEta);
    setI32(sp + GENERAITON_OFFSETS.sp.mirostat_m, cfg.mirostatM);

    setU32(sp + GENERAITON_OFFSETS.sp.mirostat_v2_seed, cfg.mirostatV2Seed);
    setF32(sp + GENERAITON_OFFSETS.sp.mirostat_v2_tau, cfg.mirostatV2Tau);
    setF32(sp + GENERAITON_OFFSETS.sp.mirostat_v2_eta, cfg.mirostatV2Eta);

    // Pointers and counts
    setU32(sp + GENERAITON_OFFSETS.sp.grammar_str, chatTemplatePtr ? (grammarStrPtr >>> 0) : (grammarStrPtr >>> 0)); // always set ptr
    setU32(sp + GENERAITON_OFFSETS.sp.grammar_root, grammarRootPtr >>> 0);
    setU32(sp + GENERAITON_OFFSETS.sp.grammar_trigger_patterns, grammarPatternsPtr >>> 0);
    setU32(sp + GENERAITON_OFFSETS.sp.grammar_num_trigger_patterns, (cfg.grammarTriggerPatterns.length >>> 0));

    setI32(sp + GENERAITON_OFFSETS.sp.penalty_last_n, cfg.penaltyLastN);
    setF32(sp + GENERAITON_OFFSETS.sp.penalty_repeat, cfg.penaltyRepeat);
    setF32(sp + GENERAITON_OFFSETS.sp.penalty_freq, cfg.penaltyFreq);
    setF32(sp + GENERAITON_OFFSETS.sp.penalty_present, cfg.penaltyPresent);

    setI32(sp + GENERAITON_OFFSETS.sp.dry_n_ctx_train, cfg.dryNCtxTrain);
    setF32(sp + GENERAITON_OFFSETS.sp.dry_multiplier, cfg.dryMultiplier);
    setF32(sp + GENERAITON_OFFSETS.sp.dry_base, cfg.dryBase);
    setI32(sp + GENERAITON_OFFSETS.sp.dry_allowed_length, cfg.dryAllowedLength);
    setI32(sp + GENERAITON_OFFSETS.sp.dry_penalty_last_n, cfg.dryPenaltyLastN);
    setU32(sp + GENERAITON_OFFSETS.sp.dry_seq_breakers, drySeqBreakersPtr >>> 0);
    setU32(sp + GENERAITON_OFFSETS.sp.dry_num_breakers, (cfg.drySeqBreakers.length >>> 0));

    // Fill sampler_chain (offset 160)
    const sc = base + GENERAITON_OFFSETS.gp.sampler_chain;
    setU32(sc + GENERAITON_OFFSETS.sc.types, samplerTypesPtr >>> 0);
    setU32(sc + GENERAITON_OFFSETS.sc.count, samplerTypesCount >>> 0);

    // Fill chat_template_params (offset 168)
    const ct = base + GENERAITON_OFFSETS.gp.template_params;
    setU32(ct + GENERAITON_OFFSETS.ct.chat_template, chatTemplatePtr >>> 0);
    setI8(ct + GENERAITON_OFFSETS.ct.use_models_chat_template, chatTemplateUseModel ? 1 : 0);
    // padding already zeroed

    // Fill remaining fields
    setU32(base + GENERAITON_OFFSETS.gp.max_tokens, cfg.maxTokens >>> 0);
    setU32(base + GENERAITON_OFFSETS.gp.chunk_size, cfg.chunkSize >>> 0);

    return {
        ptr: base >>> 0,
        free: freeAll,
        allocations,
        normalized: cfg
    };
}

////////////////////////////////////
// llama_initialization_params    //
////////////////////////////////////

/*
  Utilities to validate JSON produced by LlamaInitializationParams.cs and to allocate
  a WebAssembly-compatible llama_initialization_params_t structure using an Emscripten
  Module providing _malloc and setValue.

  This assumes 32-bit pointers (Emscripten), C default alignment, and the following C layout:

  typedef struct {
      // Represented as 8-byte structs: { int32_t has_value; <T> value; }
      // For optional_bool_t, value is int32_t (0 or 1).
      optional_int32_t n_gpu_layers;   // offset 0
      optional_int32_t split_mode;     // offset 8
      optional_int32_t main_gpu;       // offset 16
      optional_bool_t  vocab_only;     // offset 24
      optional_bool_t  use_mmap;       // offset 32
      optional_bool_t  use_mlock;      // offset 40
      optional_bool_t  check_tensors;  // offset 48
      bool enable_abort_init;          // offset 56 (i8, padded)
  } llama_model_params_t;               // size: 60

  typedef struct {
      // Each optional_* is 8 bytes: { int32_t has_value; <T> value; }
      optional_uint32_t n_ctx;               // 0
      optional_uint32_t n_batch;             // 8
      optional_uint32_t n_ubatch;            // 16
      optional_uint32_t n_seq_max;           // 24
      optional_int32_t  n_threads;           // 32
      optional_int32_t  n_threads_batch;     // 40
      optional_int32_t  rope_scaling_type;   // 48
      optional_int32_t  pooling_type;        // 56
      optional_int32_t  attention_type;      // 64
      optional_float_t  rope_freq_base;      // 72
      optional_float_t  rope_freq_scale;     // 80
      optional_float_t  yarn_ext_factor;     // 88
      optional_float_t  yarn_attn_factor;    // 96
      optional_float_t  yarn_beta_fast;      // 104
      optional_float_t  yarn_beta_slow;      // 112
      optional_uint32_t yarn_orig_ctx;       // 120
      optional_float_t  defrag_thold;        // 128
      optional_bool_t   embeddings;          // 136
      optional_bool_t   offload_kqv;         // 144
      optional_bool_t   flash_attn;          // 152
      optional_bool_t   no_perf;             // 160
      optional_bool_t   op_offload;          // 168
      optional_bool_t   swa_full;            // 176
      bool enable_abort_generation;          // 184 (i8, padded)
  } llama_context_params_t;                    // size: 188

  typedef struct {
      const char*            model_path;   // 0
      llama_model_params_t   model_params; // 4
      llama_context_params_t context_params; // 64
  } llama_initialization_params_t;           // size: 252
*/

const INITIALIZATION_OFFSETS = Object.freeze({
    opt: {
        has: 0,
        value: 4,
        SIZE: 8
    },
    model: {
        n_gpu_layers: 0,
        split_mode: 8,
        main_gpu: 16,
        vocab_only: 24,
        use_mmap: 32,
        use_mlock: 40,
        check_tensors: 48,
        enable_abort_init: 56, // i8
        SIZE: 60
    },
    ctx: {
        n_ctx: 0,
        n_batch: 8,
        n_ubatch: 16,
        n_seq_max: 24,
        n_threads: 32,
        n_threads_batch: 40,
        rope_scaling_type: 48,
        pooling_type: 56,
        attention_type: 64,
        rope_freq_base: 72,
        rope_freq_scale: 80,
        yarn_ext_factor: 88,
        yarn_attn_factor: 96,
        yarn_beta_fast: 104,
        yarn_beta_slow: 112,
        yarn_orig_ctx: 120,
        defrag_thold: 128,
        embeddings: 136,
        offload_kqv: 144,
        flash_attn: 152,
        no_perf: 160,
        op_offload: 168,
        swa_full: 176,
        enable_abort_generation: 184, // i8
        SIZE: 188
    },
    init: {
        model_path: 0,
        model_params: 4,
        context_params: 64,
        SIZE: 252
    }
});

function validateNonEmptyString(val, fieldName) {
    const s = validateString(val, fieldName);
    if (!s) throw new Error(`${fieldName} must be a non-empty string`);
    return s;
}

function isOptionalObjectLike(v) {
    return v !== null && typeof v === 'object' && (
        ('HasValue' in v) || ('hasValue' in v) || ('value' in v)
    );
}
function normalizeOptionalI32(v, name) {
    if (v === undefined || v === null) return { has: false, value: 0 };
    if (isOptionalObjectLike(v)) {
        const hv = ('HasValue' in v) ? !!v.HasValue : (('hasValue' in v) ? !!v.hasValue : (v.value !== undefined));
        if (!hv) return { has: false, value: 0 };
        if (!isInt32(v.value)) throw new Error(`${name}.value must be int32`);
        return { has: true, value: asInt32(v.value) };
    }
    if (!isInt32(v)) throw new Error(`${name} must be int32`);
    return { has: true, value: asInt32(v) };
}
function normalizeOptionalU32(v, name) {
    if (v === undefined || v === null) return { has: false, value: 0 };
    if (isOptionalObjectLike(v)) {
        const hv = ('HasValue' in v) ? !!v.HasValue : (('hasValue' in v) ? !!v.hasValue : (v.value !== undefined));
        if (!hv) return { has: false, value: 0 };
        if (!isUInt32(v.value)) throw new Error(`${name}.value must be uint32`);
        return { has: true, value: asUInt32(v.value) };
    }
    if (!isUInt32(v)) throw new Error(`${name} must be uint32`);
    return { has: true, value: asUInt32(v) };
}
function normalizeOptionalFloat(v, name) {
    if (v === undefined || v === null) return { has: false, value: 0.0 };
    if (isOptionalObjectLike(v)) {
        const hv = ('HasValue' in v) ? !!v.HasValue : (('hasValue' in v) ? !!v.hasValue : (v.value !== undefined));
        if (!hv) return { has: false, value: 0.0 };
        if (!isFloat(v.value)) throw new Error(`${name}.value must be a number`);
        return { has: true, value: asFloat(v.value) };
    }
    if (!isFloat(v)) throw new Error(`${name} must be a number`);
    return { has: true, value: asFloat(v) };
}
function normalizeOptionalBool(v, name) {
    if (v === undefined || v === null) return { has: false, value: 0 };
    if (isOptionalObjectLike(v)) {
        const hv = ('HasValue' in v) ? !!v.HasValue : (('hasValue' in v) ? !!v.hasValue : (v.value !== undefined));
        if (!hv) return { has: false, value: 0 };
        if (!isBoolish(v.value)) throw new Error(`${name}.value must be boolean or 0/1`);
        return { has: true, value: toBool(v.value) ? 1 : 0 };
    }
    if (!isBoolish(v)) throw new Error(`${name} must be boolean or 0/1`);
    return { has: true, value: toBool(v) ? 1 : 0 };
}

// Validate and normalize JSON produced by LlamaInitializationParams.cs
function validateLlamaInitializationParamsJson(json) {
    const j = typeof json === 'string' ? JSON.parse(json) : json;
    if (!j || typeof j !== 'object') {
        throw new Error('Input must be a JSON object or JSON string');
    }

    const modelPath = validateNonEmptyString(j.modelPath, 'modelPath');

    // Model params
    const model = {
        nGpuLayers: normalizeOptionalI32(j.gpuLayers, 'gpuLayers'),
        splitMode: normalizeOptionalI32(j.splitMode, 'splitMode'),
        mainGpu: normalizeOptionalI32(j.mainGpu, 'mainGpu'),
        vocabOnly: normalizeOptionalBool(j.vocabOnly, 'vocabOnly'),
        useMmap: normalizeOptionalBool(j.useMmap, 'useMmap'),
        useMlock: normalizeOptionalBool(j.useMlock, 'useMlock'),
        checkTensors: normalizeOptionalBool(j.checkTensors, 'checkTensors'),
        enableAbortInit: !!j.enableAbortInit
    };

    // Context params
    const ctx = {
        nCtx: normalizeOptionalU32(j.contextLength, 'contextLength'),
        nBatch: normalizeOptionalU32(j.batchSize, 'batchSize'),
        nUBatch: normalizeOptionalU32(j.ubatchSize, 'ubatchSize'),
        nSeqMax: normalizeOptionalU32(j.maxSequences, 'maxSequences'),
        nThreads: normalizeOptionalI32(j.threads, 'threads'),
        nThreadsBatch: normalizeOptionalI32(j.threadsBatch, 'threadsBatch'),
        ropeScalingType: normalizeOptionalI32(j.ropeScalingType, 'ropeScalingType'),
        poolingType: normalizeOptionalI32(j.poolingType, 'poolingType'),
        attentionType: normalizeOptionalI32(j.attentionType, 'attentionType'),
        ropeFreqBase: normalizeOptionalFloat(j.ropeFreqBase, 'ropeFreqBase'),
        ropeFreqScale: normalizeOptionalFloat(j.ropeFreqScale, 'ropeFreqScale'),
        yarnExtFactor: normalizeOptionalFloat(j.yarnExtFactor, 'yarnExtFactor'),
        yarnAttnFactor: normalizeOptionalFloat(j.yarnAttnFactor, 'yarnAttnFactor'),
        yarnBetaFast: normalizeOptionalFloat(j.yarnBetaFast, 'yarnBetaFast'),
        yarnBetaSlow: normalizeOptionalFloat(j.yarnBetaSlow, 'yarnBetaSlow'),
        yarnOrigCtx: normalizeOptionalU32(j.yarnOrigCtx, 'yarnOrigCtx'),
        defragThold: normalizeOptionalFloat(j.defragThreshold, 'defragThreshold'),
        embeddings: normalizeOptionalBool(j.embeddings, 'embeddings'),
        offloadKqv: normalizeOptionalBool(j.offloadKqv, 'offloadKqv'),
        flashAttn: normalizeOptionalBool(j.flashAttn, 'flashAttn'),
        noPerf: normalizeOptionalBool(j.noPerf, 'noPerf'),
        opOffload: normalizeOptionalBool(j.opOffload, 'opOffload'),
        swaFull: normalizeOptionalBool(j.swaFull, 'swaFull'),
        enableAbortGeneration: !!j.enableAbortGeneration
    };

    return {
        modelPath,
        model,
        context: ctx
    };
}

function writeOptI32(ModuleHelpers, addr, opt) {
    const { setI32 } = ModuleHelpers;
    setI32(addr + INITIALIZATION_OFFSETS.opt.has, opt.has ? 1 : 0);
    setI32(addr + INITIALIZATION_OFFSETS.opt.value, opt.value | 0);
}
function writeOptU32(ModuleHelpers, addr, opt) {
    const { setU32, setI32 } = ModuleHelpers;
    setI32(addr + INITIALIZATION_OFFSETS.opt.has, opt.has ? 1 : 0);
    setU32(addr + INITIALIZATION_OFFSETS.opt.value, opt.value >>> 0);
}
function writeOptF32(ModuleHelpers, addr, opt) {
    const { setI32, setF32 } = ModuleHelpers;
    setI32(addr + INITIALIZATION_OFFSETS.opt.has, opt.has ? 1 : 0);
    setF32(addr + INITIALIZATION_OFFSETS.opt.value, +opt.value);
}
function writeOptBool(ModuleHelpers, addr, opt) {
    const { setI32, setI32: setValI32 } = ModuleHelpers;
    setI32(addr + INITIALIZATION_OFFSETS.opt.has, opt.has ? 1 : 0);
    setValI32(addr + INITIALIZATION_OFFSETS.opt.value, (opt.value ? 1 : 0) | 0);
}

// Allocate and populate a llama_initialization_params_t and all nested buffers.
// Returns { ptr, free: () => void, allocations, normalized }
function allocLlamaInitializationParams(Module, json) {
    if (!Module || typeof Module.setValue !== 'function' || typeof Module._malloc !== 'function') {
        throw new Error('Module with setValue and _malloc is required');
    }

    const cfg = validateLlamaInitializationParamsJson(json);
    const H = makeAllocHelpers(Module);

    // Pre-allocate strings
    const modelPathPtr = H.allocCString(cfg.modelPath);

    // Allocate the top-level struct
    const base = H.malloc(INITIALIZATION_OFFSETS.init.SIZE);

    // Zero the memory (important for paddings)
    for (let i = 0; i < INITIALIZATION_OFFSETS.init.SIZE; i++) {
        Module.setValue(base + i, 0, 'i8');
    }

    // model_path
    H.setU32(base + INITIALIZATION_OFFSETS.init.model_path, modelPathPtr >>> 0);

    // model_params
    const mp = base + INITIALIZATION_OFFSETS.init.model_params;
    writeOptI32(H, mp + INITIALIZATION_OFFSETS.model.n_gpu_layers, cfg.model.nGpuLayers);
    writeOptI32(H, mp + INITIALIZATION_OFFSETS.model.split_mode, cfg.model.splitMode);
    writeOptI32(H, mp + INITIALIZATION_OFFSETS.model.main_gpu, cfg.model.mainGpu);
    writeOptBool(H, mp + INITIALIZATION_OFFSETS.model.vocab_only, cfg.model.vocabOnly);
    writeOptBool(H, mp + INITIALIZATION_OFFSETS.model.use_mmap, cfg.model.useMmap);
    writeOptBool(H, mp + INITIALIZATION_OFFSETS.model.use_mlock, cfg.model.useMlock);
    writeOptBool(H, mp + INITIALIZATION_OFFSETS.model.check_tensors, cfg.model.checkTensors);
    H.setI8(mp + INITIALIZATION_OFFSETS.model.enable_abort_init, cfg.model.enableAbortInit ? 1 : 0);

    // context_params
    const cp = base + INITIALIZATION_OFFSETS.init.context_params;
    writeOptU32(H, cp + INITIALIZATION_OFFSETS.ctx.n_ctx, cfg.context.nCtx);
    writeOptU32(H, cp + INITIALIZATION_OFFSETS.ctx.n_batch, cfg.context.nBatch);
    writeOptU32(H, cp + INITIALIZATION_OFFSETS.ctx.n_ubatch, cfg.context.nUBatch);
    writeOptU32(H, cp + INITIALIZATION_OFFSETS.ctx.n_seq_max, cfg.context.nSeqMax);
    writeOptI32(H, cp + INITIALIZATION_OFFSETS.ctx.n_threads, cfg.context.nThreads);
    writeOptI32(H, cp + INITIALIZATION_OFFSETS.ctx.n_threads_batch, cfg.context.nThreadsBatch);
    writeOptI32(H, cp + INITIALIZATION_OFFSETS.ctx.rope_scaling_type, cfg.context.ropeScalingType);
    writeOptI32(H, cp + INITIALIZATION_OFFSETS.ctx.pooling_type, cfg.context.poolingType);
    writeOptI32(H, cp + INITIALIZATION_OFFSETS.ctx.attention_type, cfg.context.attentionType);
    writeOptF32(H, cp + INITIALIZATION_OFFSETS.ctx.rope_freq_base, cfg.context.ropeFreqBase);
    writeOptF32(H, cp + INITIALIZATION_OFFSETS.ctx.rope_freq_scale, cfg.context.ropeFreqScale);
    writeOptF32(H, cp + INITIALIZATION_OFFSETS.ctx.yarn_ext_factor, cfg.context.yarnExtFactor);
    writeOptF32(H, cp + INITIALIZATION_OFFSETS.ctx.yarn_attn_factor, cfg.context.yarnAttnFactor);
    writeOptF32(H, cp + INITIALIZATION_OFFSETS.ctx.yarn_beta_fast, cfg.context.yarnBetaFast);
    writeOptF32(H, cp + INITIALIZATION_OFFSETS.ctx.yarn_beta_slow, cfg.context.yarnBetaSlow);
    writeOptU32(H, cp + INITIALIZATION_OFFSETS.ctx.yarn_orig_ctx, cfg.context.yarnOrigCtx);
    writeOptF32(H, cp + INITIALIZATION_OFFSETS.ctx.defrag_thold, cfg.context.defragThold);
    writeOptBool(H, cp + INITIALIZATION_OFFSETS.ctx.embeddings, cfg.context.embeddings);
    writeOptBool(H, cp + INITIALIZATION_OFFSETS.ctx.offload_kqv, cfg.context.offloadKqv);
    writeOptBool(H, cp + INITIALIZATION_OFFSETS.ctx.flash_attn, cfg.context.flashAttn);
    writeOptBool(H, cp + INITIALIZATION_OFFSETS.ctx.no_perf, cfg.context.noPerf);
    writeOptBool(H, cp + INITIALIZATION_OFFSETS.ctx.op_offload, cfg.context.opOffload);
    writeOptBool(H, cp + INITIALIZATION_OFFSETS.ctx.swa_full, cfg.context.swaFull);
    H.setI8(cp + INITIALIZATION_OFFSETS.ctx.enable_abort_generation, cfg.context.enableAbortGeneration ? 1 : 0);

    return {
        ptr: base >>> 0,
        free: H.freeAll,
        allocations: H.allocations,
        normalized: cfg
    };
}

////////////////////////////////////
// worker.js main body            //
////////////////////////////////////

const prepareMessage = function(message) {
    if (typeof message === 'string') {
        return message;
    } else {
        return String(Boolean(message));
    }
}

const logCallback = function(level, messagePtr) {
    const message = self.module.UTF8ToString(messagePtr);
    if (level === 0) {
        self.module.print('[Aviad Info]: ' + message);
    } else if (level === 1) {
        self.module.print('[Aviad Warning]: ' + message);
    } else if (level === 2) {
        self.module.print('[Aviad Error]: ' + message);
    } else {
        self.module.print('[Aviad Error]: ' + message);
    }
};
let logCallbackPtr;

const create_module = (callbackId) => {
    createModule().then((Module) => {
        Module.print("AviadCPP Module initialized");
        self.module = Module;
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(true),
        });
    });
};

const moduleStringToNewUTF8 = function(str) {
    if (!str || typeof self.module === 'undefined') return null;
    const len = self.module.lengthBytesUTF8(str) + 1; // +1 for null terminator
    const ptr = self.module._malloc(len);
    self.module.stringToUTF8(str, ptr, len);
    return ptr;
}

const set_logging_enabled = (callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    try {
        logCallbackPtr = self.module.addFunction(logCallback, 'vii');
        self.module._set_log_callback(logCallbackPtr);
    } catch (e) {
        console.error('Failed to set logging: ' + e.message);
    }
    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(true),
    });
};

const init_context = (contextKey, messagesJson, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);
    let messages;
    try {
        messages = JSON.parse(messagesJson);
    } catch (e) {
        self.module.printErr('Failed to parse messages: ' + e.message);
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    // Allocate memory for arrays of role pointers and content pointers
    const messageCount = messages.length || 0;
    const rolesPtr = self.module._malloc(4 * messageCount);  // array of char* (4 bytes per pointer)
    const contentsPtr = self.module._malloc(4 * messageCount);  // array of char* (4 bytes per pointer)

    // Allocate and populate each role and content string
    for (let i = 0; i < messageCount; i++) {
        const rolePtr = moduleStringToNewUTF8(messages[i].role);
        const contentPtr = moduleStringToNewUTF8(messages[i].content);
        // Store pointers in the arrays
        self.module.setValue(rolesPtr + (i * 4), rolePtr, 'i32');
        self.module.setValue(contentsPtr + (i * 4), contentPtr, 'i32');
    }

    // Create the message sequence struct (12 bytes total)
    const seqPtr = self.module._malloc(12);
    self.module.setValue(seqPtr, rolesPtr, 'i32');         // roles array pointer
    self.module.setValue(seqPtr + 4, contentsPtr, 'i32');  // contents array pointer
    self.module.setValue(seqPtr + 8, messageCount, 'i32'); // message count

    let response = false;
    try {
        // Call the native function with the context key and message sequence
        const result = self.module._init_context(contextKeyCopyPtr, seqPtr);
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to init context: ' + e.message);
    }

    // Clean up allocated memory (the native side should have copied what it needs)
    for (let i = 0; i < messageCount; i++) {
        const rolePtr = self.module.getValue(rolesPtr + (i * 4), 'i32');
        const contentPtr = self.module.getValue(contentsPtr + (i * 4), 'i32');
        self.module._free(rolePtr);
        self.module._free(contentPtr);
    }
    self.module._free(rolesPtr);
    self.module._free(contentsPtr);
    self.module._free(seqPtr);
    self.module._free(contextKeyCopyPtr);

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const get_context = (contextKey, maxMessages, maxStrLen, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(""),
        });
        return;
    }

    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);
    const rolesPtr = self.module._malloc(maxMessages * 4);    // Pointers to roles
    const contentsPtr = self.module._malloc(maxMessages * 4); // Pointers to contents
    const roleStrPtrs = [];
    const contentStrPtrs = [];

    for (let i = 0; i < maxMessages; i++) {
        const roleBuf = self.module._malloc(maxStrLen);
        const contentBuf = self.module._malloc(maxStrLen);
        self.module.setValue(rolesPtr + i * 4, roleBuf, 'i32');
        self.module.setValue(contentsPtr + i * 4, contentBuf, 'i32');
        roleStrPtrs.push(roleBuf);
        contentStrPtrs.push(contentBuf);
    }

    const seqPtr = self.module._malloc(12);
    self.module.setValue(seqPtr, rolesPtr, 'i32');
    self.module.setValue(seqPtr + 4, contentsPtr, 'i32');
    self.module.setValue(seqPtr + 8, 0, 'i32'); // count

    let serializedResult = "";
    try {
        const result = self.module._get_context(contextKeyCopyPtr, seqPtr, maxMessages, maxStrLen);
        if (result === 1) {
            const messageCount = self.module.getValue(seqPtr + 8, 'i32');
            const messageSequence = {
                messages: []
            };
            for (let i = 0; i < messageCount; i++) {
                const rolePtr = self.module.getValue(rolesPtr + (i * 4), 'i32');
                const contentPtr = self.module.getValue(contentsPtr + (i * 4), 'i32');
                const role = self.module.UTF8ToString(rolePtr);
                const content = self.module.UTF8ToString(contentPtr);
                messageSequence.messages.push({
                    role: role,
                    content: content
                });
            }
            serializedResult = JSON.stringify(messageSequence);
        }
    } catch (e) {
        self.module.printErr('Failed to get context: ' + e.message);
    } finally {
        // Clean up all allocated memory
        for (let i = 0; i < maxMessages; i++) {
            self.module._free(roleStrPtrs[i]);
            self.module._free(contentStrPtrs[i]);
        }
        self.module._free(rolesPtr);
        self.module._free(contentsPtr);
        self.module._free(seqPtr);
        self.module._free(contextKeyCopyPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(serializedResult),
    });
};

const add_turn_to_context = (contextKey, role, content, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);
    const roleCopyPtr = moduleStringToNewUTF8(role);
    const contentCopyPtr = moduleStringToNewUTF8(content);

    let result = false;
    try {
        const response = self.module._add_turn_to_context(contextKeyCopyPtr, roleCopyPtr, contentCopyPtr);
        result = response === 1;
    } catch (e) {
        self.module.printErr('Failed to add turn to context: ' + e.message);
    } finally {
        self.module._free(contextKeyCopyPtr);
        self.module._free(roleCopyPtr);
        self.module._free(contentCopyPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(result),
    });
};

const append_to_context = (contextKey, content, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);
    const contentCopyPtr = moduleStringToNewUTF8(content);

    let result = false;
    try {
        const response = self.module._append_to_context(contextKeyCopyPtr, contentCopyPtr);
        result = response === 1;
    } catch (e) {
        self.module.printErr('Failed to append to context: ' + e.message);
    } finally {
        self.module._free(contextKeyCopyPtr);
        self.module._free(contentCopyPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(result),
    });
};

const copy_context = (sourceContextKey, targetContextKey, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const sourceContextKeyCopyPtr = moduleStringToNewUTF8(sourceContextKey);
    const targetContextKeyCopyPtr = moduleStringToNewUTF8(targetContextKey);

    let result = false;
    try {
        const response = self.module._copy_context(sourceContextKeyCopyPtr, targetContextKeyCopyPtr);
        result = response === 1;
    } catch (e) {
        self.module.printErr('Failed to copy context: ' + e.message);
    } finally {
        self.module._free(sourceContextKeyCopyPtr);
        self.module._free(targetContextKeyCopyPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(result),
    });
};

const free_context = (contextKey, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);

    let result = false;
    try {
        const response = self.module._free_context(contextKeyCopyPtr);
        result = response === 1;
    } catch (e) {
        self.module.printErr('Failed to free context: ' + e.message);
    } finally {
        self.module._free(contextKeyCopyPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(result),
    });
};

const initialize_model = (modelId, modelParamsJson, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    let modelParams;
    try {
        modelParams = JSON.parse(modelParamsJson);
    } catch (e) {
        console.error('Failed to parse model params: ' + e.message);
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    if (!modelParams.modelPath) {
        self.module.printErr('Model path not passed correctly. ', modelParams.modelPath);
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);
    const wasmModelPathPtr = moduleStringToNewUTF8(modelParams.modelPath);

    // Helper to set optional values directly in memory
    const setOptionalInt32 = (ptr, value) => {
        if (value !== undefined && value !== null && value.HasValue) {
            self.module.setValue(ptr, 1, 'i32');  // has_value = true
            self.module.setValue(ptr + 4, value.value, 'i32');  // value
        } else {
            self.module.setValue(ptr, 0, 'i32');  // has_value = false
            self.module.setValue(ptr + 4, 0, 'i32');  // value = 0
        }
    };

    const setOptionalFloat = (ptr, value) => {
        if (value !== undefined && value !== null && value.HasValue) {
            self.module.setValue(ptr, 1, 'i32');  // has_value = true
            self.module.setValue(ptr + 4, value.value, 'float');  // value
        } else {
            self.module.setValue(ptr, 0, 'i32');  // has_value = false
            self.module.setValue(ptr + 4, 0, 'float');  // value = 0
        }
    };

    const setOptionalBool = (ptr, value) => {
        if (value !== undefined && value !== null && value.HasValue) {
            self.module.setValue(ptr, 1, 'i32');  // has_value = true
            self.module.setValue(ptr + 4, value.value ? 1 : 0, 'i32');  // bool as int
        } else {
            self.module.setValue(ptr, 0, 'i32');  // has_value = false
            self.module.setValue(ptr + 4, 0, 'i32');  // value = 0
        }
    };

    const initConfig = validateLlamaInitializationParamsJson(modelParams);
    const { ptr: initParamsPtr, free: freeInitParams } = allocLlamaInitializationParams(self.module, initConfig);

    // Use setTimeout to ensure async behavior
    setTimeout(() => {
        let response = false;
        try {
            const result = self.module._initialize_model(modelIdPtr, initParamsPtr);
            response = result === 1;
        } catch (e) {
            console.error('Failed to initialize model: ' + e.message);
        } finally {
            freeInitParams();
            self.module._free(wasmModelPathPtr);
            self.module._free(modelIdPtr);
        }

        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(response),
        });
    }, 0);
};

const shutdown_model = (modelId, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);

    setTimeout(() => {
        let response = false;
        try {
            const result = self.module._shutdown_model(modelIdPtr);
            response = result === 1;
        } catch (e) {
            self.module.printErr('Failed to shutdown model: ' + e.message);
        } finally {
            self.module._free(modelIdPtr);
        }

        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(response),
        });
    }, 0);
};

const abort_initialize_model = (modelId, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);

    setTimeout(() => {
        let response = false;
        try {
            const result = self.module._abort_initialize_model(modelIdPtr);
            response = result === 1;
        } catch (e) {
            self.module.printErr('Failed to abort initialize model: ' + e.message);
        } finally {
            self.module._free(modelIdPtr);
        }

        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(response),
        });
    }, 0);
};

const load_context = (modelId, contextKey, templateParamsJson, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);
    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);

    let templateParams;
    try {
        templateParams = JSON.parse(templateParamsJson);
    } catch (e) {
        console.error('Failed to parse chat template params: ' + e.message);
        self.module._free(modelIdPtr);
        self.module._free(contextKeyCopyPtr);
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    // Create chat_template_params_t struct - you'll need to adjust this based on your actual struct definition
    const templatePtr = moduleStringToNewUTF8(templateParams.template || "llama3");
    const paramsSize = 4; // Assuming just a template pointer for now
    const templateParamsPtr = self.module._malloc(paramsSize);
    self.module.setValue(templateParamsPtr, templatePtr, 'i32'); // template pointer

    let response = false;
    try {
        const result = self.module._load_context(modelIdPtr, contextKeyCopyPtr, templateParamsPtr);
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to load context: ' + e.message);
    } finally {
        self.module._free(modelIdPtr);
        self.module._free(contextKeyCopyPtr);
        self.module._free(templatePtr);
        self.module._free(templateParamsPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const cache_context = (modelId, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);

    let response = false;
    try {
        const result = self.module._cache_context(modelIdPtr);
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to cache context: ' + e.message);
    } finally {
        self.module._free(modelIdPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const unload_active_context = (modelId, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);

    let response = false;
    try {
        const result = self.module._unload_active_context(modelIdPtr);
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to unload active context: ' + e.message);
    } finally {
        self.module._free(modelIdPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const abort_generation = (modelId, callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);

    let response = false;
    try {
        const result = self.module._abort_generation(modelIdPtr);
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to abort generation: ' + e.message);
    } finally {
        self.module._free(modelIdPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const generate_response = (modelId, contextKey, outContextKey, genParamsJson, onTokenCallbackId, onDoneCallbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: onDoneCallbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);
    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);
    const outContextKeyCopyPtr = moduleStringToNewUTF8(outContextKey);

    let config;
    try {
        config = JSON.parse(genParamsJson);
    } catch (e) {
        self.module.printErr('Failed to parse config: ' + e.message);
        self.module._free(modelIdPtr);
        self.module._free(contextKeyCopyPtr);
        self.module._free(outContextKeyCopyPtr);
        postMessage({
            event: 'unity_callback',
            callbackId: onDoneCallbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    // Marshal string pointers
    const grammarStrPtr = moduleStringToNewUTF8(config.grammarStr || "");
    const grammarRootPtr = moduleStringToNewUTF8(config.grammarRoot || "");

    // Marshal grammar trigger patterns array
    const grammarPatterns = config.grammarTriggerPatterns || [];
    const grammarPatternsCount = grammarPatterns.length;
    let grammarPatternsArrayPtr = 0;
    const grammarPatternPtrs = [];

    if (grammarPatternsCount > 0) {
        grammarPatternsArrayPtr = self.module._malloc(4 * grammarPatternsCount);
        for (let i = 0; i < grammarPatternsCount; i++) {
            const patternPtr = moduleStringToNewUTF8(grammarPatterns[i]);
            grammarPatternPtrs.push(patternPtr);
            self.module.setValue(grammarPatternsArrayPtr + (i * 4), patternPtr, 'i32');
        }
    }

    // Marshal dry sequence breakers array
    const drySeqBreakers = config.drySeqBreakers || [];
    const dryBreakersCount = drySeqBreakers.length;
    let dryBreakersArrayPtr = 0;
    const dryBreakerPtrs = [];

    if (dryBreakersCount > 0) {
        dryBreakersArrayPtr = self.module._malloc(4 * dryBreakersCount);
        for (let i = 0; i < dryBreakersCount; i++) {
            const breakerPtr = moduleStringToNewUTF8(drySeqBreakers[i]);
            dryBreakerPtrs.push(breakerPtr);
            self.module.setValue(dryBreakersArrayPtr + (i * 4), breakerPtr, 'i32');
        }
    }

    const genConfig = validateLlamaGenerationConfigJson(config);
    const { ptr: nativeParamsPtr, free: freeGenParams } = allocLlamaGenerationParams(self.module, genConfig);

    const tokenCallback = function(token) {
        postMessage({
            event: 'unity_callback',
            callbackId: onTokenCallbackId,
            messagePtr: prepareMessage(self.module.UTF8ToString(token)),
        });
    };

    const doneCallback = function(success) {
        postMessage({
            event: 'unity_callback',
            callbackId: onDoneCallbackId,
            messagePtr: prepareMessage(success),
        });
    };

    const tokenCallbackPtr = self.module.addFunction(tokenCallback, 'vi');
    const doneCallbackPtr = self.module.addFunction(doneCallback, 'vi');

    setTimeout(() => {
        try {
            self.module._generate_response(
                modelIdPtr,
                contextKeyCopyPtr,
                outContextKeyCopyPtr,
                nativeParamsPtr,
                tokenCallbackPtr,
                doneCallbackPtr
            );
        } catch (e) {
            self.module.printErr('Generation error: ', e.message);
            postMessage({
                event: 'unity_callback',
                callbackId: onDoneCallbackId,
                messagePtr: prepareMessage(false),
            });
        } finally {
            // Clean up all allocated memory
            self.module._free(modelIdPtr);
            self.module._free(contextKeyCopyPtr);
            self.module._free(outContextKeyCopyPtr);
            self.module._free(grammarStrPtr);
            self.module._free(grammarRootPtr);
            self.module._free(chatTemplatePtr);
            self.module._free(samplerChainPtr);

            // Free grammar pattern strings and array
            for (const ptr of grammarPatternPtrs) {
                self.module._free(ptr);
            }
            if (grammarPatternsArrayPtr) {
                self.module._free(grammarPatternsArrayPtr);
            }

            // Free dry breaker strings and array
            for (const ptr of dryBreakerPtrs) {
                self.module._free(ptr);
            }
            if (dryBreakersArrayPtr) {
                self.module._free(dryBreakersArrayPtr);
            }

            freeGenParams();
            self.module.removeFunction(tokenCallbackPtr);
            self.module.removeFunction(doneCallbackPtr);
        }
    }, 0);
};

const download_file = function (url, targetPath, callbackId) {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    fetch(url)
        .then(response => {
            if (!response.ok) throw new Error("HTTP error " + response.status);
            return response.arrayBuffer();
        })
        .then(data => {
            try {
                const uint8Array = new Uint8Array(data);
                self.module.FS.writeFile(targetPath, uint8Array);
                postMessage({
                    event: 'unity_callback',
                    callbackId: callbackId,
                    messagePtr: prepareMessage(true),
                });
            } catch (writeErr) {
                console.error("[aviad_web_worker] Write failed:", writeErr.message);
                postMessage({
                    event: 'unity_callback',
                    callbackId: callbackId,
                    messagePtr: prepareMessage(false),
                });
            }
        })
        .catch(err => {
            self.module.printErr("[aviad_web_worker] Download failed:", err.message);
            postMessage({
                event: 'unity_callback',
                callbackId: callbackId,
                messagePtr: prepareMessage(false),
            });
        });
};

const compute_embeddings = function(modelId, context, embeddingParamsJson, callbackId) {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);
    const contextPtr = moduleStringToNewUTF8(context);

    let embeddingParams;
    try {
        embeddingParams = JSON.parse(embeddingParamsJson);
    } catch (e) {
        self.module.printErr('Failed to parse embedding params: ' + e.message);
        self.module._free(modelIdPtr);
        self.module._free(contextPtr);
        postMessage({
            event: 'unity_callback',
            callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    try {
        var size = embeddingParams.maxEmbeddingsSize;
        if (!size || size <= 0) {
            size = self.module._get_embeddings_size(modelIdPtr) >>> 0;
        }
        if (!size || size <= 0) {
            self.module._free(modelIdPtr);
            self.module._free(contextPtr);
            postMessage({
                event: 'unity_callback',
                callbackId,
                messagePtr: prepareMessage(false),
            });
            return;
        }

        const outputPtr = self.module._malloc(size * 4);
        const embeddingParamsPtr = self.module._malloc(4);
        self.module.setValue(embeddingParamsPtr, size, 'i32');

        const ok = self.module._compute_embeddings(modelIdPtr, contextPtr, embeddingParamsPtr, outputPtr);
        let success = ok === 1;
        let payload = "[]";

        if (success) {
            const start = outputPtr >> 2;
            const end = start + size;
            const floats = Array.from(self.module.HEAPF32.subarray(start, end));
            // TODO: Make a callback type for sending raw memory.
            payload = JSON.stringify({"floats":floats});
        }

        self.module._free(outputPtr);
        self.module._free(modelIdPtr);
        self.module._free(contextPtr);

        postMessage({
            event: 'unity_callback',
            callbackId,
            messagePtr: payload, // already a JSON string
        });
    } catch (e) {
        self.module.printErr('Failed to compute embeddings: ' + e.message);
        self.module._free(modelIdPtr);
        self.module._free(contextPtr);
        postMessage({
            event: 'unity_callback',
            callbackId,
            messagePtr: prepareMessage(false),
        });
    }
};

const get_embeddings_size = function(modelId, callbackId) {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId,
            messagePtr: "0",
        });
        return;
    }

    const modelIdPtr = moduleStringToNewUTF8(modelId);
    let size = 0;
    try {
        size = self.module._get_embeddings_size(modelIdPtr) >>> 0;
    } catch (e) {
        self.module.printErr('Failed to get embeddings size: ' + e.message);
    } finally {
        self.module._free(modelIdPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId,
        messagePtr: String(size),
    });
};

const process_event = function(event) {
    switch (event.data.event) {
        case 'call_start_web_worker':
            create_module(event.data.callbackId);
            break;
        case 'call_set_logging_enabled':
            set_logging_enabled(event.data.callbackId);
            break;
        case 'call_init_context':
            init_context(event.data.contextKey, event.data.messagesJson, event.data.callbackId);
            break;
        case 'call_get_context':
            get_context(event.data.contextKey, event.data.maxTurnCount, event.data.maxStringLength, event.data.callbackId);
            break;
        case 'call_add_turn_to_context':
            add_turn_to_context(event.data.contextKey, event.data.role, event.data.content, event.data.callbackId);
            break;
        case 'call_append_to_context':
            append_to_context(event.data.contextKey, event.data.content, event.data.callbackId);
            break;
        case 'call_copy_context':
            copy_context(event.data.sourceContextKey, event.data.targetContextKey, event.data.callbackId);
            break;
        case 'call_free_context':
            free_context(event.data.contextKey, event.data.callbackId);
            break;
        case 'call_initialize_model':
            initialize_model(event.data.modelId, event.data.modelParamsJson, event.data.callbackId);
            break;
        case 'call_abort_initialize_model':
            abort_initialize_model(event.data.modelId, event.data.callbackId);
            break;
        case 'call_shutdown_model':
            shutdown_model(event.data.modelId, event.data.callbackId);
            break;
        case 'call_unload_active_context':
            unload_active_context(event.data.modelId, event.data.callbackId);
            break;
        case 'call_load_context':
            load_context(event.data.modelId, event.data.contextKey, event.data.templateParamsJson, event.data.callbackId);
            break;
        case 'call_cache_context':
            cache_context(event.data.modelId, event.data.callbackId);
            break;
        case 'call_generate_response':
            generate_response(
                event.data.modelId,
                event.data.contextKey,
                event.data.outContextKey,
                event.data.generationParamsJson,
                event.data.onTokenCallbackId,
                event.data.onDoneCallbackId,
            );
            break;
        case 'call_abort_generation':
            abort_generation(event.data.modelId, event.data.callbackId);
            break;
        case 'call_compute_embeddings':
            compute_embeddings(event.data.modelId, event.data.context, event.data.embeddingParamsJson, event.data.callbackId);
            break;
        case 'call_get_embeddings_size':
            get_embeddings_size(event.data.modelId, event.data.callbackId);
            break;
        case 'call_download_file':
            download_file(event.data.url, event.data.targetPath, event.data.callbackId);
            break;
    }
};

self.onmessage = function(event) {
    try {
        process_event(event);
    } catch (e) {
        if (typeof self !== 'undefined' && typeof self.module !== 'undefined') {
            self.module.printErr("[AviadWebWorker] onmessage failed", e);
        }
    }
};