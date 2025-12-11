window.textEditMonaco = window.textEditMonaco || {
  editors: {},
  pendingSpellCheckDecorations: {},
  
  // Test function to verify JS interop is working
  testFunction: function() {
    console.log('[monacoInterop] testFunction called');
    return { message: 'Hello from testFunction', timestamp: new Date().getTime() };
  },

  debugLog: function(obj) {
    try { console.debug('[monacoInterop] debugLog:', obj); } catch (e) { }
  },
  
  loadMonaco: (function() {
    let loadingPromise = null;

    return function(baseUrl) {
      console.log('[monacoInterop] loadMonaco called with baseUrl:', baseUrl);
      baseUrl = baseUrl || 'https://cdn.jsdelivr.net/npm/monaco-editor@0.38.0/min';

      // Prevent duplicate loading of the Monaco loader script
      if (document.getElementById('monaco-loader-script')) {
        console.log('[monacoInterop] Monaco loader script already loaded, resolving immediately');
        return Promise.resolve();
      }

      if (loadingPromise) {
        console.log('[monacoInterop] Monaco loader script is already loading, returning existing promise');
        return loadingPromise;
      }

      loadingPromise = new Promise((resolve, reject) => {
        if (window.monaco) {
          console.log('[monacoInterop] Monaco already loaded, resolving immediately');
          resolve();
          return;
        }

        console.log('[monacoInterop] Loading Monaco from', baseUrl + '/vs/loader.js');
        const script = document.createElement('script');
        script.id = 'monaco-loader-script';
        script.src = baseUrl + '/vs/loader.js';
        script.onload = () => {
          console.log('[monacoInterop] Loader script loaded, configuring require');
          require.config({ paths: { vs: baseUrl + '/vs' } });
          require(['vs/editor/editor.main'], () => {
            console.log('[monacoInterop] Monaco editor main loaded, resolving');
            resolve();
          }, reject);
        };
        script.onerror = (err) => {
          console.error('[monacoInterop] Failed to load loader.js:', err);
          reject(err);
        };
        document.head.appendChild(script);
      });

      return loadingPromise;
    };
  })(),

  createEditor: function(elementId, dotNetRef, options) {
    console.log('[monacoInterop] createEditor called for', elementId);
    options = options || {};
    // Ensure an entry exists early so other functions can detect 'pending' editors
    if (!window.textEditMonaco.editors[elementId]) {
      window.textEditMonaco.editors[elementId] = { editor: null };
    }

    // Prevent duplicate initialization (only if editor already created)
    if (window.textEditMonaco.editors[elementId] && window.textEditMonaco.editors[elementId].editor) {
      console.warn('[monacoInterop] Editor already exists for', elementId);
      return false;
    }

    const el = document.getElementById(elementId);
    if (!el) {
      console.error('[monacoInterop] Element not found:', elementId);
      throw new Error('Element not found: ' + elementId);
    }

    console.log('[monacoInterop] Creating editor with options:', options);

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
    
    // F7 - manual spell check trigger
    editor.addCommand(
      monaco.KeyCode.F7,
      () => {
        console.log('[monacoInterop] F7 triggered - dispatching blazor-spell-check');
        document.dispatchEvent(new CustomEvent('blazor-spell-check'));
      }
    );
    
    const keyDownListener = null;

  // Save editor instance for later
  window.textEditMonaco.editors[elementId] = { editor, changeListener, selectionListener, keyDownListener };
    // Flush any pending spell check decorations that were queued while editor initialization was pending
      try {
      const pending = window.textEditMonaco.pendingSpellCheckDecorations && window.textEditMonaco.pendingSpellCheckDecorations[elementId];
      if (pending) {
        const pendingIsArray = Array.isArray(pending);
        const pendingLength = pendingIsArray ? pending.length : 0;
        if (pendingIsArray && pendingLength === 0) {
          console.log('[createEditor] Applying queued clear for', elementId);
          // apply clear in a micro-task to let the editor settle
          setTimeout(() => {
            try { window.textEditMonaco.clearSpellCheckDecorations(elementId); } catch (e) { console.error('[createEditor] Error applying queued clear for pending decorations:', e); }
          }, 10);
        }
        else if (pendingIsArray) {
          console.log('[createEditor] Applying queued spell check decorations for', elementId, 'count:', pendingLength);
          setTimeout(() => {
            try { window.textEditMonaco.setSpellCheckDecorations(elementId, pending); } catch (e) { console.error('[createEditor] Error applying pending decorations:', e); }
          }, 10);
        }
        else {
          // Non-array payload; try to set decorations as-is and log accordingly
          console.log('[createEditor] Applying queued spell check decorations (unknown shape) for', elementId);
          setTimeout(() => {
            try { window.textEditMonaco.setSpellCheckDecorations(elementId, pending); } catch (e) { console.error('[createEditor] Error applying pending decorations (unknown shape):', e); }
          }, 10);
        }
        delete window.textEditMonaco.pendingSpellCheckDecorations[elementId];
      }
    } catch (e) { console.error('[createEditor] Error while flushing pending spell check decorations:', e); }
    return true;
  },

  /**
   * Ensures the content change listener is attached for the specified editor.
   * This is idempotent and safe to call if the listener is already attached.
   */
  attachContentChangeListener: function(elementId, dotNetRef) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[attachContentChangeListener] No editor found for:', elementId);
      return false;
    }

    if (entry.contentChangeAttached) {
      console.log('[attachContentChangeListener] Content change listener already attached for', elementId);
      return true;
    }

    try {
      const listener = entry.editor.onDidChangeModelContent(() => {
        try {
          dotNetRef.invokeMethodAsync('OnEditorContentChanged', entry.editor.getValue()).catch(e => console.error('[attachContentChangeListener] callback error', e));
        } catch (e) { /* ignore */ }
      });
      entry.contentChangeListener = listener;
      entry.contentChangeAttached = true;
      console.log('[attachContentChangeListener] Attached listener for', elementId);
      return true;
    } catch (e) {
      console.error('[attachContentChangeListener] Error attaching listener for', elementId, e);
      return false;
    }
  },

  getValue: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    try { return (entry && entry.editor && typeof entry.editor.getValue === 'function') ? entry.editor.getValue() : null; } catch (e) { console.error('[getValue] Error:', e); return null; }
  },

  setValue: function(elementId, value) {
    const entry = window.textEditMonaco.editors[elementId];
    try { if (entry && entry.editor && typeof entry.editor.setValue === 'function') entry.editor.setValue(value); }
    catch (e) { console.error('[setValue] Error setting value:', e); }
  },

  disposeEditor: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) return;
    try { if (entry.changeListener && typeof entry.changeListener.dispose === 'function') entry.changeListener.dispose(); } catch (e) { console.warn('[disposeEditor] Failed hashing changeListener', e); }
    try { if (entry.selectionListener && typeof entry.selectionListener.dispose === 'function') entry.selectionListener.dispose(); } catch (e) { console.warn('[disposeEditor] Failed disposing selectionListener', e); }
    try { if (entry.keyDownListener && typeof entry.keyDownListener.dispose === 'function') entry.keyDownListener.dispose(); } catch (e) { console.warn('[disposeEditor] Failed disposing keyDownListener', e); }
    try { if (entry.editor && typeof entry.editor.dispose === 'function') entry.editor.dispose(); } catch (e) { console.warn('[disposeEditor] Failed disposing editor', e); }
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

  /**
   * Get spell check decoration at current caret position (if any).
   * Returns an object { range: { startLineNumber, startColumn, endLineNumber, endColumn }, word, suggestions }
   */
  getSpellCheckDecorationAtCaret: function (elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return null;
    try {
      const pos = entry.editor.getPosition();
      if (!pos) return null;
      const model = entry.editor.getModel();
      if (!model) return null;
      const decorations = model.getDecorationsInRange(new monaco.Range(pos.lineNumber, pos.column, pos.lineNumber, pos.column));
      for (const dec of decorations) {
        const opts = dec.options || {};
        const className = opts.className || opts.inlineClassName || '';
        if (className.indexOf('spell-check-error') >= 0 || className.indexOf('spell-check') >= 0) {
          const range = dec.range;
          const text = model.getValueInRange(range);
            let suggestions = opts.suggestions || opts.Suggestions || opts.SUGGESTIONS || [];
            // Normalize suggestions to objects with a Word property if they are strings
            try {
              suggestions = suggestions.map(s => {
                try {
                  if (typeof s === 'string') return { Word: s, Confidence: 50, IsPrimary: false };
                  if (s && typeof s === 'object' && (s.Word || s.word)) {
                    return { Word: s.Word || s.word, Confidence: s.Confidence || s.confidence || 50, IsPrimary: s.IsPrimary || s.isPrimary || false };
                  }
                  return s;
                } catch (e) { return s; }
              });
            } catch (e) { /* ignore normalization errors */ }
          return {
            range: { startLineNumber: range.startLineNumber, startColumn: range.startColumn, endLineNumber: range.endLineNumber, endColumn: range.endColumn },
            word: text,
            suggestions: suggestions
          };
        }
      }
      return null;
    } catch (e) {
      console.error('[getSpellCheckDecorationAtCaret] Error:', e);
      return null;
    }
  },

  // Check if undo is available by querying the editor's command state
  canUndo: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[monacoInterop] canUndo: editor not found');
      return false;
    }
    try {
      const model = entry.editor.getModel();
      if (!model) return false;
      // Monaco tracks undo/redo through the model's version and edit stack
      // We can check if there are edits in the undo stack by attempting to get undo/redo stack info
      // Note: Monaco doesn't expose undo stack directly, but we can use onDidChangeModelContent
      // and track edits, or we can check if the model has version history
      
      // The model has a version counter that increases with each edit
      // We can track the base version to determine if undo is available
      const currentVersion = model.getVersionId();
      // Store initial version if not already stored
      if (!window.textEditMonaco._editorVersions) {
        window.textEditMonaco._editorVersions = {};
      }
      const key = 'undo_' + elementId;
      if (!window.textEditMonaco._editorVersions[key]) {
        window.textEditMonaco._editorVersions[key] = currentVersion;
      }
      // Can undo if current version is different from baseline
      return currentVersion !== window.textEditMonaco._editorVersions[key];
    } catch (e) {
      console.error('[monacoInterop] canUndo failed:', e);
      return false;
    }
  },

  // Check if redo is available
  canRedo: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[monacoInterop] canRedo: editor not found');
      return false;
    }
    try {
      // Monaco doesn't expose redo state directly either
      // We would need to track this through edit history
      // For now, return false as a safe default
      return false;
    } catch (e) {
      console.error('[monacoInterop] canRedo failed:', e);
      return false;
    }
  },

  // Listen for undo/redo stack changes and invoke a callback
  onUndoRedoStackChange: function(elementId, dotNetRef) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[monacoInterop] onUndoRedoStackChange: editor not found');
      return false;
    }
    try {
      const editor = entry.editor;
      const model = editor.getModel();
      if (!model) return false;
      
      // Track when content changes (which updates undo/redo state)
      const changeListener = model.onDidChangeContent(() => {
        try {
          // Dispatch event so Blazor can poll for undo/redo state
          document.dispatchEvent(new CustomEvent('monaco-undo-redo-changed', { 
            detail: { 
              elementId: elementId,
              versionId: model.getVersionId()
            } 
          }));
          
          // Optionally invoke .NET callback
          if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnUndoRedoStackChanged', model.getVersionId()).catch(e => console.error('Callback error:', e));
          }
        } catch (e) {
          console.error('[monacoInterop] onUndoRedoStackChange callback error:', e);
        }
      });
      
      // Store the listener for potential disposal
      if (!entry.undoRedoListener) {
        entry.undoRedoListener = changeListener;
      }
      return true;
    } catch (e) {
      console.error('[monacoInterop] onUndoRedoStackChange failed:', e);
      return false;
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
  },

  // Get the current undo/redo state - SIMPLE VERSION
  // Just returns what Monaco gives us directly without complex tracking
  // Returns { canUndo: boolean, canRedo: boolean, versionId: number }
  getUndoRedoState: function(elementId) {
    // For now, just return a constant to test if JS interop works at all
    window.___lastUndo = new Date().getTime();  // Track when this was called
    return { canUndo: true, canRedo: false, versionId: 42, hasSavedPoint: false };
  },

  // Mark the current version as a saved point for undo/redo tracking
  markSavePoint: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return false;
    
    try {
      const model = entry.editor.getModel();
      if (!model) return false;
      
      if (!window.textEditMonaco._undoRedoStates) {
        window.textEditMonaco._undoRedoStates = {};
      }
      
      const stateKey = 'state_' + elementId;
      window.textEditMonaco._undoRedoStates[stateKey] = {
        savedVersion: model.getVersionId(),
        hasSavedPoint: true
      };
      
      console.log('[monacoInterop] Marked save point at version', model.getVersionId());
      return true;
    } catch (e) {
      console.error('[monacoInterop] markSavePoint failed:', e);
      return false;
    }
  },

  /**
   * Sets spell check decorations on the editor.
   * Creates red wavy underlines for misspelled words.
   * @param {string} elementId - Editor container ID
   * @param {Array} decorationData - Array of decoration objects with range and options
   */
  setSpellCheckDecorations: function(elementId, decorationData) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[setSpellCheckDecorations] No editor found for (or not initialized yet):', elementId);
      // If no editor yet, queue decorations to be applied once the editor is created.
      // If decorationData is empty, this signals a clear; queue an explicit clear operation instead of an empty array
      try {
        const isArray = Array.isArray(decorationData);
        const length = isArray ? decorationData.length : 0;
        if (!isArray || length === 0) {
          // Queue a clear operation for this editor when it is created
          window.textEditMonaco.pendingSpellCheckDecorations[elementId] = [];
          console.log('[setSpellCheckDecorations] Queued clear for', elementId);
        } else {
          const clone = JSON.parse(JSON.stringify(decorationData));
          window.textEditMonaco.pendingSpellCheckDecorations[elementId] = clone;
          console.log('[setSpellCheckDecorations] Queued', clone.length, 'decorations for', elementId);
        }
      } catch (e) {
        // Fallback: store as-is
        window.textEditMonaco.pendingSpellCheckDecorations[elementId] = decorationData;
      }
      return;
    }

    try {
      // Transform decoration data into Monaco decoration objects
      const dataArray = Array.isArray(decorationData) ? decorationData : [];
      const validDecorationData = [];
      const invalidEntries = [];
      dataArray.forEach((d, idx) => {
        if (!d) { invalidEntries.push({ idx, reason: 'null or undefined', item: d }); return; }
        const r = d.range || d.Range || {};
        const startLine = (r.startLineNumber !== undefined ? r.startLineNumber : r.StartLineNumber);
        const startCol = (r.startColumn !== undefined ? r.startColumn : r.StartColumn);
        const endLine = (r.endLineNumber !== undefined ? r.endLineNumber : r.EndLineNumber);
        const endCol = (r.endColumn !== undefined ? r.endColumn : r.EndColumn);
  const reasons = [];
        if (!Number.isInteger(startLine)) reasons.push('startLine not integer:' + startLine);
        if (!Number.isInteger(startCol)) reasons.push('startCol not integer:' + startCol);
        if (!Number.isInteger(endLine)) reasons.push('endLine not integer:' + endLine);
  if (!Number.isInteger(endCol)) reasons.push('endCol not integer:' + endCol);
  if (Number.isInteger(startCol) && Number.isInteger(endCol) && endCol < startCol) reasons.push('endCol < startCol');
        if (reasons.length > 0) { invalidEntries.push({ idx, reasons, item: d }); return; }
        validDecorationData.push(d);
      });
      if (invalidEntries.length > 0) {
        // Build a readable report string to ensure the console displays it fully and helps debugging
        try {
          const report = invalidEntries.map(e => ({ idx: e.idx, reasons: e.reasons || e.reason, item: e.item }));
          console.warn('[setSpellCheckDecorations] Skipping invalid decoration entries:', JSON.stringify(report, null, 2));
        } catch (e) {
          console.warn('[setSpellCheckDecorations] Skipping invalid decoration entries (could not stringify)', invalidEntries);
        }
      }
      if (validDecorationData.length === 0) {
        // Nothing valid to apply; clear any previous decorations and return
        if (entry.spellCheckDecorationsId && entry.spellCheckDecorationsId.length > 0) {
          entry.spellCheckDecorationsId = entry.editor.deltaDecorations(entry.spellCheckDecorationsId, []);
        }
        console.log('[setSpellCheckDecorations] No valid decorations to apply, cleared previous ones');
        return;
      }

      const decorations = validDecorationData.map(d => {
        const r = d.range || d.Range || {};
        const opts = d.options || d.Options || {};
        return {
          range: new monaco.Range(
            (r.startLineNumber !== undefined ? r.startLineNumber : r.StartLineNumber),
            (r.startColumn !== undefined ? r.startColumn : r.StartColumn),
            (r.endLineNumber !== undefined ? r.endLineNumber : r.EndLineNumber),
            (r.endColumn !== undefined ? r.endColumn : r.EndColumn)
          ),
          options: {
            isWholeLine: (opts.isWholeLine !== undefined ? opts.isWholeLine : opts.IsWholeLine) || false,
            className: (opts.className !== undefined ? opts.className : opts.ClassName) || 'spell-check-error',
            glyphMarginClassName: opts.glyphMarginClassName || opts.GlyphMarginClassName,
            glyphMarginHoverMessage: opts.glyphMarginHoverMessage || opts.GlyphMarginHoverMessage,
            inlineClassName: opts.inlineClassName || opts.InlineClassName,
            inlineClassNameAffectsLetterSpacing: (opts.inlineClassNameAffectsLetterSpacing !== undefined ? opts.inlineClassNameAffectsLetterSpacing : opts.InlineClassNameAffectsLetterSpacing) || false,
            beforeContentClassName: opts.beforeContentClassName || opts.BeforeContentClassName,
            afterContentClassName: opts.afterContentClassName || opts.AfterContentClassName,
            suggestions: opts.suggestions || opts.Suggestions || [],
            message: opts.message || opts.Message || ''
          }
        };
      });

      // Apply decorations to editor
      if (!entry.spellCheckDecorationsId) {
        entry.spellCheckDecorationsId = [];
      }

      // Clear previous decorations and apply new ones
      entry.spellCheckDecorationsId = entry.editor.deltaDecorations(
        entry.spellCheckDecorationsId,
        decorations
      );

  console.log('[setSpellCheckDecorations] Applied', decorations.length, 'decorations');
  try { console.debug('[setSpellCheckDecorations] decorations:', JSON.stringify(decorations, null, 2)); } catch (e) { }
    } catch (e) {
      console.error('[setSpellCheckDecorations] Error setting decorations:', e);
    }
  },

  // Backwards compatibility: alias `updateSpellCheckDecorations` -> calls `setSpellCheckDecorations`
  updateSpellCheckDecorations: function(elementId, decorationData) {
    console.log('[updateSpellCheckDecorations] alias called');
    return window.textEditMonaco.setSpellCheckDecorations(elementId, decorationData);
  },

  /**
   * Clears all spell check decorations from the editor.
   * @param {string} elementId - Editor container ID
   */
  clearSpellCheckDecorations: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) {
      console.warn('[clearSpellCheckDecorations] No editor found for (or not initialized yet):', elementId);
      // Queue clear operation until editor available
      window.textEditMonaco.pendingSpellCheckDecorations[elementId] = [];
      console.log('[clearSpellCheckDecorations] Queued clear for', elementId);
      return;
    }

    try {
      if (entry.spellCheckDecorationsId && entry.spellCheckDecorationsId.length > 0) {
        entry.spellCheckDecorationsId = entry.editor.deltaDecorations(
          entry.spellCheckDecorationsId,
          []
        );
        console.log('[clearSpellCheckDecorations] Cleared all spell check decorations');
      }
    } catch (e) {
      console.error('[clearSpellCheckDecorations] Error clearing decorations:', e);
    }
  },

  /**
   * Move caret to the next spelling error (decoration) after current caret position.
   * Returns true if moved to a next error; false otherwise.
   */
  moveToNextSpellingError: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return false;
    try {
      const model = entry.editor.getModel();
      if (!model) return false;
      const decIds = entry.spellCheckDecorationsId || [];
      if (decIds.length === 0) return false;
      // Collect ranges
      const ranges = decIds.map(id => ({ id, range: model.getDecorationRange(id) })).filter(r => r.range);
      if (ranges.length === 0) return false;
      const pos = entry.editor.getPosition();
      if (!pos) return false;
      // Find next decoration whose start is after the current position
      const currentOffset = model.getOffsetAt(pos);
      let candidate = null;
      let candidateOffset = Number.MAX_SAFE_INTEGER;
      for (const r of ranges) {
        const startOffset = model.getOffsetAt(r.range.getStartPosition());
        if (startOffset > currentOffset && startOffset < candidateOffset) {
          candidate = r.range;
          candidateOffset = startOffset;
        }
      }
      // If none found, wrap to the first
      if (!candidate) candidate = ranges[0].range;
      entry.editor.setPosition(candidate.getStartPosition());
      entry.editor.revealRangeInCenter(candidate);
      return true;
    } catch (e) {
      console.error('[moveToNextSpellingError] Error:', e);
      return false;
    }
  },

  /**
   * Move caret to the previous spelling error (decoration) before current caret position.
   * Returns true if moved to a previous error; false otherwise.
   */
  moveToPrevSpellingError: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return false;
    try {
      const model = entry.editor.getModel();
      if (!model) return false;
      const decIds = entry.spellCheckDecorationsId || [];
      if (decIds.length === 0) return false;
      const ranges = decIds.map(id => ({ id, range: model.getDecorationRange(id) })).filter(r => r.range);
      if (ranges.length === 0) return false;
      const pos = entry.editor.getPosition();
      if (!pos) return false;
      const currentOffset = model.getOffsetAt(pos);
      let candidate = null;
      let candidateOffset = -1;
      for (const r of ranges) {
        const startOffset = model.getOffsetAt(r.range.getStartPosition());
        if (startOffset < currentOffset && startOffset > candidateOffset) {
          candidate = r.range;
          candidateOffset = startOffset;
        }
      }
      // If none found, wrap to the last
      if (!candidate) candidate = ranges[ranges.length - 1].range;
      entry.editor.setPosition(candidate.getStartPosition());
      entry.editor.revealRangeInCenter(candidate);
      return true;
    } catch (e) {
      console.error('[moveToPrevSpellingError] Error:', e);
      return false;
    }
  },

  /**
   * Sets the caret position in the editor at the given line/column.
   * @param {string} elementId
   * @param {number} line
   * @param {number} column
   */
  setCaretPositionAt: function(elementId, line, column) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry || !entry.editor) return false;
    try {
      const pos = new monaco.Position(line, column);
      entry.editor.setPosition(pos);
      entry.editor.revealPositionInCenter(pos);
      return true;
    } catch (e) {
      console.error('[setCaretPositionAt] Error:', e);
      return false;
    }
  },

  /**
   * Gets spell check suggestions for a specific decoration.
   * @param {string} elementId - Editor container ID
   * @param {string} decorationId - The decoration ID to get suggestions for
   * @returns {Array} Array of suggestion strings
   */
  getSpellCheckSuggestionsForDecoration: function(elementId, decorationId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) {
      console.warn('[getSpellCheckSuggestionsForDecoration] No editor found for:', elementId);
      return [];
    }

    try {
      // In Monaco, decorations don't have direct IDs we can query
      // Suggestions are stored in the decoration options when created
      // Return empty for now - suggestions should be retrieved through context menu
      return [];
    } catch (e) {
      console.error('[getSpellCheckSuggestionsForDecoration] Error:', e);
      return [];
    }
  },

  /**
   * Replaces a misspelled word with a suggestion.
   * @param {string} elementId - Editor container ID
   * @param {object} range - { startLineNumber, startColumn, endLineNumber, endColumn }
   * @param {string} replacement - The replacement text
   */
  replaceSpellingError: function(elementId, range, replacement) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) {
      console.warn('[replaceSpellingError] No editor found for:', elementId);
      return false;
    }

    try {
      const editor = entry.editor;
      const model = editor.getModel();
      if (!model) return false;

      // Create Monaco range from decoration range
      const monacoRange = new monaco.Range(
        range.startLineNumber,
        range.startColumn,
        range.endLineNumber,
        range.endColumn
      );

      // Replace the text
      editor.executeEdits('spellcheck', [
        {
          range: monacoRange,
          text: replacement
        }
      ]);

      console.log('[replaceSpellingError] Replaced text at', monacoRange);
      return true;
    } catch (e) {
      console.error('[replaceSpellingError] Error replacing spelling error:', e);
      return false;
    }
  }
};
