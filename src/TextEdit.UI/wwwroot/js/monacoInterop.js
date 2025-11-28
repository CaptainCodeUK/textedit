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
        // Dispatch selection-changed event after content changes to update toolbar
        document.dispatchEvent(new CustomEvent('monaco-selection-changed'));
        dotNetRef.invokeMethodAsync('OnEditorContentChanged', editor.getValue());
      } catch (e) { /* ignore */ }
    });

    // Listen for selection changes to update toolbar state (Cut/Copy buttons)
    const selectionListener = editor.onDidChangeCursorSelection(() => {
      try {
        console.log('[monacoInterop] Selection changed - dispatching monaco-selection-changed DOM event');
        const selection = editor.getSelection();
        console.log('[monacoInterop] Selection object:', selection);
        const event = new CustomEvent('monaco-selection-changed', { detail: { source: 'monacoInterop' } });
        document.dispatchEvent(event);
      } catch (e) {
        console.error('[monacoInterop] Error in selection listener:', e);
      }
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
    window.textEditMonaco.editors[elementId] = { editor, changeListener, selectionListener, keyDownListener };
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
    if (entry.selectionListener) entry.selectionListener.dispose();
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
  },

  // Get current caret position as offset in the document
  getCaretOffset: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return 0;
    try {
      const position = entry.editor.getPosition();
      if (!position) return 0;
      const model = entry.editor.getModel();
      if (!model) return 0;
      // Convert line/column to offset
      return model.getOffsetAt(position);
    } catch (e) {
      console.error('[monacoInterop] getCaretOffset failed:', e);
      return 0;
    }
  },

  // Get selected text range as {start, end} offsets
  getSelectionRange: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[monacoInterop] getSelectionRange: editor not found for', elementId);
      return { start: 0, end: 0 };
    }
    try {
      const selection = entry.editor.getSelection();
      if (!selection) {
        console.log('[monacoInterop] getSelectionRange: no selection');
        return { start: 0, end: 0 };
      }
      const model = entry.editor.getModel();
      if (!model) {
        console.warn('[monacoInterop] getSelectionRange: no model');
        return { start: 0, end: 0 };
      }
      // Convert positions to offsets
      const start = model.getOffsetAt(selection.getStartPosition());
      const end = model.getOffsetAt(selection.getEndPosition());
      console.log('[monacoInterop] getSelectionRange:', { start, end });
      return { start, end };
    } catch (e) {
      console.error('[monacoInterop] getSelectionRange failed:', e);
      return { start: 0, end: 0 };
    }
  },

  // Apply an edit operation to the editor with undo/redo integration
  // Uses Monaco's executeEdits() API so Ctrl+Z works naturally
  applyEdit: function(elementId, editData) {
    console.log('[monacoInterop.applyEdit] Called with elementId:', elementId, 'editData:', editData);
    
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[monacoInterop] applyEdit: editor not found for', elementId);
      console.warn('[monacoInterop] Available editors:', Object.keys(window.textEditMonaco.editors));
      return false;
    }
    try {
      const editor = entry.editor;
      const model = editor.getModel();
      if (!model) {
        console.warn('[monacoInterop] applyEdit: no model');
        return false;
      }

      console.log('[monacoInterop] applyEdit: applying edit with content length', editData.content.length);

      // Replace entire content at once - this creates a single undo point
      const fullRange = model.getFullModelRange();
      console.log('[monacoInterop] applyEdit: fullRange:', fullRange);
      
      editor.executeEdits('formatting', [
        {
          range: fullRange,
          text: editData.content
        }
      ]);
      
      console.log('[monacoInterop] applyEdit: executeEdits completed');

      // Set selection after applying edit
      if (editData.selectionStart !== undefined && editData.selectionEnd !== undefined) {
        const startPos = model.getPositionAt(editData.selectionStart);
        const endPos = model.getPositionAt(editData.selectionEnd);
        console.log('[monacoInterop] applyEdit: setting selection from', startPos, 'to', endPos);
        editor.setSelection(new monaco.Range(
          startPos.lineNumber, startPos.column,
          endPos.lineNumber, endPos.column
        ));
        editor.revealPositionInCenter(endPos);
      }

      return true;
    } catch (e) {
      console.error('[monacoInterop] applyEdit failed:', e);
      console.error('[monacoInterop] applyEdit stack:', e.stack);
      return false;
    }
  }
};
