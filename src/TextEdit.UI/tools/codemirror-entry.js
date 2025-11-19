import * as State from '@codemirror/state';
import * as View from '@codemirror/view';
import { basicSetup } from '@codemirror/basic-setup';
import { markdown } from '@codemirror/lang-markdown';

// Attach to a global so the dynamic importer can pick it up consistently.
window.textEditCodeMirror = window.textEditCodeMirror || {};
// Mark that we loaded a local vendored CodeMirror bundle so other scripts
// can detect it and avoid loading a second instance.
window.__codemirror6_vendor_loaded = true;
window.textEditCodeMirror._cm6 = {
  EditorState: State.EditorState || State,
  EditorView: View.EditorView || View,
  basicSetup: basicSetup,
  markdown: markdown
};

export default window.textEditCodeMirror._cm6;
