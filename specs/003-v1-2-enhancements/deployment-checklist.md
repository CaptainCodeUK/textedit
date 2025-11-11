# Deployment Checklist: v1.2 Release

This checklist guides the process of creating and testing a v1.2 release with auto-update support.

## Pre-Release Verification

- [ ] All US6 (Auto-updater) tasks completed
- [ ] All US7 (CI/CD) tasks completed
- [ ] All unit tests passing (250+ tests)
- [ ] Code coverage meets threshold (65%+)
- [ ] Manual testing on all platforms (Windows/macOS/Linux)
- [ ] No critical bugs in issue tracker
- [ ] Release notes drafted (What's New section)

## Version Update

- [ ] Update version in project files:
  - [ ] `src/TextEdit.App/TextEdit.App.csproj` - `<Version>1.2.0</Version>`
  - [ ] `src/TextEdit.Core/TextEdit.Core.csproj`
  - [ ] `src/TextEdit.Infrastructure/TextEdit.Infrastructure.csproj`
  - [ ] `src/TextEdit.UI/TextEdit.UI.csproj`
  - [ ] `src/TextEdit.Markdown/TextEdit.Markdown.csproj`

- [ ] Update CHANGELOG.md with v1.2.0 changes
- [ ] Update README.md version references if applicable

## GitHub Workflow Testing

### Test 1: CI Workflow (Non-Release Build)
- [ ] Push to branch `003-v1-2-enhancements`
- [ ] Verify CI workflow runs successfully
- [ ] Check test results are uploaded as artifacts
- [ ] Verify coverage reports are generated

### Test 2: Release Workflow (Dry Run)
- [ ] Use workflow dispatch for manual trigger:
  ```bash
  # Go to: Actions → Release Build → Run workflow
  # Select branch: 003-v1-2-enhancements
  # Click: Run workflow
  ```
- [ ] Verify all three platform builds (Windows/macOS/Linux) succeed
- [ ] Download artifacts from workflow run
- [ ] Test each platform installer locally:
  - [ ] Windows: `.exe` installer works
  - [ ] macOS: `.dmg` opens and app runs
  - [ ] Linux: `.AppImage` is executable and runs

### Test 3: Actual Release Build
- [ ] Merge `003-v1-2-enhancements` to `main`
- [ ] Create and push release tag:
  ```bash
  git checkout main
  git pull origin main
  git tag v1.2.0
  git push origin v1.2.0
  ```
- [ ] Monitor GitHub Actions → Release Build workflow
- [ ] Verify all platform builds complete successfully
- [ ] Check GitHub Releases page for new v1.2.0 release
- [ ] Verify all artifacts are attached to release:
  - [ ] Windows: `TextEdit-Setup-1.2.0.exe`, `*.nupkg`, `RELEASES`
  - [ ] macOS: `TextEdit-1.2.0.dmg`, `TextEdit-1.2.0.zip`
  - [ ] Linux: `TextEdit-1.2.0.AppImage`, `TextEdit-1.2.0.deb`

## Auto-Update Testing

### Test 4: Update Detection
- [ ] Install v1.1.0 (previous version) on clean test machine
- [ ] Launch app and wait for startup update check
- [ ] Verify update notification appears with v1.2.0 details
- [ ] Check Options → Automatic Updates shows:
  - [ ] Current version: 1.1.0
  - [ ] Status: "Update available" or "Downloading..."
  - [ ] Last check time updated

### Test 5: Update Download
- [ ] Monitor download progress in Options dialog
- [ ] Verify status changes: Checking → Available → Downloading → Ready
- [ ] Check download percentage updates during download
- [ ] Verify UpdateNotificationDialog appears when Ready

### Test 6: Update Installation
- [ ] Click "Restart and Update" in notification dialog
- [ ] Verify app closes and restarts
- [ ] After restart, verify version is now 1.2.0
- [ ] Check all features work correctly after update
- [ ] Verify no data loss (open documents, preferences preserved)

### Test 7: Manual Update Check
- [ ] Open Options → Automatic Updates
- [ ] Click "Check for Updates Now" button
- [ ] Verify status updates immediately
- [ ] Verify "Checking..." button state while checking
- [ ] Verify "Up to date" status when no update available

### Test 8: Update Settings Persistence
- [ ] Toggle "Check for updates on startup" OFF
- [ ] Restart app
- [ ] Verify no automatic update check occurs
- [ ] Toggle "Download updates automatically" OFF
- [ ] Trigger update check manually
- [ ] Verify update detected but not downloaded automatically
- [ ] Verify settings persist across restarts

### Test 9: Critical Update Flow
- [ ] Create test release with `IsCritical: true` in metadata (requires manual edit)
- [ ] Trigger update check
- [ ] Verify notification dialog shows:
  - [ ] Red warning icon (not blue info icon)
  - [ ] "Critical Update Available" title
  - [ ] "This is a critical security update" message
  - [ ] No "Remind Me Later" button (only "Restart and Update")
  - [ ] Cannot dismiss with Escape or backdrop click

### Test 10: Error Handling
- [ ] Test with invalid feed URL (simulate network error)
- [ ] Verify status changes to "Error"
- [ ] Verify error is logged (check logs folder)
- [ ] Verify no error dialog shown to user (graceful degradation)
- [ ] Verify app continues working normally

## Post-Release Verification

- [ ] Monitor GitHub issue tracker for user-reported bugs
- [ ] Check analytics/telemetry for update adoption rate
- [ ] Verify auto-update server (GitHub Releases) is accessible
- [ ] Test update flow from multiple older versions (1.0.0, 1.1.0)
- [ ] Document any issues in Known Issues section

## Rollback Plan

If critical issues are discovered after release:

1. **Immediate Actions:**
   - [ ] Create GitHub issue with "critical" and "regression" labels
   - [ ] Draft emergency patch or revert commit
   - [ ] Notify users via GitHub Releases page update

2. **Rollback Options:**
   - [ ] Delete problematic release tag and GitHub Release
   - [ ] Users on v1.2.0 won't see new updates until fixed version released
   - [ ] Create v1.2.1 hotfix release with fix
   - [ ] Document rollback in CHANGELOG.md

3. **Long-term Fix:**
   - [ ] Implement rollback mechanism (track previous version, show "Revert" option)
   - [ ] Add pre-release testing channel for beta users
   - [ ] Improve automated smoke tests in CI/CD

## Success Criteria

Release is considered successful when:
- ✅ All three platform builds complete without errors
- ✅ GitHub Release created with all artifacts attached
- ✅ Update detection works on all platforms
- ✅ Update installation succeeds without data loss
- ✅ No critical bugs reported within 48 hours
- ✅ Options dialog shows correct version and update status
- ✅ Auto-update preferences persist correctly

## Notes

- **First Release:** v1.2.0 is the first release with auto-update support. Users on v1.1.0 or earlier won't auto-detect this update (requires manual download).
- **Future Updates:** v1.3.0+ will auto-detect and notify users on v1.2.0+.
- **GitHub Releases Feed:** `https://github.com/CaptainCodeUK/textedit/releases` (update if repo changes)
- **Squirrel Limitations:** Windows/macOS use Squirrel format. Linux uses AppImage with zsync for delta updates.

---

**Date Completed:** _____________

**Release Manager:** _____________

**Issues Encountered:** _____________
