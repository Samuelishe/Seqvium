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

## Independent review follow-up

Date: 2026-10-15. **0.1.7.0006 → 0.1.7.0007**, one BUILD for two bounded R6 corrections and
their regressions. There is no new capability/PATCH, R7 implementation, plugin framework, reader
change, full-suite rerun or renewed stress campaign. The preceding L3 remains the baseline;
completed-task cadence is now one.

Browser previously displayed `Found Sounds` but sent that text to filesystem navigation on Enter.
Virtual pack/Project resources addresses are now read-only labels; Enter uses the current internal
location. Ordinary folder addresses remain editable. The real-window routed check focuses the
address field before Enter and verifies pack rows, Project resources, Up/Back, editable filesystem
navigation and return to the same pack. It also passes the existing managed Rack/Playlist import,
disable/re-enable, disabled-pack reopen and RU/EN Dark/Light/reduced-window checks. Ordinary sound
list height remains 298 DIP; shared tokens/templates are unchanged.

Discovery now calls `RejectLink` on `extension.json` before opening it, under the existing damaged
manifest diagnostic boundary. The regression moves valid metadata outside the deployment root and
links the package's manifest to it; discovery returns Damaged with no admitted manifest and refuses
activation. Windows file symlink creation succeeded here: the test ran without a skip. Platforms
that deny symlink creation report an explicit skip; no other-platform execution is claimed.

Final affected Release build: zero warnings/errors, **7.94 s** MSBuild summary. Final focused
L1/L2: **108/108 passed, zero failures/skips**, **16.301 s** CLI summary, including extension,
Browser, workspace and actual product metadata checks. Commands:

```powershell
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class '*ExtensionFoundationTests' --filter-class '*SampleBrowserTests' --filter-class '*WorkspaceStateTests' --filter-class '*ProductVersionTests' --max-threads 1 --report-xunit-trx --results-directory .artifacts/r6-followup/tests-final
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll extensions .artifacts/r6-followup/routed-final --timeout-seconds=45
```

The routed window run preceded the metadata-only BUILD change; final metadata tests cover 0007.
During verification, one nullable observer expression was corrected after a compiler error. Two
early routed checks incorrectly sent Enter while the file list still had focus; the fixture now
focuses the address field. Those failed checks are not counted as passes.

### Native input remains bounded evidence

Exactly one new native attempt was made, on a task-owned process with a temporary 20-second pause
before input to attach Rider. A test-tool observer recorded PointerPressed and PointerReleased
through `Extension.Sounds`, **one Click**, completed Browser work,
`seqvium-pack:seqvium.found-sounds` and **three rows**. The debugger paused at the immediate
post-click assertion in `ExtensionFoundationAsync` on the UI continuation; its stack and evaluation
confirmed `list.ItemCount = 3` and `browser._currentPath = "seqvium-pack:seqvium.found-sounds"`.

This localizes a successful Pointer → Click → Browse path, but does not reproduce or explain the
original zero-row failure. The attachment pause changes startup timing. **Ordinary Windows native
acceptance therefore remains unverified.** No speculative input/production fix was applied and no
repeated native attempts followed. The run stopped at that assertion before further native imports.
Agent breakpoints were removed, the task-owned session stopped, user breakpoint settings preserved,
and the temporary attachment pause removed before the final build.

The reusable tool now writes `extension-browse-input.json` around the first Browse action so future
evidence distinguishes pointer delivery, Click and the resulting Browser state. Address keyboard
regressions remain routed in either tool mode. Ignored `.artifacts/r6-followup` retains TRX, successful
routed renders/independent media and failed/debugger-attempt observations for review; none is a
runtime dependency. No untracked product/resource files were created. Git started clean; all tracked
changes belong to this follow-up. No staging, commit, push or tag was performed.
Static checks pass: 258 local links in changed documentation, no missing targets/anchors, and
`git diff --check`. Final status has 13 modified tracked files, an unchanged staging index and no
untracked files; ignored verification evidence is intentionally retained as described above.
