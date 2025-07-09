import createModule from './aviad-main.js';

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

const set_logging_enabled = () => {
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
    // TODO: Add callback
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

const append_to_context = (contextKey, text, callbackId) => {
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
    const textCopyPtr = moduleStringToNewUTF8(text);

    let result = false;
    try {
        const response = self.module._append_to_context(contextKeyCopyPtr, textCopyPtr);
        result = response === 1;
    } catch (e) {
        self.module.printErr('Failed to append to context: ' + e.message);
    } finally {
        self.module._free(contextKeyCopyPtr);
        self.module._free(textCopyPtr);
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

const initialize_generation_model = (modelParamsJson, callbackId) => {
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

    const wasmModelPathPtr = moduleStringToNewUTF8(modelParams.modelPath);
    const paramsSize = 20;
    const paramsPtr = self.module._malloc(paramsSize);
    self.module.setValue(paramsPtr, wasmModelPathPtr, 'i32');                          // model_path
    self.module.setValue(paramsPtr + 4, modelParams.maxContextLength || 2048, 'i32');  // max_context_length
    self.module.setValue(paramsPtr + 8, modelParams.gpuLayers || 0, 'i32');            // gpu_layers
    self.module.setValue(paramsPtr + 12, modelParams.threads || 1, 'i32');             // threads
    self.module.setValue(paramsPtr + 16, modelParams.maxBatchLength || 512, 'i32');    // max_batch_length

    // Use setTimeout to ensure async behavior
    setTimeout(() => {
        let response = false;
        try {
            const result = self.module._initialize_generation_model(paramsPtr);
            response = result === 1;
        } catch (e) {
            console.error('Failed to initialize generation model: ' + e.message);
        } finally {
            self.module._free(paramsPtr);
            self.module._free(wasmModelPathPtr);
        }

        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(response),
        });
    }, 0);
};

const shutdown_generation_model = (callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    setTimeout(() => {
        let response = false;
        try {
            const result = self.module._shutdown_generation_model();
            response = result === 1;
        } catch (e) {
            self.module.printErr('Failed to shutdown generation model: ' + e.message);
        }

        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(response),
        });
    }, 0);
};

const load_context = (contextKey, template, callbackId) => {
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
    const templateCopyPtr = moduleStringToNewUTF8(template);

    let response = false;
    try {
        const result = self.module._load_context(contextKeyCopyPtr, templateCopyPtr);
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to load context: ' + e.message);
    } finally {
        self.module._free(contextKeyCopyPtr);
        self.module._free(templateCopyPtr);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const cache_context = (callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    let response = false;
    try {
        const result = self.module._cache_context();
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to cache context: ' + e.message);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const unload_active_context = (callbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: callbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    let response = false;
    try {
        const result = self.module._unload_active_context();
        response = result === 1;
    } catch (e) {
        self.module.printErr('Failed to unload active context: ' + e.message);
    }

    postMessage({
        event: 'unity_callback',
        callbackId: callbackId,
        messagePtr: prepareMessage(response),
    });
};

const generate_response_streaming = (contextKey, outContextKey, genParamsJson, chunkSize, onTokenCallbackId, onDoneCallbackId) => {
    if (typeof self.module === 'undefined') {
        console.error("Module not initialized.");
        postMessage({
            event: 'unity_callback',
            callbackId: onDoneCallbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const contextKeyCopyPtr = moduleStringToNewUTF8(contextKey);
    const outContextKeyCopyPtr = moduleStringToNewUTF8(outContextKey);

    let config;
    try {
        config = JSON.parse(genParamsJson);
    } catch (e) {
        self.module.printErr('Failed to parse config: ' + e.message);
        self.module._free(contextKeyCopyPtr);
        self.module._free(outContextKeyCopyPtr);
        postMessage({
            event: 'unity_callback',
            callbackId: onDoneCallbackId,
            messagePtr: prepareMessage(false),
        });
        return;
    }

    const chatTemplate = config.chatTemplate || "llama3";
    const grammarString = config.grammarString || "";
    const chatTemplatePtr = moduleStringToNewUTF8(chatTemplate);
    const grammarStringPtr = moduleStringToNewUTF8(grammarString);
    const paramsSize = 24;
    const nativeParamsPtr = self.module._malloc(paramsSize);
    self.module.setValue(nativeParamsPtr, chatTemplatePtr, 'i32');                  // chat_template
    self.module.setValue(nativeParamsPtr + 4, grammarStringPtr, 'i32');             // grammar_string
    self.module.setValue(nativeParamsPtr + 8, config.grammarString ? 1 : 0, 'i32'); // use_grammar_string
    self.module.setValue(nativeParamsPtr + 12, config.temperature || 0.7, 'float'); // temperature
    self.module.setValue(nativeParamsPtr + 16, config.topP || 0.9, 'float');        // top_p
    self.module.setValue(nativeParamsPtr + 20, config.maxTokens || 256, 'i32');     // max_tokens

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
            self.module._generate_response_streaming(
                contextKeyCopyPtr,      // const char* context_key
                outContextKeyCopyPtr,   // const char* out_context_key
                nativeParamsPtr,        // const llama_generation_params_t* config
                tokenCallbackPtr,       // token_stream_callback on_token
                doneCallbackPtr,        // stream_done_callback on_done
                chunkSize               // int chunk_size
            );
        } catch (e) {
            self.module.printErr('Generation error: ', e.message);
            postMessage({
                event: 'unity_callback',
                callbackId: onDoneCallbackId,
                messagePtr: prepareMessage(false),
            });
        } finally {
            self.module._free(contextKeyCopyPtr);
            self.module._free(outContextKeyCopyPtr);
            self.module._free(chatTemplatePtr);
            self.module._free(grammarStringPtr);
            self.module._free(nativeParamsPtr);
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

self.onmessage = function(event) {
    switch (event.data.event) {
        case 'call_start_web_worker':
            create_module(event.data.callbackId);
            break;
        case 'call_set_logging_enabled':
            set_logging_enabled();
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
            append_to_context(event.data.contextKey, event.data.text, event.data.callbackId);
            break;
        case 'call_copy_context':
            copy_context(event.data.sourceContextKey, event.data.targetContextKey, event.data.callbackId);
            break;
        case 'call_free_context':
            free_context(event.data.contextKey, event.data.callbackId);
            break;
        case 'call_initialize_generation_model':
            initialize_generation_model(event.data.modelParamsJson, event.data.callbackId);
            break;
        case 'call_shutdown_generation_model':
            shutdown_generation_model(event.data.callbackId);
            break;
        case 'call_unload_active_context':
            unload_active_context(event.data.callbackId);
            break;
        case 'call_load_context':
            load_context(event.data.contextKey, event.data.templateString, event.data.callbackId);
            break;
        case 'call_cache_context':
            cache_context(event.data.callbackId);
            break;
        case 'call_generate_response_streaming':
            generate_response_streaming(
                event.data.contextKey,
                event.data.outContextKey,
                event.data.generationParamsJson,
                event.data.chunkSize,
                event.data.onTokenCallbackId,
                event.data.onDoneCallbackId,
            );
            break;
        case 'call_download_file':
            download_file(event.data.url, event.data.targetPath, event.data.callbackId);
            break;
    }
};