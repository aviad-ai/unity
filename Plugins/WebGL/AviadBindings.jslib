// AviadGeneration.jslib - Unity WebGL JavaScript interface for Aviad Generation
const AviadGenerationPlugin = {
    $aviadState: {
        logCallback: null,
        workerCallbackWrapper: null,
        workerCallbackPtr: null,
        isInitialized: false,
    },

    AviadStartWebWorker: function(onCompleteCallbackIdPtr, callbackPtr) {
        // Setup callback to C#
        aviadState.workerCallbackPtr = callbackPtr;
        aviadState.workerCallbackWrapper = function(callbackId, message) {
            if (typeof callbackId !== 'string' || typeof message !== 'string') {
                console.error('workerCallback expects both callbackId and message to be strings.', {
                    callbackId,
                    message
                });
                return;
            }
            if (aviadState.workerCallbackPtr !== null) {
                const callbackIdPtr = stringToNewUTF8(callbackId);
                const messagePtr = stringToNewUTF8(message);
                try {
                    {{{ makeDynCall('vii', 'aviadState.workerCallbackPtr') }}}(callbackIdPtr, messagePtr);
                } catch (e) {
                    console.error('Error calling worker callback:', e);
                } finally {
                    // Clean up allocated memory
                    if (callbackIdPtr) _free(callbackIdPtr);
                    if (messagePtr) _free(messagePtr);
                }
            } else {
                console.warn('Worker callback to C# has not been set.');
            }
        };

        if (typeof window !== 'undefined') {
            window.aviadUnityCallback = aviadState.workerCallbackWrapper;
        }

        // Trigger the web worker's initialization of the wasm module.
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_start_web_worker',
                'callbackId': UTF8ToString(onCompleteCallbackIdPtr),
            });
        }
    },

    AviadDebug: function(eventTypePtr, messagePtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': UTF8ToString(eventTypePtr),
                'message': UTF8ToString(messagePtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadSetLoggingEnabled: function(callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_set_logging_enabled',
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    // Context management functions (no model_id)
    AviadInitContext: function(contextKeyPtr, messagesJsonPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_init_context',
                'contextKey': UTF8ToString(contextKeyPtr),
                'messagesJson': UTF8ToString(messagesJsonPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadGetContext: function(contextKeyPtr, maxTurnCount, maxStringLength, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_get_context',
                'contextKey': UTF8ToString(contextKeyPtr),
                'maxTurnCount': maxTurnCount,
                'maxStringLength': maxStringLength,
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadAddTurnToContext: function(contextKeyPtr, rolePtr, contentPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_add_turn_to_context',
                'contextKey': UTF8ToString(contextKeyPtr),
                'role': UTF8ToString(rolePtr),
                'content': UTF8ToString(contentPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadAppendToContext: function(contextKeyPtr, contentPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_append_to_context',
                'contextKey': UTF8ToString(contextKeyPtr),
                'content': UTF8ToString(contentPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadCopyContext: function(sourceContextKeyPtr, targetContextKeyPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_copy_context',
                'sourceContextKey': UTF8ToString(sourceContextKeyPtr),
                'targetContextKey': UTF8ToString(targetContextKeyPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadFreeContext: function(contextKeyPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_free_context',
                'contextKey': UTF8ToString(contextKeyPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    // Model management functions
    AviadInitializeModel: function(modelIdPtr, modelParamsJsonPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_initialize_model',
                'modelId': UTF8ToString(modelIdPtr),
                'modelParamsJson': UTF8ToString(modelParamsJsonPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadAbortInitializeModel: function(modelIdPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_abort_initialize_model',
                'modelId': UTF8ToString(modelIdPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadShutdownModel: function(modelIdPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_shutdown_model',
                'modelId': UTF8ToString(modelIdPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadAbortGeneration: function(modelIdPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_abort_generation',
                'modelId': UTF8ToString(modelIdPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    // Context loading and caching
    AviadUnloadActiveContext: function(modelIdPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_unload_active_context',
                'modelId': UTF8ToString(modelIdPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadLoadContext: function(modelIdPtr, contextKeyPtr, templateParamsJsonPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_load_context',
                'modelId': UTF8ToString(modelIdPtr),
                'contextKey': UTF8ToString(contextKeyPtr),
                'templateParamsJson': UTF8ToString(templateParamsJsonPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadCacheContext: function(modelIdPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_cache_context',
                'modelId': UTF8ToString(modelIdPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    // Generation functions
    AviadGenerateResponse: function(modelIdPtr, contextKeyPtr, outContextKeyPtr, generationParamsJsonPtr, onTokenCallbackIdPtr, onDoneCallbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_generate_response',
                'modelId': UTF8ToString(modelIdPtr),
                'contextKey': UTF8ToString(contextKeyPtr),
                'outContextKey': UTF8ToString(outContextKeyPtr),
                'generationParamsJson': UTF8ToString(generationParamsJsonPtr),
                'onTokenCallbackId': UTF8ToString(onTokenCallbackIdPtr),
                'onDoneCallbackId': UTF8ToString(onDoneCallbackIdPtr),
            });
        }
    },

    // Embeddings functions
    AviadComputeEmbeddings: function(modelIdPtr, contextPtr, embeddingParamsJsonPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_compute_embeddings',
                'modelId': UTF8ToString(modelIdPtr),
                'context': UTF8ToString(contextPtr),
                'embeddingParamsJson': UTF8ToString(embeddingParamsJsonPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadGetEmbeddingsSize: function(modelIdPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_get_embeddings_size',
                'modelId': UTF8ToString(modelIdPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    // Utility functions
    AviadDownloadFile: function(urlPtr, targetPathPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_download_file',
                'url': UTF8ToString(urlPtr),
                'targetPath': UTF8ToString(targetPathPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    // Log callback setup
    AviadSetLogCallback: function(callbackPtr) {
        aviadState.logCallback = callbackPtr;
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_set_log_callback'
            });
        }
    },
}

// Auto-merge the plugin
autoAddDeps(AviadGenerationPlugin, '$aviadState');
mergeInto(LibraryManager.library, AviadGenerationPlugin);