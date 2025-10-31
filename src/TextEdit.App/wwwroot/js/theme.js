// Simple theme helper for setting data-theme on <html>
window.theme = {
  set: function(mode) {
    try {
      var m = (mode || 'light').toString().toLowerCase();
      document.documentElement.setAttribute('data-theme', m);
    } catch (e) {
      // Silently ignore failures to modify DOM theme attribute in production
    }
  }
};
