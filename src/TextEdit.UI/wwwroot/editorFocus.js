// Focus management for the text editor
window.editorFocus = {
    _currentEditorId: 'main-editor-textarea',
    focus: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
            return true;
        }
        return false;
    },
    

    // Focus with a slight delay to ensure DOM updates are complete
    focusDelayed: function (elementId, delayMs = 10) {
        setTimeout(() => {
            const element = document.getElementById(elementId);
            if (element) {
                element.focus();
            }
        }, delayMs);
    },

    // Get caret position (line, column, and absolute index) in textarea
    getCaretPosition: function (elementId) {
        const element = document.getElementById(elementId);
        if (!element) {
            return { line: 1, column: 1, index: 0 };
        }

        const text = element.value;
        const caretPos = element.selectionStart;
        
        // Count lines up to caret
        let line = 1;
        let lastNewlinePos = -1;
        
        for (let i = 0; i < caretPos; i++) {
            if (text[i] === '\n') {
                line++;
                lastNewlinePos = i;
            }
        }
        
        // Column is position from last newline (or start)
        const column = caretPos - lastNewlinePos;
        
        return { line: line, column: column, index: caretPos };
    },

    // Set caret (selection) position by absolute index
    setCaretPosition: function (elementId, index) {
        const element = document.getElementById(elementId);
        if (!element) return;
        const pos = Math.max(0, Math.min(index || 0, element.value.length));
        element.setSelectionRange(pos, pos);
    },

    // Initialize global focus management
    initialize: function(editorId) {
        // Prevent focus loss when clicking on non-interactive areas
        if (editorId) this._currentEditorId = editorId;
        if (this._initialized) return;
        this._initialized = true;
        document.addEventListener('mousedown', (e) => {
            const target = e.target;
            const editor = document.getElementById(this._currentEditorId);
            // If the click is inside the Find/Replace bars, never interfere
            const inFindReplaceBar = !!(target.closest && (target.closest('.findbar') || target.closest('.findbar-container')));
            if (inFindReplaceBar) {
                return; // allow normal focus behavior for bar controls
            }
            
            // If clicking on something that's not the editor and not an interactive element
            if (editor && 
                target !== editor && 
                !editor.contains(target) &&
                target.tagName !== 'BUTTON' && 
                target.tagName !== 'INPUT' &&
                target.tagName !== 'TEXTAREA' &&
                target.tagName !== 'SELECT' &&
                target.tagName !== 'A' &&
                !target.closest('button') &&
                !target.closest('input') &&
                !target.closest('textarea') &&
                !target.closest('select') &&
                !target.closest('a')) {
                
                // Prevent the click from taking focus
                e.preventDefault();
                
                // Restore focus to editor
                setTimeout(() => {
                    try { editor.focus(); } catch (err) { /* ignore */ }
                }, 0);
            }
        });
    }

    // Change which editor element is considered the active editor.
    setActiveEditor: function (editorId) {
        if (!editorId) return false;
        this._currentEditorId = editorId;
        try {
            // Clear existing active classes
            document.querySelectorAll('.codemirror-editor-placeholder.active-editor, .alt-editor-placeholder.active-editor').forEach(e => e.classList.remove('active-editor'));
            // Add active class to the wrapper element containing the editor
            const elm = document.getElementById(editorId);
            if (elm) {
                const wrap = elm.closest('.codemirror-editor-placeholder') || elm.closest('.alt-editor-placeholder');
                if (wrap) wrap.classList.add('active-editor');
            }
        } catch (e) { }
        return true;
    },

    // Focus the current active editor. Try Monaco/CodeMirror focus methods
    focusActiveEditor: function () {
        try {
            // Monaco: textEditMonaco.editors['monaco-editor'].editor.focus()
            if (window.textEditMonaco && window.textEditMonaco.editors && window.textEditMonaco.editors['monaco-editor'] && window.textEditMonaco.editors['monaco-editor'].editor) {
                try { window.textEditMonaco.editors['monaco-editor'].editor.focus(); return true; } catch (e) { }
            }

            // CM6: textEditCodeMirror.editors['codemirror-editor'].view.focus()
            if (window.textEditCodeMirror && window.textEditCodeMirror.editors && window.textEditCodeMirror.editors['codemirror-editor']) {
                const entry = window.textEditCodeMirror.editors['codemirror-editor'];
                try { if (entry.view && entry.view.focus) { entry.view.focus(); return true; } } catch (e) { }
                try { if (entry.editor && entry.editor.focus) { entry.editor.focus(); return true; } } catch (e) { }
            }

            // Otherwise, fallback to focusing an HTML element with the id
            const el = document.getElementById(this._currentEditorId || 'main-editor-textarea');
            if (el) { el.focus(); return true; }
        } catch (e) { }
        return false;
    }
};

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        window.editorFocus.initialize('main-editor-textarea');
    });
} else {
    window.editorFocus.initialize('main-editor-textarea');
}
