// portal.js - small helper to move modal overlays to document.body so fixed positioning works
window.textEditPortal = {
  _placeholders: {},
  attach: function (selectorOrElement) {
    var el = null;
    if (!selectorOrElement) return;
    // If element reference passed from Blazor, it will be a DOM element object
    if (selectorOrElement instanceof Element) {
      el = selectorOrElement;
    } else {
      el = document.querySelector(selectorOrElement);
    }
    if (!el) return;

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
    return true;
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
    if (!placeholder || !el) return false;

    placeholder.parentNode.insertBefore(el, placeholder);
    // remove and cleanup
    if (placeholder.parentNode) {
      placeholder.parentNode.removeChild(placeholder);
    }
    if (this._placeholders && this._placeholders[selectorOrElement]) delete this._placeholders[selectorOrElement];
    if (el && el.__portalPlaceholder) delete el.__portalPlaceholder;
    delete el.__isPortalAttached;
    console.log("textEditPortal: detached element from body", el);
    return true;
  }
};
