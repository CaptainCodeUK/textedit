// Focus management for the text editor
// Global guard/state to avoid duplicate initialization and handlers
window.__editorFocusState = window.__editorFocusState || { initialized: {}, globalKeydown: {}, globalMousedown: {} };
window.editorFocus = {
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
        if (window.__editorFocusState.initialized[editorId]) {
            return;
        }
        window.__editorFocusState.initialized[editorId] = true;
        // Prevent focus loss when clicking on non-interactive areas
        if (!window.__editorFocusState.globalMousedown[editorId]) {
            document.addEventListener('mousedown', function(e) {
            const target = e.target;
            const editor = document.getElementById(editorId);
            
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
                    editor.focus();
                }, 0);
            }
        });
        window.__editorFocusState.globalMousedown[editorId] = true;
        }

        // Helper: insert a tab character at current caret and update binding
        function insertTab(editor) {
            const start = editor.selectionStart ?? 0;
            const end = editor.selectionEnd ?? start;
            const value = editor.value ?? '';
            editor.value = value.substring(0, start) + '\t' + value.substring(end);
            const newPos = start + 1;
            editor.setSelectionRange(newPos, newPos);
            editor.dispatchEvent(new Event('input', { bubbles: true }));
        }

        // Insert TAB characters in textarea instead of moving focus
        function attachTabHandlerTo(editor) {
            if (!editor) {
                return;
            }
            if (editor.dataset && editor.dataset.tabHandlerAttached === 'true') {
                // Already attached; skip
                return;
            }
            if (editor._tabHandlerAttached) return; // legacy flag safeguard
            editor._tabHandlerAttached = true;
            if (editor.dataset) editor.dataset.tabHandlerAttached = 'true';
            editor.addEventListener('keydown', function(e) {
                if (e.key === 'Tab') {
                    e.preventDefault();
                    e.stopPropagation();
                    // Insert a single tab character, replacing any selected text
                    insertTab(editor);
                }
            }, true); // capture phase to win over other handlers
        }

        // Global capture-phase safeguard: handle Tab when active element is the editor
        if (!window.__editorFocusState.globalKeydown[editorId]) {
            document.addEventListener('keydown', function(e) {
                if (e.key !== 'Tab') return;
                const active = document.activeElement;
                if (!active) return;
                if (active.id === editorId && active.tagName === 'TEXTAREA') {
                    // If element-level handler is attached, let it handle
                    const handledByElement = (active.dataset && active.dataset.tabHandlerAttached === 'true') || active._tabHandlerAttached;
                    if (handledByElement) {
                        return;
                    }
                    e.preventDefault();
                    e.stopPropagation();
                    insertTab(active);
                }
            }, true);
            window.__editorFocusState.globalKeydown[editorId] = true;
        }

        // Initial attach if editor already exists
        const initialEditor = document.getElementById(editorId);
        attachTabHandlerTo(initialEditor);

        // Observe DOM changes to (re)attach when Blazor re-renders the editor
        const observer = new MutationObserver(function(mutations) {
            const editor = document.getElementById(editorId);
            if (editor) {
                attachTabHandlerTo(editor);
            }
        });
        observer.observe(document.body, { childList: true, subtree: true });
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
