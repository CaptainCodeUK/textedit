// focusTrap.js
window.textEditFocusTrap = {
  activeTraps: {},
  
  trap: function (dialogSelector, dotNetRef, methodName) {
    var dialog = document.querySelector(dialogSelector);
    if (!dialog) {
      return;
    }
    
    // Remove existing trap if present
    if (this.activeTraps[dialogSelector]) {
      document.removeEventListener('keydown', this.activeTraps[dialogSelector], true);
    }
    
    // Create the trap handler
    var trapHandler = function (e) {
      // Check if the event target is within this dialog or the dialog itself
      if (!dialog.contains(e.target) && e.target !== dialog) return;
      
      if (e.key === 'Escape' && dotNetRef && methodName) {
        e.preventDefault();
        e.stopPropagation();
        dotNetRef.invokeMethodAsync(methodName);
        return;
      }
      
      if (e.key === 'Tab') {
        var focusable = dialog.querySelectorAll('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
        if (!focusable.length) return;
        
        var first = focusable[0];
        var last = focusable[focusable.length - 1];
        
        if (e.shiftKey) {
          if (document.activeElement === first) {
            e.preventDefault();
            e.stopPropagation();
            last.focus();
          }
        } else {
          if (document.activeElement === last) {
            e.preventDefault();
            e.stopPropagation();
            first.focus();
          }
        }
      }
    };
    
    // Register trap with capture phase to intercept early
    document.addEventListener('keydown', trapHandler, true);
    this.activeTraps[dialogSelector] = trapHandler;
  },
  focusDialog: function (dialogSelector) {
    var dialog = document.querySelector(dialogSelector);
    if (!dialog) return;
    // If there are focusable controls, prefer the first one; else focus the dialog container so key events are captured
    var focusable = dialog.querySelectorAll('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
    if (focusable && focusable.length) {
      try { focusable[0].focus(); return; } catch (err) { /* ignore */ }
    }
    try {
      dialog.tabIndex = -1; dialog.focus();
    } catch (err) { }
  },
  focusElementById: function (id) {
    try {
      var el = document.getElementById(id);
      if (el && typeof el.focus === 'function') {
        el.focus();
      }
    } catch (e) { }
  },
  
  release: function (dialogSelector) {
    if (this.activeTraps[dialogSelector]) {
      document.removeEventListener('keydown', this.activeTraps[dialogSelector], true);
      delete this.activeTraps[dialogSelector];
    }
  }
};
