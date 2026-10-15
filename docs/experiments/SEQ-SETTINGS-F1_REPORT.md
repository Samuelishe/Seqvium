# SEQ-SETTINGS-F1 — global settings and application bindings

Date: 2026-10-15. Evidence for the implemented global Settings center; contracts remain in
[Settings](../SETTINGS.md#unified-global-settings-center), [UX](../UX_CONTRACT.md),
[Architecture](../ARCHITECTURE.md) and [UI design](../UI_DESIGN.md).

## Outcome and scope

General owns explicit language/theme selection, Controls owns persistent Browser auto preview and
thirteen remappable application commands, and About retains the brand composition and assembly-derived
version. Appearance, toolbar language and the old About flyout are retired. The title-row sun/moon
opens a checked catalog menu, with a disclosure chevron and Choose theme/current-theme tooltip.
Only effective custom shortcuts show a dot/bold chord and an individual default icon. Defaults have
no reset button; the common keyboard reset remains. Default restoration can itself require named
conflict transfer. Existing preference/layout files and musical project format are compatible.

The stage is complete for global application commands. Editor-local gestures, pane navigation/close,
multiple alternatives/profiles, controllers, theme packages, broader reset/repair and future VST/audio
I/O/MIDI content remain separate work. No empty categories or R8 implementation are introduced.
New global preferences and commands are routed through AGENTS/INDEX/PROJECT_STATE and canonical owners.
This is the repository's current Markdown knowledge base, not an installed RAG indexer.

## Verification and environment

Windows 11 x64, .NET SDK 10.0.401/runtime 10.0.12, Avalonia production Desktop and the existing
VisualCheck executable with isolated preferences/layout/media. The read-only Neon Orchard source
is opened without Play. No ordinary user preference file or source project is modified by checks.

- Full Release solution build: zero warnings/errors.
- L3: **1384/1384 tests**, zero skips, one thread, **99.262 s** CLI summary. This follows one failed
  full attempt (1383 pass, one stale source-location assertion looking for About in MainWindow AXAML).
  That existing version guard now checks the Settings accessor; version identity stays source-derived.
- The final small minimum-size/custom-reset/theme-affordance refinement uses focused checks instead
  of repeating L3: **105/105 focused tests**, zero skips, **17.642 s** CLI summary, with a zero-warning
affected Release build. Its new regression distinguishes effective custom values, explicit defaults and
  unassigned bindings. Tests also cover real resolution/removal, conflict transfer, text-local Undo,
  reserved keys, storage backup/refusal and musical/appearance independence.
- Actual production windows: RU/EN × Dark/Light, settings 720×570 and **500×340**, main window down
  to **640×480**, plus populated 900×600 chrome review. General/Controls/About, disclosed fixed-key
  details and temporary capture are rendered. Capture closes details so minimum-size actions stay
  above the footer. Shortcut reset visibility, filter/no-results, capture/cancel/reserved refusal,
  conflict transfer, clear, per-command defaults and keyboard-only reset are checked through controls.
- A remapped chord really opens Browser; its previous chord stops doing so. Browser consumes the same
  persistent Auto preview value. File reload and settings reopen retain choices. Text-local Undo does
  not edit the musical snapshot/history. Explicit title-menu choices consume ThemeCatalog.
- Shared component catalog: Dark/Light templates, field geometry, state fixtures and resource
  propagation pass. These pseudo states are not native pointer evidence.

Use [VisualCheck settings](../../tools/Seqvium.VisualCheck/README.md#global-settings) and the normal
SDK-10/xUnit MTP commands in [test execution](../TEST_EXECUTION.md#global-settings-and-command-bindings).
The owner-provided four screenshots and local WORK-EQ-001/WORK-OT-001/WORK-CR-001 actual images were
inspected before design. They are private visual study, not distributed assets.

## Failures, retention and limits

The first rendered pass exposed reset icons under the scrollbar; the shared scroll gutter fixes it.
The first filter assertion ran before the TextBox event was published; the harness now waits for that
GUI publication. One invalid MTP filter wildcard and one incorrect solution filename were corrected;
they did not execute tests/builds. Native startup/action attempts, including ordinary activation and
same-user shell launch, were refused by exact foreground/OS coverage guards. No foreign-window input
was sent. Successful actual-window renders and admitted Windows compositor captures do not establish
native mouse/keyboard acceptance. Native input remains an explicit evidence gap.

Completion inspection found that ProjectStats' generic settings-directory privacy filter excluded
SettingsWindow source. A narrow exception admits only `src/Seqvium.Desktop/Settings`; a regression
checks this source is counted while other Settings and nested private trees are never opened.
The rebuilt tool passes **76/76 ProjectStats tests** (1.964 s), zero skips, and both ignored root
reports are refreshed; this is corpus accuracy, not a new
settings feature or refactoring permission.

Generated screenshots/configuration/JSON stay ignored under `.artifacts/settings/` for local review,
with the existing rolling retention policy; the current success and bounded failure are retained.
Reusable source stays in tools/tests. No raw screenshots are distributed. No physical output,
Linux/macOS, mixed/high DPI, accessibility certification, stress or owner aesthetic acceptance is
claimed. GUI and deterministic checks do not close earlier audio pressure gaps.

One BUILD advances `0.1.8.0002` to **0.1.8.0003**. This refines application configuration and input,
not a new musical capability/PATCH, project reader change or release. No stage/commit/push/tag occurs.
