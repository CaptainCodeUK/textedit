# Research: Scrappy Text Editor v1.2 Enhancements

## Decision: Spell checking engine/library
- **Chosen**: Use WeCantSpell.Hunspell (MIT) as the spell checking engine for all platforms.
- **Rationale**: Modern, .NET-native implementation of Hunspell (industry standard, used by LibreOffice/Firefox/Chrome). Supports custom/user dictionaries, Unicode, and is actively maintained. Integrates easily with .NET 8, Blazor, and Electron.NET. Avoids reinventing spell checking logic and leverages proven algorithms.
- **Alternatives considered**: NHunspell (older, less .NET-native), NetSpell (less accurate), custom implementation (higher maintenance, less robust).

## Decision: Spell checking in code blocks
- **Chosen**: Spell checking is disabled within code blocks and markdown fenced sections; no misspelling indicators appear inside those regions.
- **Rationale**: Reduces false positives for technical content, matches user/editor expectations, and improves usability for developers and technical writers.
- **Alternatives considered**: Enable everywhere (would flag code as misspelled), user toggle (adds UI complexity).

## Decision: Critical security update behavior
- **Chosen**: Prompt user with a clearly labeled critical update dialog; installation proceeds only after user confirmation and the messaging emphasizes urgency.
- **Rationale**: Balances user control with security urgency, avoids surprise restarts, and ensures users are aware of critical updates.
- **Alternatives considered**: Auto-install without prompt (could disrupt work), treat as normal update (less urgency).

## Decision: CI build behavior for rapid commits
- **Chosen**: Any in-progress or queued builds for earlier commits are canceled and only the latest commit is built; canceled runs are marked accordingly in CI.
- **Rationale**: Efficient use of CI resources, fast feedback on current state, avoids redundant builds and artifacts.
- **Alternatives considered**: Queue all builds (slow, expensive), debounce with time window (delays feedback).

## Decision: Test assertion library preference
- **Chosen**: Use standard xUnit assertions (`Assert.Equal`, `Assert.True`, `Assert.Throws`, etc.) for all unit tests. Avoid FluentAssertions or other third-party assertion libraries.
- **Rationale**: Reduces dependencies, keeps tests simple and maintainable, avoids learning curve for new contributors, and xUnit's built-in assertions are sufficient for our needs. FluentAssertions adds minimal value for the added complexity and dependency maintenance.
- **Alternatives considered**: FluentAssertions (more expressive but adds dependency), Shouldly (similar tradeoffs).

