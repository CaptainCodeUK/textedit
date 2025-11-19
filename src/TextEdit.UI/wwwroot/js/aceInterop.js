window.textEditAce = window.textEditAce || {
  editors: {},
  
  loadAce: function(baseUrl) {
    baseUrl = baseUrl || 'https://cdn.jsdelivr.net/npm/ace-builds@1.32.0/src-min-noconflict';
    return new Promise((resolve, reject) => {
      if (window.ace) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = baseUrl + '/ace.js';
      script.onload = () => {
        // Set ACE base path for worker and themes
        if (window.ace) {
          ace.config.set('basePath', baseUrl);
          ace.config.set('modePath', baseUrl);
          ace.config.set('themePath', baseUrl);
          ace.config.set('workerPath', baseUrl);
        }
        resolve();
      };
      script.onerror = reject;
      document.head.appendChild(script);
    });
  },

  createEditor: function(elementId, dotNetRef, options) {
    options = options || {};
    const el = document.getElementById(elementId);
    if (!el) throw new Error('Element not found: ' + elementId);

    const editor = ace.edit(elementId, {
      theme: 'ace/theme/textmate',
      value: options.value || '',
      fontSize: 14,
      showPrintMargin: false,
      wrap: true,
      useSoftTabs: true,
      tabSize: 2
    });

    // DO NOT set mode immediately — ace's $onChangeMode handler may try to access
    // doc.getLength() before the document is fully initialized, causing:
    // "Cannot read properties of null (reading 'getLength')"
    // Instead, delay mode setting until after the document is definitely ready.
    const modeName = 'ace/mode/' + (options.mode || 'markdown');
    setTimeout(() => {
      try {
        const session = editor.getSession && editor.getSession();
        if (session && session.setMode) {
          session.setMode(modeName);
        }
      } catch (err) {
        console.warn('ACE setMode failed:', err);
      }
    }, 500);
  // Listen for content changes
    editor.session.on('change', function() {
      try {
        const value = editor.getValue();
        dotNetRef.invokeMethodAsync('OnEditorContentChanged', value)
          .catch(err => console.warn('ACE OnEditorContentChanged callback failed:', err));
      } catch (e) {
        console.warn('ACE change listener error:', e);
      }
    });

  // Save editor instance for later
    window.textEditAce.editors[elementId] = { editor };
    return true;
  },

  getValue: function(elementId) {
    const entry = window.textEditAce.editors[elementId];
    return entry?.editor.getValue() ?? null;
  },

  setValue: function(elementId, value) {
    const entry = window.textEditAce.editors[elementId];
    if (!entry) return;
    entry.editor.setValue(value, -1); // -1 moves cursor to start
  },

  disposeEditor: function(elementId) {
    const entry = window.textEditAce.editors[elementId];
    if (!entry) return;
    entry.editor.destroy();
    delete window.textEditAce.editors[elementId];
  }
};
