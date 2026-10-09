# SEQ-R4-F3-A desktop workflow evidence

Question: can the existing canonical/music/media/graph/audio foundations support a bounded usable
desktop workflow, with a compact first-party presentation and safe document/consumer lifetime?

Scope: real WAV import/use, selected canonical Gain, explicit physical playback, Undo/Redo,
Save/Save As/Open and dirty replacement/close. No graph canvas or later musical editor is implemented.
Functional implementation is complete; revised visual acceptance remains **pending owner review**.
R4-F3 as a whole is incomplete, and R4-F2 remains partial.

## Environment and deterministic verification

Windows 11 x64, 10.0.26300, one 1920x1080 screen at 96 DPI; SDK 10.0.401/runtime 10.0.12,
Avalonia 12.1.3, framework-dependent Release desktop. Commands:

```powershell
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
```

Locked restore and full build pass with zero warnings/errors; **646 tests pass, zero failures/skips**.
All previous 609 cases remain; 28 workflow cases and nine presentation/observation cases are added.
Real document, durable media, preparation, coordinator and sampler implementations execute in tests;
only physical output ownership is substituted. Gate/barrier tests establish pending worker cancellation,
join ordering, failed join retention/retry and prepared PCM release without a timing oracle or device.
Canonical Gain changes actual PCM while retaining voices, prepared execution identity, attachment and
origin/equivalent provenance. Save As/reopen preserves IDs/media; no inherited runtime or implicit Play.
[Test execution](../TEST_EXECUTION.md#r4-f3-a-desktop-workflow-verification) owns suite scope.

Intermediate failures were corrected: early test fixtures used the wrong file-failure setup/PCM packet
size; a proposed Geometry.Parse test lacked Avalonia's rendering platform and was removed rather than
initializing GUI in deterministic tests. Vector geometry is exercised by the real application.
Final duration review found a real one-frame truncation with a one-tick-only duration guard during
44.1↔48 kHz conversion. Two controlled regression cases fail with that old policy and pass with a
one-frame-at-44.1-kHz guard plus one tick. Three cross-rate/tempo cases verify actual final PCM against
an independent rational interpolation-to-zero EOF oracle; its initial constant-final-frame assumption
was corrected. An intermediate build retried a temporarily locked desktop executable; the final
required build has zero warnings/errors. No process was force-terminated to obtain this result.

## Actual Windows GUI observations

The actual Release executable and Windows UI Automation/native input were used, not a mock window.
Rendered screenshots cover the revised one-row layout at 1100x750, both RU/EN and Dark/Light, reduced
width 640 DIP, and maximized 1920x1040 pixels (1080-pixel display minus taskbar). The inherited R3
nominal client minimum is 640x480 DIP; its visible Windows minimum capture is **640x511 pixels**.
Do not infer an exact 480-pixel outer-frame review or mixed/high-DPI acceptance from this matrix.

- Project/transport/history vectors, single row, readable selection and numeric Gain; no UUID/internal
  graph key or device paragraph in the ordinary workspace. Details/status deliberately disclose them.
- Read-only BPM/meter follow real settings, including 137.125 BPM and 7/8 from an opened project;
  reduced width exposes them in overflow and Inspector. No tempo/meter editor was added.
- Gain 0.00/1.00, decimal point and reopened invalid 2.00 render fully; invalid Gain blocks Play and
  explicit repair to 0.20 succeeds. Actual Undo/Redo and keyboard F5/Shift+F5/Ctrl+Esc were exercised.
- Owned unsaved modal is 410x94 DIP, without system titlebar, duplicate header or empty lower area;
  RU/EN, Dark/Light, initial Cancel focus, Cancel/Esc/Alt+F4 and failed Save retain the document/window.
- Native WAV/project pickers and Save As were used. Accepted WAV remains durable after removal of its
  external task-owned source. Open/reopen does not start audio. Picker cancellation returns safe focus.
- Inspector and Appearance collapse/restore, hide/reopen and left/right dock actions retain real
  content. Disabled startup/missing-media/invalid graph actions, tooltip/hover and keyboard focus were
  inspected, including the disabled Play tooltip with shortcut and blocking reason. Locked-file
  Save/close-Save failure keeps work; corrupt Open keeps the current document.

Automation limits were observed and retried, not treated as product success: native dialogs expose
duplicate filename IDs for ComboBox/Edit; first Invoke can navigate rather than finish, and menu items
do not expose InvokePattern. Keyboard/menu interaction completed applicable checks. Late captures
guard foreground process identity to avoid recording unrelated windows. Incorrect/intermediate
captures are not acceptance evidence. Screenshot file names were reconciled with actual semantic
surface colors after language/theme toggles; images themselves are unmodified.

Final review captures remain ignored in `.artifacts/SEQ-R4-F3-A/`, especially
`review-final-1100-{ru,en}-{dark,light}.png`, `review-final-640-{ru,en}-{dark,light}.png`,
`review-final-dialog-ru-{dark,light}.png`, `review-final-gain-{zero,unity}.png`,
`review-final-640-overflow.png` and `review-final-output-details.png`. Earlier verified final captures
retain EN modal, pane/focus and failure evidence. These local paths are not durable repository links.

## Physical output and limits

Explicit quiet Play opened **Samsung USB C Earphones**, 48000 Hz, two channels through the existing
WASAPI/coordinator path. The task-authored ten-second mono PCM16 220 Hz signal has source amplitude
approximately 0.01; the smoke uses canonical Gain 0.10. Short playing/stopped observations, Stop/Panic,
reopen requiring new Play and normal joined close were exercised. This establishes bounded endpoint
integration, not callback timing, acoustic quality, DAC latency, prolonged pressure or listening approval.
No owner subjective listening result is recorded. No device disconnect/driver fault was forced;
deterministic fault injection covers release/no restart. No fallback, audio policy, GC mode, backend,
buffer size, tracing, driver or scheduler/priority change was made.

The previously published [F2 pressure failures](SEQ-R4-F2_REPORT.md) remain failures. This UI slice
does not supersede their attribution/evidence or complete F2 acceptance. Screen readers, other OSes,
high/mixed DPI and long/multiple-device sessions remain unevidenced.

## Retention and recommendation

Source/tests/styles/icons are permanent product material; synthetic projects/WAVs, GUI scripts,
screenshots and generated configuration remain ignored local review artifacts. No original user
configuration file was present in the pre-launch conditional backup; GUI-created preference/layout
files were backed up as `config-generated/` and removed from the application location after joined
shutdown to restore initial absence. The final tooltip/Alt+F4 pass separately snapshots and restores
the configuration present immediately before that pass, preserving subsequent owner changes.
Accepted managed WAV copies retain the existing conservative R2
policy; this work adds no automatic media GC. No staging, commit or push was performed.

Recommend owner review of the revised controls/modal before any visual accepted-ready declaration.
F3-B still owns real canvas/wires, create/delete/connect/disconnect, free placement/pan/zoom, typed
feedback and gesture grouping, plus single-/multi-source invalid/last-valid/repair GUI acceptance.
