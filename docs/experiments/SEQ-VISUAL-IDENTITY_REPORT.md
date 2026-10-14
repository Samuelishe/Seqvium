# SEQ-VISUAL-IDENTITY — 2026-10-14

Role: Bounded visual identity integration evidence and limits.
Audience: Contributors reviewing artwork, resource packaging and actual desktop presentation.
Authority: Observations only; [brand kit](../../brand/README.md), [UI design](../UI_DESIGN.md),
[Design System](../DESIGN_SYSTEM.md) and source own the current visual contract.

## Outcome and design judgement

Direction 09 becomes Seqvium's primary identity. Its displaced cyan/teal/slate/mint fragments,
opposing slopes and asymmetric overlaps express sequence without turning every musical object
turquoise. Manually constructed SVG paths replace the generated reference's raster noise and
lighting; these are disclosed interpretations, not recovered original vectors or automatic traces.
Dedicated small/tiny masters retain recognition at 20–40 and 16 px. Both 01's displaced lanes and
07's continuous S ribbon retain their full source boards, editable reconstructions and previews.

Onest Black 900 most closely reproduces the dense, rounded wordmark impression in the visual
comparison with Manrope ExtraBold 800. This is a design judgement, not identification of an
unknown generated typeface. Onest Regular/SemiBold serve UI text/headings and JetBrains Mono
Regular serves exact fields. The Noto Sans Symbols music subset removes system dependence for ♯.
All font sources, transformations, hashes and complete OFL licenses are retained.

Desktop title/About, inherited UI/drawn typography, semantic action/focus accents, EXE and window
icons change. Musical palettes, geometry/density, DSP, project format and workflows do not change.
README receives original brand artwork and real lossless captures of the bundled demo. Social
Preview is ready for the owner's GitHub settings upload; no external setting or Git index is changed.

## Verification

Windows 11 x64, repository SDK 10.0.401, Avalonia 12.1.3. Scope is **L0 + focused L1/L2 and visual/native
resource L4**, not L3/full regression, acoustic acceptance or a packaged release.

| Check | Actual evidence |
| --- | --- |
| Affected Release build | Tests project and references, `--no-restore -m:1`: 9.22 s, zero warnings/errors. Final verifier-only rebuild: about 2.3 s, zero warnings/errors |
| Focused tests | `HostPresentationTests`, `DesignSystemTests`, `DesktopFoundationGestureTests`, `ProductVersionTests`: **60/60**, zero skips/failures, 15.741 s final CLI summary |
| Identity GUI | Actual MainWindow with read-only Neon Orchard, English/Russian × Dark/Light; Playlist/Rack/Mixer, melodic Piano, About, 1920×1040 and 1400×900 renders; music/history unchanged and no audio session |
| Font resolution | Actual glyph streams at 400/600/900 Onest and 400 Mono hash-match embedded asset streams; no weight simulations; complete RU/EN alphabet and numeric/minus coverage; ♯ fallback stream matches bundled subset |
| Component catalog | Both themes, ordinary and synthetic fixture states, live shared resource propagation/restoration: passes, 5.69 s scenario |
| Broader field/window matrix | RU/EN × Dark/Light with ordinary/reduced/minimum/maximized and pane sizes, toolbar/Graph/Node Editor actual TextPresenter alignment: passes, 24.05 s scenario |
| EXE icons | Win32 PrivateExtractIcons extracts all nine 16–256 sizes from built apphost; alpha and opaque RGB match target exports exactly; translucent-edge RGB differs by at most 5/255 after native premultiplication/readback |
| Live application | Real Desktop EXE launches and closes gracefully without Play; WM_GETICON returns actual 16 px small and 24 px large icons at this display scale |
| Shortcut | Disposable Shell `.lnk` targets built EXE; SHGetFileInfo returns nonempty actual small/large icons with standard shortcut overlay |
| Reproduction | Offline static font/export regeneration, source/derived hashes, ICO/ICNS dimensions, documentation links and Git diff checks; results recorded with the kit |

Focused test command (MTP/xUnit on .NET 10):

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*HostPresentationTests" --filter-class "*DesignSystemTests" --filter-class "*DesktopFoundationGestureTests" --filter-class "*ProductVersionTests" --report-xunit-trx --results-directory .artifacts/identity/tests-final
```

Final successful production build/test + selected catalog/matrix/identity scenarios fit the ordinary
120-second verification target (roughly 70–85 seconds combined, excluding preparation, visual review,
font research/downloads and intermediate repairs). No new packages enter the .NET application graph;
existing valid restore assets are reused. Earlier test/build passes are not added to the final count.

## Failures corrected and remaining limits

The initial font audit failed because Onest/Mono do not contain ♯. A first candidate, Noto Sans
Symbols 2, also lacked that block; its task-owned files were removed. The shipped subset is from
Noto Sans Symbols and the final actual fallback stream is checked. The first About content capture
omitted the popup's background; final captures render the real FlyoutPresenter instead.
These initial failures are not counted as passing matrix evidence.

The real EXE/window/Shell icon data is verified and visually inspected. A separate attempt to capture
the taskbar/Alt+Tab screen refused native input because the exact Seqvium window was not foreground.
No foreground forcing or input into another app occurred. Thus actual **taskbar/Alt+Tab composition
and Explorer cache refresh are not visually established**, despite the correct native icon handles.
Some GUI screen captures pass their foreground guard; others are explicitly labelled rendered-window
evidence only. Fixture/routed setup is not native musical authoring.

Linux/macOS packaging/runtime, high/mixed DPI and cross-monitor behavior remain unverified. ICNS/SVG/
PNG readiness is an asset claim. Extended user scripts/symbols outside RU/EN/common text and the
bounded music subset can still use platform fallback. No audio/device tests are needed for unchanged
DSP, and none are claimed. Owner aesthetic approval remains a separate judgement.

## Retention and checkpoint

Intentional product content: [brand kit](../../brand/README.md), Desktop runtime assets/fonts/licenses,
publication images/capture provenance and reusable tools. Only three selected source boards are retained;
the Desktop collection remains untouched. The overall kit is a few MiB, not the entire reference collection.

Local ignored `.artifacts/identity/` retains TRX, font/field/resource JSON, GUI renders and Win32 icon
captures for review under normal rolling retention. `.scratch/identity/` retains the local Python
artwork environment for further brand edits; it is not a build dependency and may be removed after
review. No transient capture is linked as if it were a permanent repository resource.

**0.1.7.0002 → 0.1.7.0003**, one BUILD for a verified product-resource/presentation checkpoint.
No new music-creation capability qualifies for PATCH. MAJOR/MINOR stay owner-controlled. Last L3
remains SEQ-R10-SONG-STRUCTURE; completed tasks since it become three. No staging, commit, push or tag.
