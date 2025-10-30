# Specification Analysis Report — 002-v1-1-enhancements

Date: 2025-10-30
Scope: specs/002-v1-1-enhancements/spec.md, plan.md, tasks.md

## Coverage summary

- User Stories (US1–US8)
  - US1 (CLI + single-instance + CLI error summary): Covered by T031–T043, T039–T040 (non-blocking summary). Timing verification partially covered by T175 (startup), but not explicitly asserting “tabs open ≤3s” (SC-001) or “summary appears ≤2s” (SC-016).
  - US2 (Identity, About, Title bar, App icon): Covered by T044–T060 and T050–T055. App icon multi-resolution (FR-013) via T047; branding across surfaces (FR-008–FR-012) covered.
  - US3 (Theme: Light/Dark/System + OS follow + persistence): Covered by T061–T080. OS detection and watch (T071–T073), debouncing (T074), persistence (T075–T076). Markdown preview theme support (T069–T070).
  - US4 (File extensions): Covered by T081–T093. Format validation (T084), duplicates (T090), persistence (T092–T093). Gap noted for scenario with removed non-critical default extension behavior (see “Gaps”).
  - US5 (Toolbar: file/clipboard/markdown + fonts): Covered by T094–T127. Disabled states (T100/T102/T104/T106), tooltips (T126 per FR-052), markdown insertion/wrap (T119), global font prefs (T107–T111).
  - US6 (Menu icons): Covered by T128–T135. Consistent sizing (T133 per FR-058), theme-aware variants (T134) – phrasing is conditional/ambiguous (see “Ambiguities”).
  - US7 (Logging toggle): Covered by T136–T149. Rotation (T145 per FR-042), location (T144), latency check (T149).
  - US8 (Enhanced styling): Covered by T150–T160. System accent colors (T150–T151 per FR-060), WCAG contrast (T152–T153 per FR-061), focus indicators (T155 per FR-062), consistency (T157 per FR-063).

- Functional Requirements (FR)
  - Explicitly referenced in tasks: FR-028, FR-042, FR-052, FR-058, FR-060, FR-062. The remaining FRs are covered implicitly via US-tagged tasks as described above.
  - Notable mappings:
    - FR-001..FR-007 → US1 (T031–T043)
    - FR-008..FR-013, FR-014..FR-020 → US2 (T044–T060, T050–T055)
    - FR-021..FR-028 → US3 (T061–T080) [FR-028 is explicit]
    - FR-029..FR-036 → US4 (T081–T093)
    - FR-037..FR-044 → US7 (T136–T149) [FR-042 explicit]
    - FR-045..FR-055 → US5 (T094–T127) [FR-052 explicit]
    - FR-056..FR-059 → US6 (T128–T135) [FR-058 explicit]
    - FR-060..FR-063 → US8 (T150–T160) [FR-060, FR-062 explicit]

- Success Criteria (SC)
  - Performance SCs are partially covered by T175–T179 (startup, theme switch, toolbar, font), and logging latency (T149). Missing explicit checks for: SC-001 (tabs open ≤3s including multiple CLI files) and SC-016 (summary appears ≤2s). SC-013 (title bar updates within 100ms) lacks explicit measurement.

## Gaps and risks (top issues)

1) US4 Scenario 5 (Prompt after removing a default, non-critical extension) — Gap
   - Spec: If a user removes a default extension and opens such a file, prompt whether to open as text or use system default.
   - Current tasks: T091 updates open logic to check configured list; no prompt behavior task exists.
   - Add: New task to implement and test a prompt path for files with extensions removed from the recognized list (excluding critical defaults .txt/.md, per FR-032).

2) SC-001 (Tabs open ≤3s with CLI args) — Partial
   - T175 measures startup <2s but doesn’t explicitly assert time-to-tabs-open for N files (up to 10 per spec).
   - Add: Extend integration/perf test to measure and assert tabs opening time threshold.

3) SC-016 (CLI invalid-path summary ≤2s after UI interactive) — Gap
   - Tasks T039–T040 implement the summary but do not measure timing.
   - Add: Integration test asserting the 2s bound and that interaction isn’t blocked.

4) SC-013 (Title bar updates ≤100ms) — Gap
   - Tasks T056–T060 implement logic; no explicit performance validation.
   - Add: Lightweight perf/stopwatch check around dirty change and active tab change.

5) FR-048a/FR-049a (Font defaults and size clamping 8–72pt) — Partial
   - Tasks specify defaults and range (T107–T111), but no explicit clamping logic/test is called out.
   - Add: Task to enforce and test clamping at boundaries.

6) FR-059 (Menu icons adapt to theme) — Ambiguous
   - T134 says “if needed”; acceptance should be explicit: icons must remain visible in both themes. Provide deterministic approach (e.g., dual-asset or CSS filter) and tests/visual checks.

7) FR-007 (Spaces/special characters in CLI paths) — Test coverage
   - Implementation implied in T031–T036; ensure integration tests cover quoted paths, spaces, unicode.
   - Add: Integration tests for path variants across platforms.

8) US3 performance under load
   - SC requires theme switching ≤500ms with many tabs. T176 exists, but ensure test opens 10+ tabs before measuring to match spec.

9) US2 App icon quality across DPIs
   - T135 covers platform verification; include a DPI/retina check list or screenshot-based validation for smallest sizes (SC-012).

10) Accessibility confirmations
   - T169–T174 cover audits broadly; ensure keyboard focus order and aria-labels for toolbar icons are included explicitly (ties to FR-062 and UX requirements). Add short checklist.

## Ambiguities to clarify (and suggested resolutions)

- “Theme-aware icon variations (light/dark) if needed” (T134)
  - Clarify criteria: Icons MUST remain legible in both themes (FR-059). Suggest approach:
    - Preferred: Provide both light and dark SVGs and switch via CSS [data-theme] selector.
    - Alternative: Use CSS filter/invert only if visual quality is verified against brand standards.

- “Populate with system monospace fonts” (T107)
  - Resolution: Use a small curated list with generic fallback: `ui-monospace, SFMono-Regular, Menlo, Consolas, Liberation Mono, DejaVu Sans Mono, monospace` to avoid slow/fragile OS enumeration.

- App icon sourcing (T046)
  - Note: If commissioning is out of scope, define a placeholder asset path and acceptance for swapping later, so engineering can proceed with build/test.

## Constitution and quality gates

- Build/Lint: PASS (no code changes in this report)
- Tests: N/A for this step. Repo-level gate T163 ensures ≥65% coverage; Core target 92%+ (T164). These match Directory.Build.props; no constitution mismatch found.

## Suggested task additions/edits

- New: T0A1 [US4] Prompt when opening a file whose extension was removed (non-critical defaults). Offer “Open as text” or “Use system default”. Persist user decision per session optional.
- New: T0A2 [US1][Perf] Assert SC-001 — tabs for up to 10 CLI files open ≤3s (integration/perf test).
- New: T0A3 [US1] Assert SC-016 — non-blocking CLI error summary appears ≤2s after UI interactive.
- New: T0A4 [US2][Perf] Assert SC-013 — title bar updates (dirty/active) ≤100ms.
- New: T0A5 [US5] Enforce and test font-size clamping at 8–72pt boundaries.
- Edit: T134 — Replace “if needed” with explicit acceptance: “Icons MUST be legible in both themes; provide light/dark variants or verified CSS filter, with visual checks.”
- Edit: T107 — Document curated monospace list + fallback to avoid OS enumeration.
- Edit: T176 — Ensure measurement is performed with ≥10 open tabs (per SC wording).

## Quick coverage map (abridged)

- FR-001..FR-007 → T031–T043
- FR-008..FR-013 → T044–T060; App icon multi-res via T047–T049
- FR-014..FR-020 → T050–T055
- FR-021..FR-028 → T061–T080 (FR-028 explicit via T158)
- FR-029..FR-036 → T081–T093 (Add prompt behavior task for removed non-critical defaults)
- FR-037..FR-044 → T136–T149 (FR-042 explicit via T145)
- FR-045..FR-055 → T094–T127 (FR-052 explicit via T126)
- FR-056..FR-059 → T128–T135 (FR-058 explicit via T133; clarify FR-059 acceptance)
- FR-060..FR-063 → T150–T160 (FR-060/FR-062 explicit via T150/T155; FR-061 via T152/T153)

- SC-004/008/009 → T176/T177/T178
- SC-001 → Needs explicit tab-open timing check (add T0A2)
- SC-013 → Needs explicit ≤100ms check (add T0A4)
- SC-016 → Needs explicit ≤2s summary timing (add T0A3)

## Next actions

- Add the “Suggested task additions/edits” to tasks.md and wire corresponding tests.
- Proceed with US1 implementation/tests first (independent slice), then US2 branding/About and US3 theming.
- Keep performance assertions close to feature tests to prevent regressions.

---
Completion summary: Parsed spec/plan/tasks, mapped requirements to tasks, identified 10 key gaps/ambiguities, and proposed concrete task additions/edits. No code changes made; quality gates unaffected at this step.
