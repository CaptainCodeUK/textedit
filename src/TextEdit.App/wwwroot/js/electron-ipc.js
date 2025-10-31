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
            console.warn('[IPC] Not in Electron environment, IPC disabled');
            return;
        }

        // Set up listener for this channel
        const listener = (event, data) => {
            console.log(`[IPC] Received ${channel}:`, data);
            
            try {
                if (channel === 'cli-file-args') {
                    dotNetRef.invokeMethodAsync('OnCliFileArgs', data);
                } else if (channel === 'theme-changed') {
                    dotNetRef.invokeMethodAsync('OnThemeChanged', data);
                }
            } catch (error) {
                console.error(`[IPC] Error processing ${channel}:`, error);
            }
        };

        this.listeners[channel] = listener;
        window.ipcRenderer.on(channel, listener);
        console.log(`[IPC] Registered listener for ${channel}`);
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
            console.log(`[IPC] Unregistered listener for ${channel}`);
        }
    },

    /**
     * Send an IPC message to Electron main process
     * @param {string} channel - The IPC channel name
     * @param {any} data - Serializable payload
     */
    send: function(channel, data) {
        if (!window.ipcRenderer) {
            console.warn('[IPC] Not in Electron environment, send ignored');
            return;
        }
        try {
            window.ipcRenderer.send(channel, data);
            console.log(`[IPC] Sent ${channel}:`, data);
        } catch (e) {
            console.warn(`[IPC] Failed to send ${channel}:`, e);
        }
    }
};
