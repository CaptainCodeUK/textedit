#!/usr/bin/env fish
# Build a local CodeMirror 6 bundle used by the app to avoid multi-instance issues
set UI_DIR "$PWD/src/TextEdit.UI"
if test -d "$UI_DIR"
    cd "$UI_DIR"
    echo "Installing npm packages for TextEdit.UI (this may take a moment)"
    if test -f package-lock.json
        npm ci
    else
        npm install
    end
    echo "Building CodeMirror bundle..."
    npm run build:codemirror
    echo "Bundle created: $UI_DIR/wwwroot/lib/codemirror/codemirror-bundle.js"
else
    echo "Can't find $UI_DIR"
    exit 2
end
