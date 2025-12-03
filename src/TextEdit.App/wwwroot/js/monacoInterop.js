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

    // F7 - manual spell check trigger
    try {
      editor.addCommand(
        monaco.KeyCode.KeyF7,
        () => {
          document.dispatchEvent(new CustomEvent('blazor-spell-check'));
        }
      );
    } catch (e) { /* ignore */ }

    // Save editor instance for later
    window.textEditMonaco.editors[elementId] = { editor, changeListener };
    return true;
  },

  attachContentChangeListener: function(elementId, dotNetRef) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) {
      console.warn('[attachContentChangeListener] No editor found for:', elementId);
      return false;
    }
    if (entry.contentChangeAttached) return true;
    try {
      const listener = entry.editor.onDidChangeModelContent(() => {
        dotNetRef.invokeMethodAsync('OnEditorContentChanged', entry.editor.getValue()).catch(e => console.error('[attachContentChangeListener] callback error', e));
      });
      entry.contentChangeListener = listener;
      entry.contentChangeAttached = true;
      return true;
    } catch (e) {
      console.error('[attachContentChangeListener] Error attaching listener for', elementId, e);
      return false;
    }
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
    entry.editor.dispose();
    delete window.textEditMonaco.editors[elementId];
  },

  executeCommand: function(elementId, commandId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) {
      console.warn('[executeCommand] No editor found for:', elementId);
      return;
    }
    
    try {
      entry.editor.trigger('keyboard', commandId, {});
      console.log('[executeCommand] Executed:', commandId);
    } catch (e) {
      console.error('[executeCommand] Error executing command:', commandId, e);
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
    if (!entry) {
      console.warn('[setSpellCheckDecorations] No editor found for:', elementId);
      return;
    }

    try {
      // Transform decoration data into Monaco decoration objects
      const decorations = decorationData.map(d => ({
        range: new monaco.Range(
          (d.range.startLineNumber !== undefined ? d.range.startLineNumber : d.range.StartLineNumber),
          (d.range.startColumn !== undefined ? d.range.startColumn : d.range.StartColumn),
          (d.range.endLineNumber !== undefined ? d.range.endLineNumber : d.range.EndLineNumber),
          (d.range.endColumn !== undefined ? d.range.endColumn : d.range.EndColumn)
        ),
          options: {
          isWholeLine: (d.options.isWholeLine !== undefined ? d.options.isWholeLine : d.options.IsWholeLine) || false,
          className: (d.options.className !== undefined ? d.options.className : d.options.ClassName) || 'spell-check-error',
          glyphMarginClassName: d.options.glyphMarginClassName || d.options.GlyphMarginClassName,
          glyphMarginHoverMessage: d.options.glyphMarginHoverMessage || d.options.GlyphMarginHoverMessage,
          inlineClassName: d.options.inlineClassName || d.options.InlineClassName,
          inlineClassNameAffectsLetterSpacing: (d.options.inlineClassNameAffectsLetterSpacing !== undefined ? d.options.inlineClassNameAffectsLetterSpacing : d.options.InlineClassNameAffectsLetterSpacing) || false,
          beforeContentClassName: d.options.beforeContentClassName || d.options.BeforeContentClassName,
          afterContentClassName: d.options.afterContentClassName || d.options.AfterContentClassName,
          // Store suggestions in a custom property for context menu access
          suggestions: d.options.suggestions || d.options.Suggestions || [],
          message: d.options.message || d.options.Message || ''
        }
      }));

      // Apply decorations to editor
      // Use decorationCollectionId 'spell-check' to manage all spell check decorations together
      if (!entry.spellCheckDecorationsId) {
        entry.spellCheckDecorationsId = [];
      }

      // Clear previous decorations and apply new ones
      entry.spellCheckDecorationsId = entry.editor.deltaDecorations(
        entry.spellCheckDecorationsId,
        decorations
      );

      console.log('[setSpellCheckDecorations] Applied', decorations.length, 'decorations');
    } catch (e) {
      console.error('[setSpellCheckDecorations] Error setting decorations:', e);
    }
  },

  updateSpellCheckDecorations: function(elementId, decorationData) {
    // Backwards compatibility alias
    return window.textEditMonaco.setSpellCheckDecorations(elementId, decorationData);
  },

  /**
   * Clears all spell check decorations from the editor.
   * @param {string} elementId - Editor container ID
   */
  clearSpellCheckDecorations: function(elementId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) {
      console.warn('[clearSpellCheckDecorations] No editor found for:', elementId);
      return;
    }

    try {
      if (entry.spellCheckDecorationsId && entry.spellCheckDecorationsId.length > 0) {
        entry.spellCheckDecorationsId = entry.editor.deltaDecorations(
          entry.spellCheckDecorationsId,
          []
        );
      }
      console.log('[clearSpellCheckDecorations] Cleared all spell check decorations');
    } catch (e) {
      console.error('[clearSpellCheckDecorations] Error clearing decorations:', e);
    }
  },

  /**
   * Gets the spell check suggestions for a specific decoration.
   * Used for context menu display.
   * @param {string} elementId - Editor container ID
   * @param {number} decorationId - Decoration ID from deltaDecorations
   * @returns {Array} Array of suggestion objects
   */
  getSpellCheckSuggestionsForDecoration: function(elementId, decorationId) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) {
      console.warn('[getSpellCheckSuggestionsForDecoration] No editor found for:', elementId);
      return [];
    }

    try {
      // Get all decorations from the editor
      const allDecorations = entry.editor.getLineDecorations(1); // Get all decorations
      
      // Find the decoration with matching ID
      const decoration = allDecorations.find(d => d.id === decorationId);
      if (decoration && decoration.options && decoration.options.suggestions) {
        return decoration.options.suggestions;
      }
      
      return [];
    } catch (e) {
      console.error('[getSpellCheckSuggestionsForDecoration] Error getting suggestions:', e);
      return [];
    }
  },

  /**
   * Replaces misspelled word at position with suggestion.
   * Used when user clicks on a suggestion in the context menu.
   * @param {string} elementId - Editor container ID
   * @param {object} position - Position object with line and column
   * @param {string} replacement - Replacement text
   * @param {number} wordLength - Length of word to replace
   */
  replaceSpellingError: function(elementId, position, replacement, wordLength) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) {
      console.warn('[replaceSpellingError] No editor found for:', elementId);
      return;
    }

    try {
      const model = entry.editor.getModel();
      if (!model) return;

      // Create range for the word to replace
      const startPos = new monaco.Position(position.line, position.column);
      const endPos = new monaco.Position(position.line, position.column + wordLength);
      const range = new monaco.Range(
        startPos.lineNumber,
        startPos.column,
        endPos.lineNumber,
        endPos.column
      );

      // Execute replace operation
      model.pushEditOperations(
        [],
        [{
          range: range,
          text: replacement,
          forceMoveMarkers: true
        }],
        () => []
      );

      console.log('[replaceSpellingError] Replaced with:', replacement);
    } catch (e) {
      console.error('[replaceSpellingError] Error replacing text:', e);
    }
  }
};