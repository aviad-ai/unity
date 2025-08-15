window.addEventListener('DOMContentLoaded', () => {
    if (window.Worker) {
        window.aviadWebWorker = new Worker('worker.js', {type: "module"});
        window.aviadWebWorker.onmessage = (event) => {
            switch (event.data.event) {
                case "unity_callback":
                    if (typeof window.aviadUnityCallback === 'function') {
                        window.aviadUnityCallback(
                            event.data.callbackId,
                            event.data.messagePtr);
                    } else {
                        console.warn('aviadUnityCallback has not been set.');
                    }
                    break;
            }
        };
        window.aviadWebWorker.onerror = (err) => {
            console.error("Aviad Web Worker crashed:", err.message, err);
        };
        window.aviadWebWorker.onmessageerror = (err) => {
            console.error("Aviad Web Worker message error:", err);
        };
    } else {
        console.warn('Web Workers are not supported in this browser.');
    }
});