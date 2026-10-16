# SEQ-PIANO-RACK-UI-FIX-R1 — Piano selectors and Rack time geometry

Date: 2026-10-17. Baseline: clean `master`, `e549fba72d0144fb09a7c21ccbe1f931adf882b9`,
0.1.16.0004. This checkpoint selects one BUILD, **0.1.16.0005**, for two bounded UI repairs.

## Confirmed causes and changes

During physical Pattern playback, native opening/hovering of each Piano selector produced 60
workflow notifications and 60 ItemsSource replacements in the baseline observation. Native selection
was not accepted; one attempt refused an unsettled popup hit target. `Refresh()` replaced both
sources even when their strings were identical. Content comparison now retains unchanged sources;
selection is assigned only when the canvas value differs. Localization still replaces changed Snap
labels. Transport observations continue normally. No timer, forced close or transport suppression
was introduced.

At a 1100-DIP Rack, the baseline one-bar thumbnail was 834 DIP wide versus 462 DIP for pads, and
began 5 DIP later. Narrowing changed it to 452 DIP while pads stayed 462. Its stretch container,
button inset and Bounds.Width normalization disagreed with the fixed step/beat/bar groups.
`RackTimelineGeometry` now supplies existing shared metrics to step groups, ruler and thumbnails.
The thumbnail uses intrinsic Pattern width and zero horizontal button inset. Note endpoints
interpolate between actual step starts, including rhythmic gaps, with no trailing Pattern gap.
The existing common ScrollViewer owns all horizontal movement. The preview remains read-only.

Only Desktop presentation and verification code changes. Canonical music, serialization/reader,
Undo ownership, audio engine, DSP, Mixer and Live Gain are unchanged.

## Verification

- Focused Release build: zero warnings/errors; **240/240** automated cases, zero skips, **11.293 s reported test
  duration**. Six new
  RackTimeline cases cover 1/2/4/8/32 bars, exact end/quarter/bar
  positions, fractional timing and crossing notes. Existing Piano, step, workspace, design,
  presentation and version cases are included.
- Native selector matrix: **48/48**, zero source replacements, RU/EN × Dark/Light × 1080/520-DIP floating Piano, three
  values in each
  selector; unchanged sources/realized hovered rows/screen positions, mouse acceptance, actual
  SnapTicks/DefaultDuration, Part switching, exact snapshot and Undo preservation. Constrained
  Popup wheel scrolling additionally selects free Snap and whole-note Length. Every long hold
  includes at least 60 real workflow notifications. The accepted combined run completes in 141.554 s.
- Rack matrix: 60 actual rendered layouts, 1/2/4/8/32 bars × 1100/650/420 DIP × both languages/themes.
  Every pad/ruler/thumbnail subdivision is compared, including quarter/bar boundaries and Pattern
  end. Widths are 462/942/1902/3822/15342 DIP. Native scrollbar drags move all three together;
  far-end captures include the final note. Pixel-rounded scrolling permits 1 DIP against the
  fractional scroll offset; relative row alignment remains within 0.1 DIP.
- Native note draw/move/Snap/Length/Undo/Redo, skipped-cell step paint/one Undo/Escape, exact preview
  opening, floating Rack resize 420×390 → 520×440 and layout restoration have separate receipts.
  The 32-bar thumbnail remains 15342 DIP after native resizing.
- Steinberg UR12, 44.1 kHz stereo: playing selector checks retain execution epoch, advance renderer
  position and require no output failure. A short four-repeat Song fixture also holds a Popup
  over dozens of observations, selects by mouse and preserves music/history and continuous output.
- Existing `piano` passes routed drawing/move/resize/velocity/properties, Save/Open/export, retained
  pane restoration and eight localized/theme/size layouts with actual field alignment. Existing
  `rack-workflow native audio` passes menu Duplicate, reorder, Ctrl-drag, history, exact/fader Gain,
  source opening, portable identity/order and playing reorder; populated/empty states and field
  alignment cover both languages/themes at ordinary/reduced sizes. Its first native menu Duplicate
  attempt times out; the unchanged repeat passes.
- Full Release solution build: **1.527 s CLI**, zero warnings/errors, valid incremental outputs.
  SDK 10.0.402, runtime 10.0.13, xUnit v3/MTP, one test thread; unchanged restore inputs reuse assets.
  The first full run passes **1844/1845**, zero skips (**74.186 s CLI**): unchanged
  `WarmPreviewProcessorAllocatesNothingAcrossVariablePackets` measures 3736 bytes instead of zero.
  Its isolated unchanged repeat passes **1/1** (**1.368 s CLI**). Cause remains unestablished;
  no threshold, DSP code or environment setting is changed. The final unchanged full repeat passes **1845/1845**, zero
  skips, **65.868 s CLI** (65.315 s module; 65.462 s reported total).
  This is the successful L3; the earlier failed run does not count as acceptance.

## Evidence and limits

`VisualCheck piano-rack <local-output> --timeout-seconds=300` is the reusable native regression;
`geometry` skips the selector matrix for focused reruns. Generated fixtures, actual Windows screen
captures, rendered images and JSON remain ignored under `.artifacts/`, with ordinary rolling
retention. Baseline observations above were read before their disposable runs rotated. Reference
images and comparative research remain private and are not distributed.

The accepted combined evidence is `.artifacts/piano-rack-accepted/20261016T221513653-46bcf36b94ad465aab565336c2a8cf42`.
Representative real Popup, narrow Rack, multi-bar and far-end captures were inspected in both
themes/languages, together with neighbouring populated/empty windows. Automatic retention removes
older managed disposable captures under its existing policy; ignored files have no Git recovery.

The old `steps` route stops at its previously documented reduced Tempo reachability assertion;
it is not reported as a complete pass. An attempted reuse of its native helper rejected grid focus
in the long fixture; a child-target movement probe also used the wrong capture owner. Dedicated
paint/cancel checks use the real capturing row. Initial horizontal-wheel/offset probes were replaced
by actual scrollbar drags and explicit pixel-rounding checks. One geometry-only capture displayed
Audio output failed after its short setup playback; its cause was not established. Later explicit
Song playback and accepted playing checks require a healthy output independently. No audio fix or
general pressure/acoustic/platform acceptance is claimed.

All changes remain unstaged. No index, history, remote or release operation is authorized/performed.
