# GitHub Actions Workflows

This directory contains CI/CD workflows for the TextEdit project.

## Workflows

### `ci.yml` - Continuous Integration

**Triggers:**
- Push to `main`, `develop`, or any `*-enhancements` branch
- Pull requests to `main` or `develop`

**What it does:**
1. Builds the .NET solution
2. Runs all tests
3. Collects code coverage (enforces 65% threshold)
4. Uploads test results and coverage reports as artifacts
5. Comments on PRs with test status
6. Creates GitHub issues for build failures

**Concurrency:** Cancels in-progress builds when new commits are pushed to the same branch/PR.

---

### `release.yml` - Release Builds

**Triggers:**
- Git tags matching `v*.*.*` pattern (e.g., `v1.2.0`)
- Manual workflow dispatch (for testing)

**What it does:**
1. **Build job** (matrix: Windows, macOS, Linux):
   - Restores dependencies with NuGet caching
   - Builds the solution in Release mode
   - Runs all tests
   - Builds Electron app for target platform using `electronize build`
   - Uploads platform-specific artifacts:
     - **Windows**: `.exe` installer, `.nupkg` + `RELEASES` (Squirrel)
     - **macOS**: `.dmg`, `.zip` (Squirrel.Mac)
     - **Linux**: `.AppImage`, `.deb`

2. **Release job** (only for tag triggers):
   - Downloads all build artifacts from build job
   - Generates release notes from git commits since previous tag
   - Creates GitHub Release with:
     - Version extracted from tag
     - Generated release notes
     - All platform artifacts attached
   - Uses `GITHUB_TOKEN` (automatic, no secrets needed)

**Concurrency:** Cancels in-progress builds when new commits are pushed to the same tag (rare edge case).

**Artifact retention:** 30 days for non-release builds.

---

## Creating a Release

### Step 1: Tag the commit
```bash
git tag v1.2.0
git push origin v1.2.0
```

### Step 2: Monitor workflow
- Go to **Actions** tab in GitHub
- Watch the "Release Build" workflow
- Verify all three platform builds succeed

### Step 3: Verify release
- Go to **Releases** page
- Confirm new release is published with all artifacts
- Check auto-generated release notes

### Step 4: Test auto-updater
- Install previous version of app
- Launch app and verify update notification appears
- Test "Check for Updates Now" in Options dialog

---

## Troubleshooting

### Build fails on a specific platform
- Check the platform-specific logs in the workflow run
- Common issues:
  - Missing dependencies (Node.js version mismatch)
  - Electron.NET CLI installation failure
  - Platform-specific electronize build errors

### Artifacts not uploading to release
- Verify the `GITHUB_TOKEN` has `contents: write` permission (default for tags)
- Check artifact paths in `release.yml` match actual build output
- Use the debug step "List build output" to see actual file locations

### Release notes missing commits
- Ensure previous tag exists and is reachable
- Check git history: `git log <previous-tag>..HEAD`
- First release will include all commits (no previous tag)

### Tests fail in CI but pass locally
- Check .NET SDK version mismatch
- Verify all test dependencies are restored
- Look for race conditions or timing-dependent tests
- Check for platform-specific differences (Linux vs Windows vs macOS)

---

## Manual Testing

To test the release workflow without creating a real release:

1. **Use workflow_dispatch:**
   - Go to Actions → Release Build → Run workflow
   - Select branch and click "Run workflow"
   - This will build all platforms but won't create a GitHub release

2. **Download artifacts:**
   - Click on the workflow run
   - Scroll to "Artifacts" section
   - Download platform-specific artifacts
   - Test installers manually

3. **Test with draft release:**
   - Edit `release.yml` and change `draft: false` to `draft: true`
   - Create a test tag like `v0.0.0-test`
   - Release will be created as draft (not visible to users)
   - Verify artifacts, then delete draft and tag

---

## Maintenance

### Updating .NET version
Edit both workflow files:
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'  # Update here
```

### Updating Node.js version
Edit `release.yml`:
```yaml
- name: Setup Node.js
  uses: actions/setup-node@v4
  with:
    node-version: '22.x'  # Update here
```

### Adjusting coverage threshold
Edit `ci.yml`:
```yaml
/p:Threshold=70  # Change from 65 to 70
```

### Changing artifact retention
Edit both workflow files:
```yaml
retention-days: 90  # Change from 30 to 90 days
```

---

## Security Notes

- **GITHUB_TOKEN**: Automatically provided by GitHub Actions, scoped to the repository
- **Secrets**: No additional secrets required for basic workflow
- **Permissions**: Uses default token permissions (contents: read/write for releases)
- **Dependencies**: All actions are from verified publishers (actions/, softprops/)

---

## Performance Optimizations

1. **NuGet caching**: Caches `~/.nuget/packages` based on `*.csproj` hash
2. **Concurrency control**: Cancels in-progress builds to save runner minutes
3. **Fail-fast: false**: Continues building other platforms if one fails
4. **Parallel platform builds**: Uses matrix strategy for simultaneous builds

---

## Future Enhancements

- [ ] Add code signing for Windows/macOS (requires certificates in secrets)
- [ ] Implement delta updates for faster downloads
- [ ] Add smoke tests for built artifacts before release
- [ ] Create pre-release channel for beta testing
- [ ] Add Slack/Discord notifications for releases
- [ ] Implement rollback mechanism if release has critical bugs
