window.addEventListener('DOMContentLoaded', () => {
    if (window.Worker) {
        window.aviadWebWorker = new Worker('worker.js', {type: "module"});
        window.aviadWebWorker.onmessage = (event) => {
            switch (event.data.event) {
                case "unity_callback":
                    if (typeof window.aviadWebWorker.unityCallback === 'function') {
                        window.aviadWebWorker.unityCallback(
                            event.data.callbackId,
                            event.data.messagePtr);
                    } else {
                        console.warn('aviadWebWorker.unityCallback has not been set.');
                    }
                    break;
            }
        };
    } else {
        console.warn('Web Workers are not supported in this browser.');
    }
});