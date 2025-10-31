// Electron IPC bridge for receiving messages from Electron main process
// Implements contracts from specs/002-v1-1-enhancements/contracts/

window.electronIpc = {
    dotNetRef: null,
    listeners: {},

    /**
     * Register a .NET object reference to receive IPC messages
     * @param {DotNetObjectReference} dotNetRef - The .NET object to invoke
     * @param {string} channel - The IPC channel name
     */
    register: function (dotNetRef, channel) {
        this.dotNetRef = dotNetRef;
        
        // Check if we're in Electron environment
        if (!window.ipcRenderer) {
            // Not running in Electron environment; silently no-op
            return;
        }

        // Set up listener for this channel
        const listener = (event, data) => {
            // Received IPC message for channel
            
            try {
                if (channel === 'cli-file-args') {
                    dotNetRef.invokeMethodAsync('OnCliFileArgs', data);
                } else if (channel === 'theme-changed') {
                    dotNetRef.invokeMethodAsync('OnThemeChanged', data);
                }
            } catch (error) {
                // Swallow errors from JS -> .NET invocations to avoid noisy console output
            }
        };

    this.listeners[channel] = listener;
    window.ipcRenderer.on(channel, listener);
    },

    /**
     * Unregister IPC listener
     * @param {string} channel - The IPC channel name
     */
    unregister: function (channel) {
    if (!window.ipcRenderer) return;

        const listener = this.listeners[channel];
        if (listener) {
            window.ipcRenderer.removeListener(channel, listener);
            delete this.listeners[channel];
        }
    },

    /**
     * Send an IPC message to Electron main process
     * @param {string} channel - The IPC channel name
     * @param {any} data - Serializable payload
     */
    send: function(channel, data) {
        if (!window.ipcRenderer) {
            // Not in Electron - ignore send
            return;
        }
        try {
            window.ipcRenderer.send(channel, data);
        } catch (e) {
            // ignore send errors
        }
    }
};
