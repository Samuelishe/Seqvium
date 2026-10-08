# SEQ-R3-F1 — Desktop host and compact presentation evidence

Role: Bounded implementation/runtime/visual evidence report.
Read when: Reviewing the first desktop host or reconstructing its verification bounds.
Authoritative for: Observed environment, commands, outcomes and limitations, not current product contracts.
Not authoritative for: Future editor layout, roadmap completion, distribution support or owner visual acceptance.

## Scope and baseline

On 2026-10-09 local Windows 11 x64 (10.0.26300), branch `master`, HEAD
`95888aeac61aff5c3a4909d7abbfa62ae89db5a4`, initially clean tracked/untracked/index state.
R1/R2 remain complete / local accepted-ready within their declared bounds. R0 remains partially evidenced /
narrow. R3-F1 is separately authorized; R3 is **in progress / partial**, not complete. No commit/push,
staging or other Git mutation was performed. Subsequent owner instructions corrected the dashboard
presentation and added a long-term DAW visual contract without authorizing further implementation stages.

## Framework and topology

Official NuGet availability, package metadata/tagged license and net10.0 assets were inspected. Stable
Avalonia Desktop/Fluent 12.1.3 was adopted: native Windows/Linux/macOS desktop adapters, one .NET host,
MIT framework terms and no global template/tool/workload requirement. The alternative 11.x line was
available, but no parallel framework was built. Avalonia's own Skia/HarfBuzz/ANGLE dependencies are real
native distribution obligations, not a claim of a managed-only renderer. Actual provenance belongs in
[THIRD_PARTY](../THIRD_PARTY.md#r3-f1-desktop-dependencies), with version authority in manifest/lock.

`src/Seqvium.Desktop` is the sole added application assembly and references Core only. `Program`/`App`
start the desktop; `MainWindow` owns visual/chrome behavior; `WindowsWindowMenu` is guarded Windows OS
interop. `Presentation` contains pure `ShellSession`, `HostLocalizer`, `HostPalette`, `HostMetrics`,
`HostPreferences` and `PreferenceStore`. No dependency on Audio.Windows, DeviceCheck or the experiment
exists. Core, Windows audio implementation, canonical schema and all existing tests are unchanged.
The solution/test reference and corresponding package locks are updated.

## Version and compact composition

Repository inspection found SDK/package version authority but no prior product-wide version policy.
Desktop alone now declares `0.0.1-dev`, read from generated informational version in the functional About
flyout. `0.1.0` is not presented as a product milestone. Work-package identity is in About/evidence only;
the ordinary screen shows neither framework nor development status. Serialization versions are unchanged.

The rejected dashboard has been removed: no large project/workspace cards, hero text, introductory
paragraphs, feature placeholder controls or permanent future-feature list. A 30-DIP title/chrome row
shows real project title/unsaved status and window actions. A 30-DIP toolbar shows canonical tempo/meter
and compact language/theme/About actions. The remaining region is quiet full-width workspace; the
22-DIP status strip reports audio inactivity and any real preference failure. Spacing is normally 6 DIP,
body/caption 12/11 DIP, controls 24 DIP, small rounding 2 DIP. Main frame has no ScrollViewer/scrollbars.
Long project titles/status and warnings use ellipsis plus full tooltip; text does not globally shrink.

Project state comes from `ProjectDocument.Create`: null canonical name, pristine/unmodified, not saved,
120 BPM and 4/4. Only the null-name presentation is translated; user names are untouched. File actions
and musical editors are absent rather than inert/deceptive. About explains the limited capabilities on
demand. The central region is only a future composition slot, not a registry or docking/pane manager.

## Functional and GUI evidence

Release executable was actually launched repeatedly, not merely compiled. Temporary ignored PowerShell
checks used Windows UI Automation and ordinary mouse/keyboard input, inspecting native state and
capturing only Seqvium's rendered window. No permanent developer harness/instrumentation was added.
The completed post-correction smoke passed 24 checks:

- Window creation, no TopMost, actual title drag and border resize.
- Custom minimize/maximize/restore, Tab reachability of window and preference actions, Enter maximize/restore.
- Space language/theme change, immediate translated title/labels and retained keyboard focus.
- Usable reduced geometry, Alt+Space native system menu, Win+Up/Down and Alt+F4 process exit.
- No Seqvium.Audio.Windows module in the desktop process.

Custom close was separately exercised after the visual/About check. About actually displayed `0.0.1-dev`,
closed with Escape and returned focus to its originating action. Preference choices were retained across
actual launches; visual runs normalized settings through UI actions, leaving EN/Dark. That created the
host's preference file/previous copy; no musical project, accepted media or recovery files were touched.

Windows uses extended content with full OS window styles, an empty managed decorations template,
title/maximize decoration roles and accessible named controls. Alt+Space is handled in a tunnel and the
native menu opens after Alt release; opening on key-down was observed to dismiss immediately during
key release. Debugger evidence confirmed Space+Alt delivery and a nonzero OS system-menu handle. Agent
breakpoints were removed, all eight pre-existing user exception breakpoints preserved, and the attached
session ended when the test application closed. Synthetic chord timing was stabilized before final smoke.
The OS menu remains OS-localized independently of the host language. Other platforms retain native chrome.
No foreground-forcing logic/global hotkey exists in production; foreground calls were test input targeting only.

Startup calls no playback, WASAPI activation, microphone capture or DeviceCheck API and changes no OS
audio preferences. This is supported by project/source boundaries and actual loaded modules, not a new
physical audio certification or claims about sound from other running applications.

## Rendered visual review

All four final images were visually inspected after layout settled, including maximization animation.
They are original Seqvium screenshots, not copies/derivations of the owner's third-party reference images.
During the authorized R3-F2 hygiene pass the four generated PNGs were removed from the current tracked
tree. The essential observations, dimensions and limitations below are retained; this report no longer
depends on image files in Git. The table records what was observed, not a screenshot download inventory.

| State               | Actual client area at 96 DPI | Reserved work area height | Observation                                                                                                  |
|---------------------|------------------------------|---------------------------|--------------------------------------------------------------------------------------------------------------|
| Dark / EN           | 1100x750                     | 668 (89.1%)               | Quiet large work area, compact chrome, visible keyboard theme focus                                          |
| Light / EN          | 1100x750                     | 668 (89.1%)               | Same geometry/roles, readable text and separators                                                            |
| Reduced Dark / RU   | 640x511                      | 429 (84.0%)               | Requested approximately 640x500; native sizing yields 511 client height, all controls/title/status reachable |
| Maximized Dark / RU | 1920x1040                    | 958 (92.1%)               | Single 1920x1080 display/work area, stable frame, no shell scrollbars or clipped actions                     |

These percentages measure **reserved space**, not implemented musical content. Header/status claims are
source metrics; screenshot/native rectangle evidence confirms the geometry at this DPI. Thin separators,
subtle surfaces and compact text are inspected; automated palette tests additionally verify primary and
secondary/warning contrast. UIA exposes real control names/state/focus, but no screen-reader certification
is claimed. Owner review/acceptance is still pending. No dense editor or multi-pane screenshot exists yet.

The four supplied FL Studio/Cubase/SunVox/modern FL Studio references were accessible and viewed. The
accepted synthesis is recorded once
in [Professional DAW Workspace Design Target](../UI_DESIGN.md#professional-daw-workspace-design-target).
No proprietary assets, exact layouts/palettes, advertising Browser artwork or extreme tracker typography
were copied. Future musical surfaces must be reviewed in both dense and sparse real content states.

## Localization, themes and preference integrity

Stable dotted EN/RU catalogs use per-entry usable English fallback, then independent essential English
labels or a meaningful localized unavailable explanation. Whitespace, control characters and excessive
length are rejected. Both semantic palettes contain identical roles; `HostMetrics` centralizes bounded
compact dimensions. Preference commands refresh existing UI/resources immediately, without replacing
the document/window, changing culture/numeric interpretation or invalidating pending musical authority.

Version-1 preference JSON contains only `en/ru` and `dark/light`, in platform user configuration paths.
Missing/corrupt/unreadable settings fall back without destructive cleanup. Reads are bounded, writes
serialized, same-directory temporary bytes flushed, prior bytes backed up before replacement. Failure
leaves in-session changes usable with localized warning; oversized originals refuse overwrite.
No reset command exists. Multi-process writers, hostile filesystem links and crash/power-loss guarantees
are outside evidence. [SETTINGS](../SETTINGS.md#implemented-r3-f1-host-preferences) owns precise storage.

## Managed verification and portability

Local SDK 10.0.401/runtime 10.0.12; these exact commands succeeded:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
```

Zero build warnings/errors; **308 passed, zero failures/skips**, retaining all 274 baseline cases.
34 new cases cover RU/EN keys/fallback, semantic palettes/contrast, compact metrics/version, canonical
snapshot/generation/lifecycle/history/name/time integrity, acceptance of an actual prepared WAV after
preference changes, pure shell lifetime, ordered overlapping commands and safe persistence/corruption/
oversize/write failure. Ordinary tests initialize neither GUI platform nor physical audio.

The RID-independent desktop assembly also built locally with `UseAppHost=false`; package/native RID
inspection supports a hosted Windows/Ubuntu/macOS .NET 10 build/test matrix's feasibility. This is not
a hosted run or actual Linux/macOS execution. No CI workflow/global tool was added. Linux X11/XWayland
and its system libraries, macOS native dylib/frameworks, driver behavior and clean distribution remain
real prerequisites. Native Wayland was not adopted.
See [portability](../PORTABILITY.md#r3-f1-desktop-feasibility-and-evidence).

Windows launch after the locked Release build:

```text
dotnet run --project src/Seqvium.Desktop/Seqvium.Desktop.csproj -c Release --no-build --no-restore
```

## Remaining work and disposition

R3 remains **in progress / partial**. F1 is available for owner visual review, not accepted solely from
tests. R3-F2 actual internal panes/activation, independent focus/selection, floating/sizing/collapse,
drag/docking and user-layout persistence remain unimplemented and separately scoped. No R4, musical
editor, Browser/audio-preview UI, plugin host, recording, new DSP or distribution packaging was started.
High/mixed DPI, cross-monitor, screen readers and Linux/macOS GUI/native runtime are missing evidence.
Native notice bundling, delivered prerequisites and product support claims remain release work.

Task changes consist of Desktop source/manifest/lock, solution/test reference/lock and two host test
files; README/current owning docs, narrowed questions, archive rationale/work records and this report
originally accompanied by four generated screenshots, since removed from the current tree under the
[retention policy](README.md#evidence-retention). Initial baseline was clean; final status was entirely task work,
unstaged,
including new untracked source/evidence. No commit or push was performed.
