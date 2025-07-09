// AviadGeneration.jslib - Unity WebGL JavaScript interface for Aviad Generation
const AviadGenerationPlugin = {
    $aviadState: {
        logCallback: null,
        workerCallbackWrapper: null,
        workerCallbackPtr: null,
        isInitialized: false,
    },

    AviadStartWebWorker: function(callbackIdPtr, callbackPtr) {
        // Setup callback to C#
        aviadState.workerCallbackPtr = callbackPtr
        aviadState.workerCallbackWrapper = function(callbackId, message) {
            if (typeof callbackId !== 'string' || typeof message !== 'string') {
                console.error('workerCallback expects both callbackId and message to be strings.', {
                    callbackId,
                    message
                });
                return;
            }
            if (this.workedCallbackPtr !== null) {
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
        window.aviadWebWorker.unityCallback = aviadState.workerCallbackWrapper;

        // Trigger the web worker's initialization of the wasm module.
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_start_web_worker',
                'callbackId': UTF8ToString(callbackIdPtr),
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

    AviadSetLoggingEnabled: function() {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_set_logging_enabled'
            });
        }
    },

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

    AviadInitializeGenerationModel: function(modelParamsJsonPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_initialize_generation_model',
                'modelParamsJson': UTF8ToString(modelParamsJsonPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadShutdownGenerationModel: function(callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_shutdown_generation_model',
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadUnloadActiveContext: function(callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_unload_active_context',
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadLoadContext: function(contextKeyPtr, templateStringPtr, callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_load_context',
                'contextKey': UTF8ToString(contextKeyPtr),
                'templateString': UTF8ToString(templateStringPtr),
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadCacheContext: function(callbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_cache_context',
                'callbackId': UTF8ToString(callbackIdPtr),
            });
        }
    },

    AviadGenerateResponseStreaming: function(contextKeyPtr, outContextKeyPtr, generationParamsJsonPtr, chunkSize, onTokenCallbackIdPtr, onDoneCallbackIdPtr) {
        if (typeof window.aviadWebWorker !== 'undefined' && window.aviadWebWorker.postMessage) {
            window.aviadWebWorker.postMessage({
                'event': 'call_generate_response_streaming',
                'contextKey': UTF8ToString(contextKeyPtr),
                'outContextKey': UTF8ToString(outContextKeyPtr),
                'generationParamsJson': UTF8ToString(generationParamsJsonPtr),
                'chunkSize': chunkSize,
                'onTokenCallbackId': UTF8ToString(onTokenCallbackIdPtr),
                'onDoneCallbackId': UTF8ToString(onDoneCallbackIdPtr),

            });
        }
    },

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
}

// Auto-merge the plugin
autoAddDeps(AviadGenerationPlugin, '$aviadState');
mergeInto(LibraryManager.library, AviadGenerationPlugin);