#!/usr/bin/env fish
# Launch TextEdit app with remote debugging enabled for Playwright tests

set APP_DIR (dirname (status -f))/../src/TextEdit.App

echo "Starting TextEdit with remote debugging on port 9222..."
echo "Press Ctrl+C to stop"

cd $APP_DIR
set -x ELECTRON_ENABLE_LOGGING 1
electronize start /args --remote-debugging-port=9222
