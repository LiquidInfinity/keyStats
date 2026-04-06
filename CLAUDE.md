# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

- **Use Xcode** to build and run (`⌘R`). Do not use `xcodebuild` from command line.
- **Tests:** `swift test` (runs model tests via SPM — see `Package.swift` for the `KeyStatsCore` test target)
- **Distribution:** `./scripts/build_dmg.sh`

## Architecture

KeyStats is a **macOS menu bar app** (LSUIElement) that tracks keyboard/mouse input statistics — counts and distances only, never content.

**Data flow:**
```
CGEventTap (InputMonitor) → StatsManager → MenuBarController (display)
                                         → UserDefaults (persistence, debounced 2s)
                                         → StatsPopoverViewController (detail panel)
```

**Core singletons:**
- `InputMonitor.shared` — Global event tap at `cgSessionEventTap` level; monitors key presses, mouse clicks, movement (30Hz sampling), scroll. Requires Accessibility permission.
- `StatsManager.shared` — Aggregates daily stats, per-app stats, per-key counts, peak KPS/CPS (sliding window). Handles persistence, midnight auto-reset, and menu bar update callbacks.
- `MenuBarController` — NSStatusItem with compact dual-line display (SwiftUI `MenuBarStatusSwiftUIView` hosted in `MenuBarStatusView`). Manages popover lifecycle and highlight state.

**Data models** (`AppStats.swift`, `StatsModels.swift`):
- `DailyStats` — per-day aggregate with history dictionary
- `AppStats` — per-bundle-ID breakdown
- Both are `Codable`, persisted as JSON in UserDefaults

**Windows variant** lives in `KeyStats.Windows/` (C#/.NET, separate codebase).

## Critical Rules

### Privacy
- NEVER log actual keystrokes, mouse positions, or user input content — only aggregate counts and distances

### Thread Safety
- Event callbacks run on background CGEventTap thread — dispatch UI updates to `DispatchQueue.main`
- Three `NSLock`s protect concurrent access: `inputRateLock`, `statsStateLock`, `mouseDistanceCalibrationLock`

### Dark Mode
- `CALayer.backgroundColor`/`borderColor` use `CGColor` (static snapshot) — they don't auto-follow appearance changes
- Always resolve dynamic colors under current `effectiveAppearance` using `resolvedCGColor(color, alpha:, for:)` helper
- Re-assign layer colors on every theme change; never cache `CGColor` values
- Prefer dynamic colors (`NSColor.labelColor`, `NSColor.controlBackgroundColor`) over hardcoded values

### UI Style
- Soft glass-card surfaces: `controlBackgroundColor` with alpha ~0.6–0.85
- Thin 0.5pt separators with low alpha, subtle shadows, 10–12pt corner radius for cards
- When adding a new page/window/popover, add matching PostHog analytics: a `pageview` event and `click` events for key actions

### Code Conventions
- Localize user-facing strings with `NSLocalizedString()` (English + Simplified Chinese)
- Use `[weak self]` in closures to prevent retain cycles
- Maintain backward compatibility with existing UserDefaults keys when changing data models

## Dependencies (SPM)

- **PostHog** (posthog-ios) — Analytics, initialized in AppDelegate
- **Sparkle** — Auto-updates via GitHub releases appcast
