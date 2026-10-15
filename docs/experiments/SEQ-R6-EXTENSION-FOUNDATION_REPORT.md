# SEQ-R6-EXTENSION-FOUNDATION

Date: 2026-10-15. Scope: trusted local content packages and the asynchronous generator lifetime/
acceptance boundary. Product checkpoint **0.1.7.0005 → 0.1.7.0006 (BUILD)**. No new project-reader gate,
public SDK, external realtime host or Sample Lab is claimed. [Extensions](../EXTENSIONS.md#implemented-r6-local-foundation)
owns the implemented contract; this report owns observations and limits.

## Delivered behavior

The application deployment has a real optional **Found Sounds** data package: three original short
PCM16 WAVs, metadata, provenance and deterministic reproduction source. Browser discovers manifests,
checks concrete required/optional platform contracts without executing code, exposes package identity,
version/purpose/availability and diagnostics, and browses enabled content through its existing list.
Enable/Disable persists independently of music. Unknown/incompatible/damaged/duplicate manifests and
unregistered executable providers remain unavailable. DLLs are never scanned or loaded.

Package, capability, active use/session and accepted resource have separate identities/lifetimes.
Known active work/editor sessions block disable. Successful retirement withdraws new leases before
awaited disposal; failed disposal retains references and refuses refresh/re-enable until cleanup.
Activation has a caller deadline; cancelled/late activation is supervised through actual termination.
This is cooperative trusted in-process code, not a sandbox or ability to kill a stuck native plugin.

Pack imports use existing Rack/Playlist managed acceptance and ordinary history. Deterministic cases
remove original pack files, Undo/Redo, Save As/Open, decode accepted media and export nonzero music
while the package is disabled/absent. There is no new Browser, audio resource store, renderer or Undo.

The functional generator is a test-only provider producing real WAV candidates. It exercises schema
validation, serial off-callback work, cancellation/errors, supersession, stale contexts, late result
disposal, editor/use leases and document/application joins. The Desktop seam explicitly accepts through
managed Rack import with captured destination and final identity/context gate. Any intervening musical
edit cancels work/disposes the now-stale candidate; New/Open/Close join the session before retirement.
Successful acceptance adds one history entry, survives generator disable and portable reopen, and
cannot be applied twice through the old generation.

R7 still needs real creative families, reference/variation/lock declarations, bounded temporary
history/comparison, standalone/contextual UI and temporary audition through actual musical processing.
R6 supplies the ownership and commit boundary; it supplies no user-facing generator or universal
similarity/recipe/realtime contract. Existing unknown/opaque state preservation and affected-operation
blocking remain covered by the existing persistence/graph execution suite.

## Automated verification

Windows x64; SDK 10.0.401, .NET runtime 10.0.12; existing xUnit 3.2.2 / Microsoft.Testing.Platform.
No package upgrades, new assemblies or test platform changes.

```powershell
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore --max-threads 1 --report-xunit-trx --results-directory "$PWD/.artifacts/r6-extensions/tests-closure"
```

Final L3: **1314/1314 passed, zero failures/skips**, **63.176 s** CLI summary. Full Release build:
zero warnings/errors, **2.13 s** incremental MSBuild summary (**2.63 s** shell wall time). Earlier full
runs passed 1314 in 69.584 s and 67.154 s; they preceded final lifetime guards and are not used
instead of the final run. The last change separates provider readiness from activation authority: a
cancelled/deadline-expired caller cannot publish a ready provider; rejection joins preparation and
retires it before retry. Extension/Browser checks pass 49/49 in 2.483 s after this change. The initial full build took 10.24 s. Restore was unnecessary because package/
SDK inputs and restored assets were unchanged. Final actual runtime metadata is 0.1.7.0006; product
version tests participate in L3. Cadence resets to zero.

Focused verification: the initial six affected classes pass **255/255**; extension/Browser/workspace
follow-up passes **76/76**; the final extension/Browser/Desktop lifetime selection passes **92/92** in
**3.283 s**. A direct extension/workspace selection passes 59/59 after nonzero absent-pack exports
were added. These focused runs remain narrower than L3.

The first new-case run exposed five failures: `InvalidDataException` was not included in the metadata
error boundary. Explicit handling now reports damaged metadata/paths rather than escaping discovery.
Compiler/analyzer failures during implementation (accessibility, inherited Name, missing fixture API
and async/collection assertion forms) were corrected before final builds; failed/stale-output runs
are not counted as final verification. No assertion or existing failure policy was weakened.

## Rendered GUI and input evidence

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll extensions "$PWD/.artifacts/r6-extensions/visual-final"
```

The real production MainWindow/Browser and shipped pack pass the routed scenario: discovery, actual
decoded waveform, Rack and Playlist button imports (two managed resources/two history entries),
disable/re-enable, source withdrawal, and Save/Open with the package disabled. Four **RU/EN ×
Dark/Light** captures pass; each ordinary layout retains a 298-DIP sound list. Reduced 640×600
expanded/collapsed disclosure checks retain list access through local scroll areas. Original package
names are metadata; host labels/diagnostics/actions remain localized. Onest and semantic resources
are inherited; no shared templates/styles or new parameter rows were changed.

Initial image review found an expanded small-window disclosure displacing the list. Bounded local
disclosure/preview scrolling fixes it. Ordinary-height preview keeps its established full controls.
The early native attempt additionally found a pre-existing Browser-open-before-arrange null dock
width; source correction uses the existing clamp until the first reflow, with a regression test.

**Native Windows input is not accepted.** After that fix, a guarded native attempt reached the first
Browse sounds check with no pack rows. A Rider debugger attachment to a separate task-owned check
confirmed `ExtensionFoundationAsync` at the post-click assertion, `list.ItemCount = 0`,
`browser._currentPath = null`, `browser._message = Browser.NoResources`, the selected package
`Found Sounds · 1.0.0`, and completed extension/Browser pending tasks. The awaited Windows click did
not establish navigation. Its delivery cause is unresolved; routed success is not evidence that
Windows input worked. Debugger breakpoints were removed, the task-owned session was stopped, and the
temporary attachment delay was removed. User breakpoint settings were not changed.

No new physical-output, acoustic, pressure, accessibility, mixed-DPI or Linux/macOS acceptance is
claimed. Deterministic decoded/rendered audio and actual-window rendering are separate evidence.

Static checks verify **1571 local Markdown links**, without missing files/anchors, and `git diff --check`.
The reproduction script regenerates all three shipped WAVs byte-for-byte with identical SHA-256.

## Version, documentation and retained material

BUILD **0006** records a substantial verified module foundation within the current capability line.
PATCH is not claimed: R7's creative product is absent and native delivery evidence remains open.
Current contracts, narrowed Q-010/Q-024/Q-054/Q-055/Q-067, R7 handoff, source provenance and user guides
are updated. Completed R6 scope/rationale moves to the archive; no future external-host decisions
become current release obligations.

The Git baseline was clean. All final tracked/untracked changes belong to this task: production
sources, permanent tests/tooling, three runtime WAVs/manifest/provenance, and documentation. Ignored
`.artifacts/r6-extensions` retains disposable screenshots, isolated projects/config/media, TRX and
debugger attempt outputs for review under existing retention; these are not runtime dependencies.
A checked task-scoped recursive cleanup of the stopped debugger run and its logs was rejected by
automatic approval review (`blocked by policy`); those ignored files are intentionally retained
rather than deleting through another execution path. Runtime/test resources remain permanent.
No user project/demo, staging index or Git history was modified. No stage, commit, push or tag.
