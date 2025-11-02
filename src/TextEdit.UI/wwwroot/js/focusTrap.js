// focusTrap.js
window.textEditFocusTrap = {
  trap: function (dialogSelector) {
    var dialog = document.querySelector(dialogSelector);
    if (!dialog) return;
    var focusable = dialog.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
    if (!focusable.length) return;
    var first = focusable[0];
    var last = focusable[focusable.length - 1];
    dialog.addEventListener('keydown', function (e) {
      if (e.key === 'Tab') {
        if (e.shiftKey) {
          if (document.activeElement === first) {
            e.preventDefault();
            last.focus();
          }
        } else {
          if (document.activeElement === last) {
            e.preventDefault();
            first.focus();
          }
        }
      }
      if (e.key === 'Escape') {
        // Let Blazor handle Escape
      }
    });
  }
};
