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

    // Initialize global focus management
    initialize: function(editorId) {
        // Prevent focus loss when clicking on non-interactive areas
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
