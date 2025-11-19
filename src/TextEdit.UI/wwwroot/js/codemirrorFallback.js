// Minimal fallback for CodeMirror when the UMD asset isn't available.
// This is a tiny shim that implements a subset of the CodeMirror 5 API we need:
// - constructor (container, options) -> editor instance
// - instance.getValue(), setValue(), focus(), on('change', cb), off('change', cb), toTextArea
(() => {
  if (window.CodeMirror) return; // don't override a real CodeMirror

  function createEditor(parent, options) {
    const ta = document.createElement('textarea');
    ta.style.width = '100%';
    ta.style.height = '100%';
    ta.style.boxSizing = 'border-box';
    ta.style.resize = 'none';
    ta.style.fontFamily = options && options.fontFamily ? options.fontFamily : 'monospace';
    ta.value = options && options.value ? options.value : '';

    parent.appendChild(ta);

    const listeners = { change: [] };
    const changeHandler = () => {
      listeners.change.forEach(cb => cb());
    };

    ta.addEventListener('input', changeHandler);

    return {
      getValue: function () { return ta.value; },
      setValue: function (v) { ta.value = v; },
      focus: function () { ta.focus(); },
      on: function (evt, cb) {
        if (!listeners[evt]) listeners[evt] = [];
        listeners[evt].push(cb);
      },
      off: function (evt, cb) {
        if (!listeners[evt]) return;
        const i = listeners[evt].indexOf(cb);
        if (i >= 0) listeners[evt].splice(i, 1);
      },
      toTextArea: function () { try { ta.removeEventListener('input', changeHandler); } catch (e) {} if (ta.parentNode) ta.parentNode.removeChild(ta); }
    };
  }

  window.CodeMirror = function (parent, options) {
    return createEditor(parent, options || {});
  };
})();
