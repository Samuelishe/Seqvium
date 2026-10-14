# Third-party provenance and evaluation

Role: Provenance ledger and evaluation boundary for external components and material.
Read when: Evaluating, introducing, upgrading, replacing, or removing a dependency, service, action, or asset.
Authoritative for: Project licensing/provenance boundary, introduced material, candidate status, obligations, and future
entry fields.
Not authoritative for: Authoritative license text, final technical choices, subsystem design, or installed-version
locks.

## Visual identity fonts and reference material

The [brand kit](../brand/README.md) retains owner-supplied generated raster references for concepts
09, 01 and 07 as optimized WebP. The [reference manifest](../brand/references/manifest.json) records
original/retained hashes and transformations. Boards are design context, not production icons,
shipped UI screenshots or evidence of platform/website ownership. New manually reconstructed SVG
geometry, export tooling and publication composition are Seqvium contribution material under the
root Apache-2.0 license. No third-party DAW graphics are extracted or unknown original vectors claimed.

These font binaries retain **SIL Open Font License 1.1** independently of the application's license.
Each full upstream OFL/copyright stays with its source; each runtime family has a license copied
into build/publish output. The [font manifest](../brand/fonts/manifest.json) owns pinned Google Fonts
revisions, source URLs, SHA-256 and transformations.

| Font / authors | Actual distributed material and official source |
| --- | --- |
| Onest / The Onest Project Authors | [Official source](https://github.com/simpals/onest); variable source plus derived static 400/600/900 faces in Desktop |
| Manrope / The Manrope Project Authors, Mikhail Sharanda and contributors | [Official source](https://github.com/googlefonts/manrope); variable source retained for comparison, not an application dependency |
| JetBrains Mono / The JetBrains Mono Project Authors | [Official source](https://github.com/JetBrains/JetBrainsMono); variable source plus derived static Regular 400 in Desktop |
| Noto Sans Symbols / The Noto Project Authors | [Official source](https://github.com/notofonts/symbols); variable source instantiated at 400 and subset to U+2669–U+266F for embedded music symbols |

[prepare-fonts.py](../tools/branding/prepare-fonts.py) reproduces the static instances/subset;
[export.py](../tools/branding/export.py) creates outlined lettering and artwork. No fonts are
installed globally and no paid/proprietary typeface is copied. The unknown generated wordmark is
not attributed to a guessed font. Font software retains OFL with its complete bundled terms;
the application retains its separate license. fontTools, Pillow and resvg-py are optional local
artwork tooling, not redistributed application libraries. Normal .NET builds use checked-in exports.

## Project license and material boundaries

Seqvium-authored work is licensed under **Apache License, Version 2.0** (`Apache-2.0`) unless a
file/component explicitly states another license. This covers Seqvium-authored code, documentation,
and assets. The root [LICENSE](../LICENSE) is the primary authoritative project license artifact,
downloaded unchanged from the [official Apache text](https://www.apache.org/licenses/LICENSE-2.0.txt).

Apache-2.0 permits commercial use, modification, redistribution, and derivative works under its terms,
includes an explicit contributor patent grant, and does not require derivative Seqvium code to remain
open source. It is a mature [OSI-approved license](https://opensource.org/license/apache-2.0) for
collaborative permissive FOSS; no extra use or redistribution restrictions are added.

**Seqvium's Apache-2.0 license does not relicense third-party material.**

Third-party executable plugins are independently authored software. Compatibility with Seqvium does
not make them Seqvium-authored or warrantied by the Seqvium project. [EXTENSIONS](EXTENSIONS.md) owns
the technical host/plugin responsibility boundary. Seqvium is provided on the `AS IS` basis, without
warranties, and with the limitation of liability as specified by Apache-2.0, subject to applicable law.
Root [LICENSE](../LICENSE) is authoritative; this summary adds no separate EULA or legal restrictions.

| Material                          | Licensing boundary                                                                                 |
|-----------------------------------|----------------------------------------------------------------------------------------------------|
| Seqvium-authored code/docs/assets | Apache-2.0 unless explicitly identified otherwise                                                  |
| Third-party dependencies          | Their own licenses/terms, including relevant transitive and distribution obligations               |
| Third-party assets/content        | Their own licenses/terms and redistribution rights, independently of application code              |
| Generated/derived material        | Source/input obligations may remain; generation, transformation, or resampling does not clear them |

Evaluate each future dependency/asset for compatibility with the **distributed product** before
distribution. OSI approval alone does not imply all licenses can be combined with Apache-2.0.
Copyleft, SDK terms, codec/patent constraints, and redistribution requirements need concrete evaluation.

### Notices and future source identification

No current attribution content requires a root `NOTICE`; do not create one for ceremony. Apache-2.0
NOTICE handling becomes relevant when Seqvium has its own notices or redistributed components require
attribution treatment. Evaluate license/notice obligations before distribution when dependencies/assets
are introduced. This ledger owns provenance/evaluation and **does not substitute for legally required
bundled licenses or notices**.

When actual source files are created, prefer concise `SPDX-License-Identifier: Apache-2.0` where
file-level identification is useful. Do not add large per-file boilerplate by default unless later
tooling/legal policy establishes a concrete need. Do not add SPDX headers to all Markdown; root LICENSE
remains the primary license artifact.

## Introduced

### Audio Clip codec adapter

Desktop references [NAudio.Wasapi 2.2.1](https://www.nuget.org/packages/NAudio.Wasapi/2.2.1), with
transitive NAudio.Core 2.2.1, by Mark Heath and contributors. The Desktop manifest and lock file own
versions and package hashes; Tests/VisualCheck inherit the same graph. The adapter uses WaveFileReader
for PCM WAV and MediaFoundationReader for MP3/FLAC normalization on a worker. Core stays BCL-only,
and the existing Seqvium WASAPI output/Mixer remain the realtime path; this does not introduce a
second output system. No upstream source or codec executable is vendored.

The [tagged MIT license](https://github.com/naudio/NAudio/blob/v2.2.1/license.txt) carries Copyright
2020 Mark Heath. Preserve that copyright and the complete permission/warranty text with redistributed
NAudio binaries. The packages retain MIT, not Seqvium's Apache license; actual delivered notice
bundles remain a distribution gate. Media Foundation is an installed **Windows** service, not a
universal cross-platform decoder. MP3/FLAC availability depends on the Windows edition/components
and installed codecs. No Microsoft DLL, FFmpeg, private recording or sample pack is redistributed.
Local original pad fixtures and private-library copies are outside Git and are not shipped content.

### Neon Orchard demo media

The official [editable demo](../examples/Neon%20Orchard/README.md) distributes six unique WAVs.
The complete per-file [inventory](../examples/Neon%20Orchard/MEDIA.json) owns relative locators,
byte counts, SHA-256, source members and transformations; no private absolute library paths are
required. Its project and original synthesis are Seqvium contribution material under Apache-2.0.

Four WAVs are VSCO 2 **Community Edition**, recorded by Sam Gossner and Simon Dalzell, with sample
cutting by Elan Hickler / Soundemote. The [official publisher](https://versilian-studios.com/vsco-community/)
permits reuse and redistribution under CC0-1.0. Each distributed file was downloaded independently
from [upstream revision 440300901dfe9275fd84e0b7763af1f8443ae62e](https://github.com/sgossner/VSCO-2-CE/tree/440300901dfe9275fd84e0b7763af1f8443ae62e)
on 2026-10-14 and its SHA-256 matched the managed source copy exactly. Members are upright piano
`Player_dyn1_rr1_018.wav`, flute `LDFlute_expvib_A3_v1_1.wav`, and solo violin
`LLVln_Pizz_A3_p_RR1.wav` / `LLVln_Pizz_A3_f_RR1.wav`. The unchanged pinned upstream
[CC0 text](../examples/Neon%20Orchard/licenses/VSCO2-CE-CC0.txt) is bundled. CC0 permits separate
WAV redistribution and modification; these recordings retain CC0 and are not Apache-relicensed.

MusicRadar's [bass synth pack](https://www.musicradar.com/music-tech/samples/sampleradar-250-free-bass-synth-samples)
and [DnB FX pack](https://www.musicradar.com/news/tech/sampleradar-100-free-drum-n-bass-fx-samples-444650)
permit musical use but prohibit sample redistribution. The source's `BassSynth_Moe-Dee_Multi_A1.wav`
and `DNB_FX_13.wav`, including duplicate managed copies, are excluded. Merely placing them inside
an editable project or converting/resampling them would not establish redistribution permission.
They are replaced by original harmonic bass and noise/chirp transition WAVs from the bundled
[generator](../examples/Neon%20Orchard/generate-sounds.py), which reads no source audio. No MusicRadar
audio, pack, private mix, reference image or personal catalog is distributed with this demo.

Future additions/replacements require the same per-file provenance and redistribution review.
The demo is intentionally musical product content; its managed WAVs and frozen project snapshot
are permanent repository resources, not disposable verification output.

### Mixer DSP and original instrument graphics

Mixer introduces no package, vendored code or external bitmap/icon/texture. Filter coefficients and
analytical response are independently implemented in Core; the mathematical reference is the
[W3C Audio EQ Cookbook](https://www.w3.org/TR/audio-eq-cookbook/). This is algorithm-reference
provenance, not a claim that third-party source or assets were distributed. Mixer vectors, metal-cap
gradients and bounded chassis grain are Seqvium-authored under the root license. Private reference
screenshots and the user's music/sample library remain outside Git and are not product content.

Installed Avalonia Fluent templates remain the existing dependency. Resource aliases customize their
rendering/input affordances; no theme source is vendored or new UI framework introduced.

R1 adopts the installed .NET 10 SDK/runtime for its framework-dependent canonical library and managed
tests. Core production code remains BCL-only; R3-F1 Desktop dependencies are recorded separately below.
The test project uses the components below;
its [manifest](../tests/Seqvium.Tests/Seqvium.Tests.csproj) and
[lock file](../tests/Seqvium.Tests/packages.lock.json) own versions and the complete dependency graph.
No third-party source/assets are vendored. Local binaries are ignored and no product distribution is
created. Package licenses were checked in the restored package metadata, with official sources linked
below; redistribution of test binaries must preserve the applicable licenses/notices.

F2's Seqvium-authored Windows adapter calls installed WASAPI/COM/MMCSS/Kernel32 services directly
through private managed interop declarations. No OS DLL, native sampler binary, third-party wrapper or
new NuGet dependency is distributed. New production/harness lock files contain project references only;
the SDK remains root-manifest-owned. The verification WAV is generated mathematically in
[DeviceCheck](../tools/Seqvium.DeviceCheck/Program.cs), imported into an owned fixture directory and
removed after joined/released execution; no external music or audio asset is introduced.

| Component / author                                                                                    | Form, purpose and source                                                                                                                                                                                                                      | License/distribution evaluation                                                                                                                                                           | Version authority                                                                  |
|-------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| xUnit.net v3 and analyzers / .NET Foundation and contributors                                         | Test-only managed framework, assertions, runners and compiler analyzers; [official source/license](https://github.com/xunit/xunit/blob/main/LICENSE), [MTP integration](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) | Apache-2.0 in the selected packages, compatible with Seqvium's license. Preserve license and any applicable notices if redistributing; no test package is a production-library dependency | Test manifest selects `xunit.v3.mtp-v1`; lock fixes xUnit components and analyzers |
| Microsoft.Testing.Platform, MSBuild, TrxReport.Abstractions and Telemetry / Microsoft                 | Test-only managed execution integration; [official source/license](https://github.com/microsoft/testfx/blob/main/LICENSE)                                                                                                                     | MIT; retain copyright/license on redistribution. Telemetry is a framework default, not a Seqvium application service; developers can opt out with `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`    | Transitive test lock entries                                                       |
| Microsoft.ApplicationInsights, Microsoft.Bcl.AsyncInterfaces and Microsoft.Win32.Registry / Microsoft | Test-only transitive telemetry/BCL compatibility packages; [Application Insights source](https://github.com/microsoft/ApplicationInsights-dotnet), [runtime source/license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)          | Installed package metadata declares MIT for all three. Preserve applicable copyright/license on redistribution; no application telemetry or Windows-only domain dependency is introduced  | Transitive test lock entries                                                       |

SEQ-R0 separately uses installed tools/runtime and Windows system APIs, without NuGet audio packages,
downloaded audio libraries or assets. Compiled probe outputs/notices remain local ignored artifacts;
the following entries describe its independent provenance and obligations.

| Component / author                                     | Form, purpose and source                                                                                                                                   | License/distribution evaluation                                                                                                                                                                                                                                                                               | Version authority                                                                                                                                                                              |
|--------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| .NET / .NET Foundation and contributors                | Installed SDK/runtime for R1 and framework-dependent experimental host; [official source/license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) | Runtime source is MIT; no runtime is bundled here. Future self-contained distribution must retain applicable runtime/transitive notices and review its actual contents                                                                                                                                        | Production [global.json](../global.json), independent experiment-local [global.json](../experiments/seq-r0/global.json); actual experimental runtime in [report](experiments/SEQ-R0_REPORT.md) |
| GCC / Free Software Foundation, MSYS2 UCRT64 packaging | Existing developer C compiler; [GCC](https://gcc.gnu.org/), [MSYS2](https://www.msys2.org/)                                                                | Compiler GPLv3+, not redistributed. Eligible covered runtime code uses GPLv3 with GCC Runtime Library Exception 3.1; installed `COPYING.RUNTIME` and `COPYING3` are copied beside DLL output. No compiler/global installation performed                                                                       | [build.ps1](../experiments/seq-r0/build.ps1) declares compiler/options; observed package identity in report                                                                                    |
| MinGW-w64 headers/CRT / MinGW-w64 contributors         | Existing Windows API headers/import libraries and linked runtime; [official source/licensing](https://www.mingw-w64.org/support/#licensing)                | Mixed runtime notices include permissive ZPL/BSD/public-domain terms and component-specific exceptions. Build copies the complete installed `COPYING.MinGW-w64-runtime.txt`, preserving its full obligations rather than assuming one umbrella license. Future distribution must audit actual linked contents | Build script uses installed MSYS2 UCRT64; exact tested headers/CRT packages in report                                                                                                          |
| Windows WASAPI/COM/MMCSS/UCRT / Microsoft              | Installed OS services, not redistributed DLLs; [official API](https://learn.microsoft.com/en-us/windows/win32/coreaudio/rendering-a-stream)                | Probe calls OS APIs. System DLLs are not bundled or relicensed. Actual native import list, including a private CRT API-set import, is reported; clean supported distribution remains unvalidated                                                                                                              | OS/API and import inspection in report                                                                                                                                                         |

The native probe's C scheduling/DSP and generated triangle/event sequence are Seqvium-authored
Apache-2.0 work. Local native loading succeeds without MSYS2 runtime DLL imports; that does not establish
clean-machine deployment or general legal/distribution acceptance. No root NOTICE is added in lieu of
the applicable runtime license files.

## R3-F1 desktop dependencies

Avalonia is explicitly adopted for the bounded desktop host, not merely proposed. The actual
[desktop manifest](../src/Seqvium.Desktop/Seqvium.Desktop.csproj) selects `Avalonia.Desktop` and
`Avalonia.Themes.Fluent`; its [lock](../src/Seqvium.Desktop/packages.lock.json) owns all exact resolved
revisions/content hashes. Evaluation on 2026-10-09 verified stable 12.1.3 availability, net10.0 assets,
MIT license and real Windows startup. The [official package](https://www.nuget.org/packages/Avalonia.Desktop/12.1.3)
and [tagged license](https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/licence.md) support adoption.
The test lock also contains this graph because display-independent shell tests reference Desktop.
No global templates/tools, copied framework source, external theme engine, icon library or font package
is introduced. Fluent is the framework's base controls/theme, customized through small host styles.

| Component / author                                                                                                                                          | Form/purpose and official source                                                                                                                    | License and redistribution implications                                                                                                                                                                                        |
|-------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Avalonia Desktop, Fluent, Win32/X11/Native, Skia/HarfBuzz integration, FreeDesktop/AT-SPI, Remote.Protocol and BuildServices / Avalonia Team, AvaloniaUI OÜ | Managed desktop/rendering/accessibility/build components and macOS native backend; [source](https://github.com/AvaloniaUI/Avalonia)                 | Restored package metadata declares MIT. Retain applicable copyrights/licenses in redistributed binaries; a protocol dependency does not mean Seqvium activates remote diagnostics                                              |
| Avalonia.Angle.Windows.Natives / Avalonia Team packaging, ANGLE contributors                                                                                | Native Windows GL/ES rendering binaries; [ANGLE](https://github.com/google/angle)                                                                   | Selected package's LICENSE is BSD-3-Clause. Preserve that license and applicable native transitive notices; ANGLE is Avalonia's renderer dependency, not a separately introduced Seqvium engine                                |
| SkiaSharp and NativeAssets Linux/macOS/Win32/WebAssembly / Microsoft, native Skia contributors                                                              | Managed/native drawing; [source/licenses](https://github.com/mono/SkiaSharp)                                                                        | Selected packages declare MIT and contain wrapper licenses. Underlying Skia uses BSD-style and transitive notices; audit actual native binary notices/third-party contents before redistribution, not just the wrapper license |
| HarfBuzzSharp and NativeAssets Linux/macOS/Win32/WebAssembly / Microsoft, HarfBuzz contributors                                                             | Managed/native text shaping; [wrapper](https://github.com/mono/SkiaSharp), [upstream terms](https://github.com/harfbuzz/harfbuzz/blob/main/COPYING) | Selected packages declare MIT; preserve wrapper and upstream copyright/license notices and actual native dependency obligations before distribution                                                                            |
| MicroCom.Runtime / Nikita Tsukanov and contributors                                                                                                         | Managed interop for framework platform adapters; [source](https://github.com/kekekeks/MicroCom)                                                     | Restored package metadata declares MIT; retain copyright/license when redistributed                                                                                                                                            |
| Tmds.DBus.Protocol / Tom Deseyn                                                                                                                             | Managed Linux D-Bus integration; [source/license](https://github.com/tmds/Tmds.DBus)                                                                | MIT in restored metadata; preserve license/copyright on redistribution                                                                                                                                                         |

This is a framework-dependent local host, not a packaged release or a completed native notice bundle.
NuGet supplies platform-native assets (actual Windows smoke loaded libSkiaSharp, libHarfBuzzSharp and
av_libGLESv2); Linux/macOS libraries/runtime/display prerequisites remain separately evidenced in
[PORTABILITY](PORTABILITY.md#r3-f1-desktop-feasibility-and-evidence). Restore containing WebAssembly
assets establishes neither a browser UI nor a WebAssembly target. No OS DLL is redistributed; the
small Windows menu adapter calls installed user32. Future delivered artifacts must include the actual
applicable licenses/notices and validate dependencies/signing/runtime installation. Permissive package
terms are compatible with Seqvium-authored Apache-2.0 without relicensing these components; native
transitive notice completion remains a release gate, not a claim made from this build.

R3-F2 adds no dependency, docking package, copied source or external asset. Generated F1/F2 screenshots
are not distributed runtime resources: F1 PNGs were removed from the current tree, and F2 captures remain
ignored locally for pending review under [evidence retention](experiments/README.md#evidence-retention).
They depict original Seqvium output, not third-party DAW references. Pane chrome/content remain
Seqvium-authored Apache-2.0 code; intentional future runtime assets still require normal provenance.

R4-F3-A adds no NuGet package or external icon library. Desktop composes the existing Windows adapter;
`Presentation/HostIcons.cs` is an original Seqvium-authored vector family under the repository license,
without copied reference application assets or font glyph dependencies. Shared host styles are original
code. Quiet synthetic GUI/device test WAVs and screenshots are ignored local evidence, not shipped
musical content or runtime assets. Existing dependency/distribution notice obligations remain unchanged.

SEQ-UI-REDESIGN introduces original Seqvium-authored vector rail icons, parameter-dial/thumbnail
drawing, instrument gradients and shadows under Apache-2.0. The eleven owner-supplied design images
are local research references outside Git and are neither runtime assets nor distributed content.
No additional dependency, borrowed font, texture, preset, sample or proprietary icon is introduced.

## Planned / under evaluation

These are candidates or product directions, not dependencies, adoption decisions, or promises.
SEQ-R0's bounded evaluation is recorded in its [protocol](experiments/SEQ-R0_PROTOCOL.md) and report;
it does not adopt a portable audio dependency or production engine.

| Candidate / direction                            | Intended evaluation boundary                                                                                                                                                                                                                                                  |
|--------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Further .NET application delivery                | Managed canonical foundation and a framework-dependent desktop host are adopted; self-contained/native packaging and distribution remain unevaluated                                                                                                                          |
| miniaudio or comparable backend                  | Still a portable candidate. Official miniaudio license offers public-domain or MIT-0 terms; PortAudio has an MIT-style license. Neither is downloaded, built, redistributed or device-tested here; concrete revisions/transitive/backend/distribution checks remain necessary |
| Future ASIO integration                          | Desired later device capability, not an R0 requirement or adopted SDK/backend; evaluate concrete implementation source, licensing and redistribution when its stage approaches                                                                                                |
| CLAP / VST3 hosting                              | Later possibility; evaluate concrete SDK terms, bridges, distribution and product need when justified                                                                                                                                                                         |
| Additional codecs / portable compressed decoding | Playlist WAV/MP3/FLAC import is implemented as described above; other codecs and a Linux/macOS compressed-source backend remain unevaluated                                                                                                                                   |

A native realtime engine, its implementation language (possibly C++), exact native ABI, and
package/plugin mechanisms remain open architectural choices. **C++ is a language choice, not a
third-party dependency.** Any chosen compiler/toolchain/runtime or library needs its own concrete
evaluation. [ARCHITECTURE](ARCHITECTURE.md), [AUDIO_ENGINE](AUDIO_ENGINE.md), and
[EXTENSIONS](EXTENSIONS.md) own those choices; this ledger does not accept them.

## Assets / borrowed material

No samples, sample packs, presets, impulse responses, demo projects/patterns, audio, icons, fonts,
or other shipped third-party assets have been introduced.

Musical content needs the same provenance discipline as code. Before material enters the official
repository or distribution, its record must identify:

- Source and author/organization, with a source URL or a documented project-authored creation method.
- License/terms and the right to redistribute the material in its intended form.
- Purpose, owning resource/subsystem, and repository paths where applicable.
- Whether it is redistributed; distinguish shipped content from developer-only evaluation/test material.
- Modifications or derivation, including source inputs and relevant generation/tool provenance.
- Introduced stage and relevant decision, if any.

This applies to samples/packs, presets, impulse responses, demo music/patterns, generated audio or
audio based on third-party material, icons, fonts, and all other shipped assets. Generation or resampling
does not erase the provenance or terms of input material. Recipe metadata can help explain derivation;
it is not evidence of redistribution rights by itself. Local test material does not become distributable
merely because it is available on disk. [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) separately owns accepted
audio durability, not rights clearance.

## Repository services and actions

No repository-configured hosted services or CI actions have been introduced. There are no workflows,
external service integrations, or runtime cloud-generation dependencies.

Future service/action entries need identity, organization, terms/license, purpose, official source,
introduction stage, and configuration/workflow version authority. Keep hosted services distinct from
redistributed runtime components; do not add CI just to populate this register.

## Future entry fields

Each introduced component/package, action, or service must record:

- Exact component/package/action identity and author/organization.
- License/terms, notices and redistribution/source obligations; concrete compatibility evaluation
  with Seqvium's Apache-2.0 work and the intended distributed product.
- Purpose and owning subsystem, official source, and introduced stage/decision when relevant.
- Form: managed, native, runtime, service, action, or asset; supported platforms where relevant.
- Version authority: the actual `.csproj`, central package/package manifest, native-version manifest,
  workflow, or other reproducible source of the installed revision. Link it when it exists.

An evaluation snapshot may record a checked version/date and official evidence, explicitly labeled
as research. It must not compete with build manifests. Native entries additionally need official
binary/source provenance, runtime/platform distribution implications, and material transitive obligations.
Evaluate codec/patent concerns where applicable. Asset entries use the additional fields above.

Record removal/replacement with status, stage and successor/evidence where relevant. This active
ledger retains current introduced material, materially evaluated candidates and current obligations;
history may move to the cold archive when no longer relevant to those obligations. Never remove
legally required current provenance or notices merely because material is old. Update this owner when
changed terms, upgrades, replacements or redistribution scope change obligations. Candidate popularity is not
sufficient adoption evidence.
