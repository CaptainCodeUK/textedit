// Simple theme helper for setting data-theme on <html>
window.theme = {
  set: function(mode) {
    try {
      var m = (mode || 'light').toString().toLowerCase();
      document.documentElement.setAttribute('data-theme', m);
      console.log('[theme] data-theme set to', m);
    } catch (e) {
      console.warn('[theme] failed to set data-theme', e);
    }
  }
};
