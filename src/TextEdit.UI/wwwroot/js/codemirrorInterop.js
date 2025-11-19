window.textEditCodeMirror = window.textEditCodeMirror || {
  editors: {},

  // Tries to dynamically import CodeMirror 6 modules (ESM) using esm.sh. Falls back
  // to CodeMirror 5 from CDN if the ESM import fails.
  // baseUrl is used for ESM provider (esm.sh) by default. If forceFallback is true
  // the function will skip ESM and only load the legacy CodeMirror 5 UMD assets.
  loadCodeMirror: function(baseUrl, forceFallback) {
    // Use the modern ESM provider by default and attempt CM6 first
    baseUrl = baseUrl || 'https://esm.sh';
    return new Promise(async (resolve, reject) => {
      if (window.EditorView) { // CM6 already loaded
        resolve();
        return;
      }
      try {
  // improve stability. If this fails due to CSP or unavailable CDN, we
  // fall back to CM5 like old code did.
        const [{EditorState}, {EditorView}, {basicSetup}, {markdown}] = await Promise.all([
          import(baseUrl + '/@codemirror/state@0.19.3'),
          import(baseUrl + '/@codemirror/view@0.19.45'),
          import(baseUrl + '/@codemirror/basic-setup@0.19.1'),
          import(baseUrl + '/@codemirror/lang-markdown@0.19.4')
        ]);

  // Whitelisted exports
        window.textEditCodeMirror._cm6 = {
          EditorState: EditorState.EditorState || EditorState,
          EditorView: EditorView.EditorView || EditorView,
          basicSetup: basicSetup.basicSetup || basicSetup,
          markdown: markdown.markdown || markdown
        };

        resolve();
      } catch (e) {
  // Some CDNs/packagers may return separate copies of dependencies which
        // breaks `instanceof` checks (unrecognized extension). Try a bundled
        // variant of the package to see if that fixes the issue.
        try {
          // First, try a local vendor bundle (esbuild) if present. This bundle
          // reduces the risk of multiple @codemirror/* instances being loaded.
          try {
            const localBundle = await import('/_content/TextEdit.UI/lib/codemirror/codemirror-bundle.js');
            // If the bundle exported the CM6 object we can pick it up from the module
            // or keep the window global it may have set during evaluation.
            const exported = localBundle && (localBundle.default || localBundle.cm6 || localBundle);
            if (exported && (exported.EditorState || exported.EditorView)) {
              window.textEditCodeMirror._cm6 = exported;
              resolve();
              return;
            }
            if (window.textEditCodeMirror && window.textEditCodeMirror._cm6) {
              resolve();
              return;
            }
            // If the local bundle told us it's the vendor, prefer it and avoid
            // any further dynamic imports which might introduce duplicate
            // module instances.
            if (window.__codemirror6_vendor_loaded && window.textEditCodeMirror && window.textEditCodeMirror._cm6) {
              console.info('Using local CodeMirror 6 vendor bundle (marked by bundle).');
              resolve();
              return;
            }
            // Otherwise, fall through to the remote import below.
          } catch (localErr) {
            // Ignore: local bundle may not exist (not built), continue to ESM provider.
          }
          // Try a fully-bundled import for all major packages so they share a single
          // @codemirror/* instance and avoid `instanceof` conflicts.
          const bundle = await Promise.all([
            import(baseUrl + '/@codemirror/state@0.19.3?bundle'),
            import(baseUrl + '/@codemirror/view@0.19.45?bundle'),
            import(baseUrl + '/@codemirror/basic-setup@0.19.1?bundle'),
            import(baseUrl + '/@codemirror/lang-markdown@0.19.4?bundle')
          ]);
          // If successful, update cm6 to the bundled exports
          window.textEditCodeMirror._cm6 = {
            EditorState: bundle[0].EditorState || bundle[0].state || bundle[0].default || null,
            EditorView: bundle[1].EditorView || bundle[1].view || bundle[1].default || null,
            basicSetup: bundle[2].basicSetup || bundle[2].default || bundle[2],
            markdown: bundle[3].markdown || bundle[3].default || bundle[3]
          };
          resolve();
          return;
        } catch (err2) {
          // Bundled import failed - fall back to legacy CM5
  }
        // Fall back to CodeMirror 5 (UMD) path if ESM import fails
        try {
          const css = document.createElement('link');
          css.rel = 'stylesheet';
          css.href = 'https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.65.13/codemirror.min.css';
          document.head.appendChild(css);

          const script = document.createElement('script');
          script.src = 'https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.65.13/codemirror.min.js';
          script.onload = () => {
            const mdScript = document.createElement('script');
            mdScript.src = 'https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.65.13/mode/markdown/markdown.min.js';
            mdScript.onload = async () => {
              // After loading, try to ensure CodeMirror global is present
              try { await window.textEditCodeMirror.waitForGlobal('CodeMirror', 2000); } catch (e) { /*ignore*/ }
              resolve();
            };
            mdScript.onerror = () => resolve();
            document.head.appendChild(mdScript);
          };
          script.onerror = reject;
          document.head.appendChild(script);
        } catch (err) {
          // If all fails, reject to allow the caller to handle gracefully
          reject(err);
        }
      }
  });
  },

  // Wait for a named global to be available on window for up to timeoutMs milliseconds.
  waitForGlobal: function(name, timeoutMs) {
    return new Promise((resolve) => {
      if (window[name]) { resolve(true); return; }
      const start = Date.now();
      const interval = setInterval(() => {
        if (window[name]) { clearInterval(interval); resolve(true); return; }
        if (Date.now() - start > (timeoutMs || 3000)) { clearInterval(interval); resolve(false); }
      }, 50);
    });
  },

  createEditor: async function(elementId, dotNetRef, options) {
    options = options || {};
    const el = document.getElementById(elementId);
    if (!el) throw new Error('Element not found: ' + elementId);

    // If we loaded CM6 modules set by loadCodeMirror, use those
  if (window.textEditCodeMirror._cm6) {
      try {
  // ignore forceFallback here - it's only relevant to loadCodeMirror
        const { EditorState, EditorView, basicSetup, markdown } = window.textEditCodeMirror._cm6;
        let updateListener = null;
        try {
          if (EditorView.updateListener && typeof EditorView.updateListener.of === 'function') {
            updateListener = EditorView.updateListener.of(update => {
              if (update.docChanged) {
                try { dotNetRef.invokeMethodAsync('OnEditorContentChanged', update.state.doc.toString()); } catch (e) { }
              }
            });
          }
        } catch (e) { /* ignore */ }

        let state;
        try {
          state = EditorState.create({
            doc: options.value || '',
            extensions: [basicSetup || [], (function() {
            try {
              if (typeof markdown === 'function') return markdown();
              if (markdown && typeof markdown.markdown === 'function') return markdown.markdown();
              if (markdown && typeof markdown.default === 'function') return markdown.default();
            } catch (e) { }
            return null;
          })(), updateListener].filter(Boolean)
          });
        } catch (createEx) {
          // CodeMirror can throw "Unrecognized extension" when multiple copies of
          // @codemirror/state are present. Try a sanitized extension set with only
          // the minimal update listener so the editor still works.
          console.warn('CodeMirror6 EditorState.create failed; retrying with minimal extensions', createEx);
          try {
            state = EditorState.create({ doc: options.value || '', extensions: [updateListener].filter(Boolean) });
          } catch (sanitizedEx) {
            console.warn('CodeMirror6 sanitized create failed; will fallback to CM5', sanitizedEx);
            try { await window.textEditCodeMirror.loadCodeMirror(undefined, true); } catch (ignore) {}
            throw sanitizedEx || createEx;
          }
        }

        // Clear parent node to avoid duplicate content, then mount view.
        el.innerHTML = '';
        const view = new EditorView({ state, parent: el });

        // Ensure the editor uses the container height and is visible and show caret.
        try {
          view.dom.style.height = '100%';
          view.dom.style.boxSizing = 'border-box';
          // quick CSS fallback for CM6 when bundler doesn't provide core CSS
          const head = document.head;
          if (!document.getElementById('codemirror-basic-style')) {
            const style = document.createElement('style');
            style.id = 'codemirror-basic-style';
            style.innerHTML = `
              .cm-editor { height: 100%; box-sizing: border-box; }
              .cm-editor .cm-scroller { overflow: auto; padding: 8px; min-height: 120px; }
              .cm-editor .cm-content { white-space: pre-wrap; }
              .cm-editor .cm-line { white-space: pre-wrap; }
            `;
            head.appendChild(style);
          }
          // Improve caret visibility — CodeMirror 6 uses .cm-cursor.
          if (!document.getElementById('codemirror-caret-style')) {
            const caretStyle = document.createElement('style');
            caretStyle.id = 'codemirror-caret-style';
            caretStyle.innerHTML = `
              .cm-editor .cm-cursor { border-left: 1px solid var(--fg, #000); }
              .cm-editor .cm-selectionBackground { background: rgba(0,0,0,0.08); }
              .cm-editor .cm-activeLine { background: rgba(0,0,0,0.02); }
              .cm-editor { color: var(--fg, #000); font-family: var(--monospace-font, Menlo, Monaco, Consolas, 'Courier New', monospace); }
            `;
            head.appendChild(caretStyle);
          }

          try { view.focus(); } catch (e) { /* ignore focus failures */ }
          } catch (x) {
            console.warn('CodeMirror6 create failed during style/setup - will fallback', x);
            try {
              await window.textEditCodeMirror.loadCodeMirror(undefined, true);
            } catch (inner) { /* ignore */ }
            throw x; // bubble to outer catch so CM5 will be tried
          }

        window.textEditCodeMirror.editors[elementId] = { cm6: true, view };
        return true;
      } catch (x) {
        console.warn('CodeMirror6 create failed - falling back to CM5', x);
        try {
          // If CM6 fails, pre-load the legacy CM5 assets to give the fallback a chance
          await window.textEditCodeMirror.loadCodeMirror(undefined, true);
        } catch (ignoreLoad) {
          // Ignore failures here and try the fallback below
        }
        // proceed to try CM5
      }
    }

    // fallback to CodeMirror 5
    try {
      if (typeof CodeMirror === 'undefined' || (CodeMirror && typeof CodeMirror.fromTextArea !== 'function' && typeof CodeMirror.getValue !== 'function')) {
        console.warn('CodeMirror fallback unavailable - global not defined or missing functions');
        // if missing/partially loaded, attempt internal fallback editor
        return window.textEditCodeMirror.createFallbackEditor(elementId, dotNetRef, options);
      }
      const editor = CodeMirror(el, {
        value: options.value || '',
        mode: options.mode || 'markdown',
        lineNumbers: true,
        lineWrapping: true,
      });

      const changeHandler = function() {
        try {
          dotNetRef.invokeMethodAsync('OnEditorContentChanged', editor.getValue());
        } catch (e) { /* ignore */ }
      };
      editor.on('change', changeHandler);

      window.textEditCodeMirror.editors[elementId] = { editor, changeHandler };
      return true;
    } catch (e) {
      console.warn('CodeMirror create failed', e);
      // If instantiation fails for whatever reason, provide a simple textarea fallback
      try {
        return window.textEditCodeMirror.createFallbackEditor(elementId, dotNetRef, options);
      } catch (inner) {
        return false;
      }
    }
  },

  // Internal fallback when CodeMirror global is not available. Creates a textarea and mimics
  // a minimal subset of the CodeMirror API used by the shell (getValue, setValue, on, off, focus, toTextArea).
  createFallbackEditor: function(elementId, dotNetRef, options) {
    const el = document.getElementById(elementId);
    if (!el) throw new Error('Element not found: ' + elementId);
  const ta = document.createElement('textarea');
  // mark this element so tests can detect the internal fallback
  ta.className = 'codemirror-fallback-editor';
  ta.setAttribute('data-editor-fallback', 'true');
    ta.style.width = '100%';
    ta.style.height = '260px';
    ta.style.boxSizing = 'border-box';
    ta.style.overflow = 'auto';
    ta.value = options?.value || '';
    el.appendChild(ta);

    const onChange = () => {
      try { dotNetRef.invokeMethodAsync('OnEditorContentChanged', ta.value); } catch (e) { /* ignore */ }
    };
    ta.addEventListener('input', onChange);

    const editor = {
      getValue: () => ta.value,
      setValue: (v) => { ta.value = v; },
      focus: () => { try { ta.focus(); } catch (e) { } },
      on: (evt, cb) => { if (evt === 'change') ta.addEventListener('input', cb); },
      off: (evt, cb) => { if (evt === 'change') ta.removeEventListener('input', cb); },
      toTextArea: () => { try { ta.removeEventListener('input', onChange); } catch (e) { } if (ta.parentNode) ta.parentNode.removeChild(ta); }
    };

    window.textEditCodeMirror.editors[elementId] = { editor, changeHandler: onChange };
    return true;
  },

  getValue: function(elementId) {
    const entry = window.textEditCodeMirror.editors[elementId];
    if (!entry) return null;
    if (entry.cm6 && entry.view) {
      try { return entry.view.state.doc.toString(); } catch (e) { return null; }
    }
    return entry.editor ? entry.editor.getValue() : null;
  },

  setValue: function(elementId, value) {
    const entry = window.textEditCodeMirror.editors[elementId];
    if (!entry) return;
    if (entry.cm6 && entry.view) {
      try {
        const len = entry.view.state.doc.length;
        entry.view.dispatch({ changes: { from: 0, to: len, insert: value } });
      } catch (e) { }
    } else if (entry.editor) {
      entry.editor.setValue(value);
    }
  },

  disposeEditor: function(elementId) {
    const entry = window.textEditCodeMirror.editors[elementId];
    if (!entry) return;
    try {
      if (entry.cm6 && entry.view) {
        entry.view.destroy();
      } else {
        entry.editor.off('change', entry.changeHandler);
        entry.editor.toTextArea?.();
      }
    } catch (e) { }
    delete window.textEditCodeMirror.editors[elementId];
  }
};
