window.textEditMonaco = window.textEditMonaco || {
  editors: {},
  loadMonaco: function(baseUrl) {
    baseUrl = baseUrl || 'https://cdn.jsdelivr.net/npm/monaco-editor@0.38.0/min';
    return new Promise((resolve, reject) => {
      if (window.monaco) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = baseUrl + '/vs/loader.js';
      script.onload = () => {
        require.config({ paths: { vs: baseUrl + '/vs' } });
        require(['vs/editor/editor.main'], () => {
          resolve();
        }, reject);
      };
      script.onerror = reject;
      document.head.appendChild(script);
    });
  },

  createEditor: function(elementId, dotNetRef, options) {
    options = options || {};
    const el = document.getElementById(elementId);
    if (!el) throw new Error('Element not found: ' + elementId);

    const editor = monaco.editor.create(el, {
      value: options.value || '',
      language: options.language || 'markdown',
      automaticLayout: true,
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      smoothScrolling: true,
      fontFamily: options.fontFamily || 'Monaco, Menlo, Consolas, "Courier New", monospace',
    });

    const changeListener = editor.onDidChangeModelContent(() => {
      try {
        dotNetRef.invokeMethodAsync('OnEditorContentChanged', editor.getValue());
      } catch (e) { /* ignore */ }
    });

    // Override Alt+P to toggle markdown preview (instead of Monaco's default binding)
    // Ctrl+Tab is handled at Electron menu level for better cross-platform support
    editor.addCommand(
      monaco.KeyMod.Alt | monaco.KeyCode.KeyP,
      () => {
        console.log('[monacoInterop] Alt+P triggered - toggling markdown preview');
        document.dispatchEvent(new CustomEvent('blazor-toggle-preview'));
      }
    );
    
    const keyDownListener = null;

    // Save editor instance for later
    window.textEditMonaco.editors[elementId] = { editor, changeListener, keyDownListener };
    return true;
  },

  getValue: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    return entry?.editor.getValue() ?? null;
  },

  setValue: function(elementId, value) {
    const entry = window.textEditMonaco.editors[elementId];
    entry?.editor.setValue(value);
  },

  disposeEditor: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) return;
    entry.changeListener.dispose();
    if (entry.keyDownListener) entry.keyDownListener.dispose();
    entry.editor.dispose();
    delete window.textEditMonaco.editors[elementId];
  },

  // Update editor options at runtime
  updateOptions: function(elementId, optionsObj) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return false;
    try {
      entry.editor.updateOptions(optionsObj);
      return true;
    } catch (e) {
      console.error('[monacoInterop] updateOptions failed:', e);
      return false;
    }
  },

  // Convenience methods for common toggles
  setLineNumbers: function(elementId, show) {
    return this.updateOptions(elementId, { lineNumbers: show ? 'on' : 'off' });
  },

  setMinimap: function(elementId, enabled) {
    return this.updateOptions(elementId, { minimap: { enabled: enabled } });
  },

  setWordWrap: function(elementId, enabled) {
    return this.updateOptions(elementId, { wordWrap: enabled ? 'on' : 'off' });
  },

  setFontSize: function(elementId, size) {
    return this.updateOptions(elementId, { fontSize: size });
  },

  setFontFamily: function(elementId, fontFamily) {
    return this.updateOptions(elementId, { fontFamily: fontFamily });
  },

  setTheme: function(elementId, theme) {
    // theme can be 'vs', 'vs-dark', 'hc-black', or custom
    try {
      monaco.editor.setTheme(theme);
      return true;
    } catch (e) {
      console.error('[monacoInterop] setTheme failed:', e);
      return false;
    }
  },

  setLanguage: function(elementId, language) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor || !entry.editor.getModel) return false;
    try {
      const model = entry.editor.getModel();
      if (model) {
        monaco.editor.setModelLanguage(model, language);
        return true;
      }
      return false;
    } catch (e) {
      console.error('[monacoInterop] setLanguage failed:', e);
      return false;
    }
  },

  // Get current options (useful for debugging)
  getOptions: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return null;
    try {
      // Return a subset of commonly used options
      const opts = entry.editor.getOptions();
      return {
        fontSize: opts.get(37), // monaco.editor.EditorOption.fontSize = 37
        fontFamily: opts.get(36), // monaco.editor.EditorOption.fontFamily = 36
        lineNumbers: opts.get(51), // monaco.editor.EditorOption.lineNumbers = 51
        wordWrap: opts.get(112), // monaco.editor.EditorOption.wordWrap = 112
      };
    } catch (e) {
      console.error('[monacoInterop] getOptions failed:', e);
      return null;
    }
  },

  // Execute a Monaco editor command
  executeCommand: function(elementId, commandId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn("[monacoInterop] executeCommand: editor not found for", elementId);
      return false;
    }
    try {
      console.log("[monacoInterop] executing command:", commandId);
      entry.editor.trigger("keyboard", commandId, undefined);
      return true;
    } catch (e) {
      console.error("[monacoInterop] executeCommand failed:", commandId, e);
      return false;
    }
  }
};
