// portal.js - small helper to move modal overlays to document.body so fixed positioning works
window.textEditPortal = {
  _placeholders: {},
  _observers: {},
  attach: function (selectorOrElement) {
    var el = null;
    if (!selectorOrElement) return;
    // If element reference passed from Blazor, it will be a DOM element object
    if (selectorOrElement instanceof Element) {
      el = selectorOrElement;
    } else if (selectorOrElement && typeof selectorOrElement === 'object' && selectorOrElement.id) {
      // Blazor ElementReference from server-side may be proxied as an object with an id property
      el = document.getElementById(selectorOrElement.id) || document.querySelector(selectorOrElement.id);
      if (!el) {
        // if id did not match, try stringifying to selector
        try { el = document.querySelector(selectorOrElement.toString()); } catch (err) { /* ignore */ }
      }
    } else {
      try { el = document.querySelector(selectorOrElement); } catch (err) { el = null; }
    }
    if (!el) {
      console.log('textEditPortal: attach - could not resolve element', selectorOrElement);
      return false;
    }

  // If already moved, nothing to do
  if (el.__isPortalAttached) return true;

    var key = null;
    if (selectorOrElement instanceof Element) {
      // If an element was passed, store the placeholder directly on the element
      key = '__portalElement_' + Math.random().toString(36).slice(2);
    } else {
      key = selectorOrElement;
    }

    var placeholder = document.createComment('portal-placeholder:' + key);
    el.parentNode.insertBefore(placeholder, el);
  document.body.appendChild(el);
    el.__isPortalAttached = true;
    // store placeholder mapping
    if (typeof selectorOrElement === 'string') this._placeholders[selectorOrElement] = placeholder;
    else el.__portalPlaceholder = placeholder;
  console.log("textEditPortal: attached element to body", el);
    // Create a MutationObserver to detect if Blazor or other code replaces the element
    try {
      var observerKey = key || (selectorOrElement && selectorOrElement.id ? selectorOrElement.id : null);
      if (observerKey && !this._observers[observerKey]) {
        var self = this;
        var obs = new MutationObserver(function (mutations) {
          // If our element is no longer attached to body, attempt reattach
          var currentEl = null;
          if (typeof selectorOrElement === 'string') {
            try { currentEl = document.querySelector(selectorOrElement); } catch (e) { currentEl = null; }
          } else if (selectorOrElement && selectorOrElement.id) {
            currentEl = document.getElementById(selectorOrElement.id) || document.querySelector(selectorOrElement.id);
          } else {
            currentEl = el;
          }
          if (!currentEl) return;
          if (currentEl.parentNode !== document.body && !currentEl.__isPortalAttached) {
            // try reattaching (will also update placeholder if needed)
            try { self.attach(selectorOrElement); } catch (err) { /* ignore */ }
          }
        });
        obs.observe(document.body, { childList: true, subtree: true });
        this._observers[observerKey] = obs;
      }
    } catch (err) {
      /* ignore */
    }
    return true;
  },
  isAttached: function (selectorOrElement) {
    var el = null;
    if (!selectorOrElement) return false;
    if (selectorOrElement instanceof Element) {
      el = selectorOrElement;
    } else if (selectorOrElement && typeof selectorOrElement === 'object' && selectorOrElement.id) {
      el = document.getElementById(selectorOrElement.id) || document.querySelector(selectorOrElement.id);
    } else {
      try { el = document.querySelector(selectorOrElement); } catch (err) { el = null; }
    }
    if (!el) return false;
    // Element is attached if its parent is document.body or we flagged it as portal attached
    return el.__isPortalAttached === true || el.parentNode === document.body;
  },
  detach: function (selectorOrElement) {
  var placeholder = null;
    var el = null;
    if (selectorOrElement instanceof Element) {
      el = selectorOrElement;
      var key = null;
      for (var sel in this._placeholders) {
        if (this._placeholders[sel] && this._placeholders[sel].nextSibling === el) { key = sel; break; }
      }
      if (key) placeholder = this._placeholders[key];
      if (!placeholder && el.__portalPlaceholder) placeholder = el.__portalPlaceholder;
    } else {
      placeholder = this._placeholders[selectorOrElement];
      el = document.querySelector(selectorOrElement);
    }
    if (!placeholder || !el) {
      console.log('textEditPortal: detach - placeholder or element not found', selectorOrElement);
      return false;
    }

    placeholder.parentNode.insertBefore(el, placeholder);
    // remove and cleanup
    if (placeholder.parentNode) {
      placeholder.parentNode.removeChild(placeholder);
    }
    if (this._placeholders && this._placeholders[selectorOrElement]) delete this._placeholders[selectorOrElement];
    if (el && el.__portalPlaceholder) delete el.__portalPlaceholder;
    delete el.__isPortalAttached;
    // cleanup observer
    try {
      var observerKey = selectorOrElement;
      if (typeof selectorOrElement === 'object' && selectorOrElement.id) observerKey = selectorOrElement.id;
      if (observerKey && this._observers[observerKey]) {
        this._observers[observerKey].disconnect();
        delete this._observers[observerKey];
      }
    } catch (err) {}
  console.log("textEditPortal: detached element from body", el);
    return true;
  }
};
