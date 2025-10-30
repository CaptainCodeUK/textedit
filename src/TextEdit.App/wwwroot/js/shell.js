// Shell operations - open URLs in system default browser
window.shell = {
    /**
     * Open a URL in the default external browser
     * @param {string} url - The URL to open
     */
    openExternal: function(url) {
        // For Electron apps, we need to use IPC to tell the main process to open the URL
        // But if window.ipcRenderer isn't available, we have to use a workaround
        
        if (window.ipcRenderer) {
            // IPC is available - send message to main process
            window.ipcRenderer.send('shell:openExternal', url);
        } else {
            // IPC not available - use a workaround by setting window.location
            // to a special protocol that will trigger the OS to open externally
            // This won't work perfectly but it's better than opening in-app
            
            // Create a temporary link and click it with target _blank
            // and rel noopener to try to force external behavior
            const link = document.createElement('a');
            link.href = url;
            link.target = '_blank';
            link.rel = 'noopener noreferrer';
            
            // Try to trigger native handling
            link.click();
        }
    }
};
