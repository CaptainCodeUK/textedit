// Focus management for the text editor
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
        // Prevent focus loss when clicking on non-interactive areas
        document.addEventListener('mousedown', function(e) {
            const target = e.target;
            const editor = document.getElementById(editorId);
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
                    editor.focus();
                }, 0);
            }
        });
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
