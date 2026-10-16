# Design System — compact studio instruments

Role: Technical catalog for the implemented first-party Desktop visual API.
Read when: Creating or changing any first-party control, window, pane, drawing, or shared style.
Authoritative for: Token/component use, supported variants, template integration, exceptions, and shared-style
verification.
Not authoritative for: Visual principles, musical/workspace behavior, product acceptance, or independently maintained
numeric/color values.

## Ownership and entry points

[UI_DESIGN](UI_DESIGN.md) owns visual principles, density and hierarchy. This catalog specifies how
to implement them. Actual values belong exclusively to
[HostPalette](../src/Seqvium.Desktop/Presentation/HostPalette.cs) and
[HostMetrics](../src/Seqvium.Desktop/Presentation/HostMetrics.cs).
[HostMaterials](../src/Seqvium.Desktop/Presentation/HostMaterials.cs) derives instrument gradients and
shadows from the same semantic palette. Do not copy their numbers or HEX
into another maintained table, local control style, or documentation page.

[App](../src/Seqvium.Desktop/App.axaml.cs) installs typed metrics, semantic brushes and derived materials.
[HostControls](../src/Seqvium.Desktop/Styles/HostControls.axaml) styles standard controls by default;
a new first-party editor inherits the system without adding a class to every control.
`shell`/`workflow` remain compatibility markers for existing surfaces, not alternate visual systems.
[HostFluentResources](../src/Seqvium.Desktop/Presentation/HostFluentResources.cs) maps the installed
Fluent template resource names to these same tokens. These aliases contain no independent colors.
Application resource replacement synchronizes aliases; theme changes retain existing controls.
Every custom drawing surface subscribes to its own `IResourceHost.ResourcesChanged` and invalidates
its visual, including retained Pattern/Part thumbnails, Gain transfer, Playlist overview/timeline and
Piano. Resolving live brushes only inside `Render` is insufficient: a cached scene may otherwise keep
the previous theme until another visual change. Resource refresh never requires replacing editor content.

SettingsWindow consumes shared compact Window, Button, ComboBox, CheckBox, TextBox and Expander
styles. Its shortcut list uses the shared scrollbar gutter; selected page borders use Accent.Action
with weight/thickness cues. Shortcut rows reserve the shared icon width for custom-only reset; a
dot/bold chord and tooltip identify changes without color alone. ThemeCatalog feeds the shared sun/moon/check icon family without new
palette values or control templates. About retains Brand.AboutSize and embedded typography.

## Token catalog

### Brand and embedded typography

The [brand kit](../brand/README.md) owns the manually reconstructed 09 SVG, separate 16/20–40 px
optical masters, retained 01/07 alternatives and offline exports. Desktop owns runtime resources in
`Assets/Brand` and `Assets/Fonts`. `ApplicationIcon` embeds the multi-frame ICO in the apphost;
MainWindow uses the same ICO as its native icon and the small PNG in title chrome.

`HostMetrics.FontFamilyName` and `MonospaceFamilyName` are explicit `avares://` font locations.
Application default, Window styles and detached HostVisual drawing use those bundled families.
Onest has static 400/600/900 faces; JetBrains Mono has static 400. `Program.FontOptions` registers
the embedded music-symbol subset for U+2669–U+266F, including Piano's sharp sign. This bounded fallback
does not promise every script or arbitrary user glyph without a platform font.

`Type.BrandFamily`, `Type.Brand`, `Type.BrandDisplay`, `Type.BrandTracking`, `Brand.ChromeSize` and
`Brand.AboutSize` support title/About. The `brand` TextBlock variant selects Black; `paneTitle` and
`section` use SemiBold. Ordinary text/commands remain regular. Numeric TextBlock/TextBox/NumericUpDown
use the embedded mono family. Keep existing dimensions and actual presenter alignment.

The [identity verifier](../tools/Seqvium.VisualCheck/IdentityCheck.cs) compares resolved glyph-stream
hashes with embedded bytes, checks real weights without simulations and RU/EN coverage, verifies
musical-symbol fallback, renders both themes/languages with the demo and measures toolbar fields.
Shared-style changes still require the catalog and broader existing field matrix below.

Playlist's section band uses `Playlist.SectionHeight` beneath the existing ruler. Its tint, name,
boundaries and selected edge reuse channel, surface, caption and accent roles; no private palette is
introduced. Track M/S fit inside each shared lane using the existing compact status-height metric.
Shared/variation glyphs supplement names and explanatory tooltips rather than acting as selection.
Section navigation/commands and an occasional boundary-conflict message remain contextual. Exact
section/clip fields use ordinary `parameterRow`/`numeric` controls and actual presenter alignment.

Playlist Audio Clip drawing uses the existing surface, bevel, channel-identity, accent and text roles,
shared Playlist metrics and HostVisual captions. Draw min/max waveform bands separately for mono/stereo;
fit visual amplitude without altering audio gain or claiming a meter. Contextual source-frame/fade/gain
fields use shared `parameterRow`/`numeric` styles, including actual TextPresenter vertical alignment.
Do not introduce a private audio-clip palette or locally stretched control templates.

Local time stretch adds one ordinary numeric percentage row to those same clip properties and a
single playback-duration caption. Non-unity `↔`/percentage precedes the source name in the existing
drawn header, distinct from repeat `×`. Both use existing typography/palette and actual field alignment;
there is no new editor, control template, theme token or global preference.

All color roles exist in Dark and Light. `HostPalette.For` is also the fallback for a detached
drawing control; application resources are the live authority after attachment.

| Category             | Implemented semantic roles                                                                                  | Use                                                                                       |
|----------------------|-------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| Surfaces             | `Surface.Background`, `Surface.Raised`, `Surface.Workspace`                                                 | Application, pane/header/popup, working canvas                                            |
| Interaction surfaces | `Surface.Hover`, `Surface.Pressed`, `Surface.Selected`, `Surface.Accent`                                    | Shared control states and selection                                                       |
| Status surfaces      | `Surface.Playing`, `Surface.Critical`                                                                       | Active transport and destructive hover                                                    |
| Text                 | `Text.Primary`, `Text.Secondary`                                                                            | Body and secondary/caption                                                                |
| Boundaries           | `Border.Default`, `Focus.Active`                                                                            | Separators and the independently visible keyboard/active-pane outline                     |
| Action/state         | `Accent.Action`, `State.Unavailable`                                                                        | Actions/selection and disabled state                                                      |
| Status               | `Status.Warning`, `Status.Critical`, `Status.Playing`                                                       | Warning/error and current success/transport indication; accompany meaning with text/shape |
| Graph                | `Graph.Node`, `Graph.NodeBorder`, `Graph.Cable`, `Graph.Grid`                                               | Unselected nodes and canvas-specific geometry                                             |
| Level controls       | `Control.Track`, `Control.Fill`, `Control.Thumb`                                                            | Slider, captured Gain level and physical dial                                             |
| Channel Rack         | `Rack.Channel0`–`Rack.Channel7`, corresponding `Off` roles, `Rack.Ruler`, `Rack.StepInk`, `Rack.StepBorder` | Channel identity, checked/off bodies, common ruler and indicators                         |
| Piano keyboard       | `Piano.WhiteKey`, `Piano.BlackKey`, `Piano.KeyInk`                                                          | Theme-owned fixed keyboard, pitch identity and readable key labels                        |
| Material colors      | `Material.Highlight`, `Material.Shade`, `Material.Clear`, `Material.Signal`                                 | Palette-owned light, shade, transparent endpoint and signal tint                          |
| Material brushes     | `Material.Panel`, `Material.Button`, `Material.Bevel`, `Material.Dial`, `Material.CurveFill`                | Derived layered instrument surfaces and analytical fill                                   |
| Material shadows     | `Material.PanelShadow`, `Material.Recess`, `Material.PadShadow`, `Material.Glow0`–`Material.Glow7`          | Raised/recessed surfaces and active channel light; typed BoxShadows                       |

Geometry and typography resources are declared in `HostMetrics.Values` and its typed `Resources`:

| Category         | Resources                                                                                                                                                                              | Use                                                                                                   |
|------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| Shell/chrome     | `Chrome.Height`, `Toolbar.Height`, `Status.Height`, `Chrome.ButtonWidth`, `Pane.HeaderHeight`, `Pane.Corner`                                                                           | Shared frame and pane chrome                                                                          |
| Controls         | `Control.Height`, `Control.InputHeight`, `Control.ComfortableHeight`, `Control.IconWidth`, `Control.StatusHeight`, `Control.Corner`                                                    | Default, editable, modal, icon and status variants                                                    |
| Boundaries       | `Border.Width`, `Focus.Width`, `Border.Control`, `Border.Top`, `Border.Bottom`, `Border.Focus`                                                                                         | Drawing widths and typed control/frame thicknesses                                                    |
| Spacing          | `Space.XSmall`, `Space.Small`, `Space.Gap`, `Space.Medium`, `Space.Large`, `Space.Section`                                                                                             | Increasing density scale; use a role instead of a copied dimension                                    |
| Padding          | `Padding.Control`, `Padding.Input`, `Padding.Content`, `Padding.Popup`, `Padding.Dialog`, `Padding.Parameter`, `Padding.ComboBox`, `Padding.Numeric`, `Padding.Status`, `Padding.Icon` | Typed padding/margin roles for actual compositions                                                    |
| Typography       | `Type.Body`, `Type.Caption`, `Type.Section`, `Type.PaneTitle`, `Type.Numeric`, `Type.Transport`, `Type.FontFamily`, `Type.MonospaceFamily`                                             | Body/caption/section/pane/numeric/transport emphasis; monospace for technical or fixed-column content |
| Level/scroll     | `Level.Height`, `Level.TrackHeight`, `Level.ThumbSize`, `Level.Inset`, `ScrollBar.Size`                                                                                                | Real hit area, drawn/templated geometry, visible local scrolling                                      |
| Rack/navigation  | `Rack.StepHeight`, `Rack.RowHeight`, `Rack.StepCorner`, `Rail.Width`                                                                                                                   | Pad hit geometry, rhythmic row and navigation rail                                                    |
| Playlist         | `Playlist.LaneHeight`, `Playlist.RulerHeight`, `Playlist.HeaderWidth`, `Playlist.OverviewHeight`, `Playlist.SectionHeight`                                                                                       | Musical lane/header hit geometry and the derived song overview                                        |
| Piano Roll       | `Piano.KeyWidth`, `Piano.RowHeight`, `Piano.RulerHeight`, `Piano.VelocityHeight`, `Piano.NoteCorner`                                                                                   | Keyboard/grid alignment, velocity lane and bevelled note geometry                                     |
| Numeric template | `Control.NumericInnerHeight`                                                                                                                                                           | Derived inner height respecting outer control/border; do not set a second local template minimum      |

Colors are semantic values. Geometry's spacing scale is the small base vocabulary; padding/control
roles give those values a purpose. Shape, graph coordinates, menu placement, content-specific widths
and pane layout bounds remain composition/interaction geometry, rather than hundreds of universal tokens.

Dark/Light have equal visual-quality goals under [theme parity](UI_DESIGN.md#theme-parity-and-control-economy).
Light's Rack.Step roles also supply the saturated Playlist bodies; Rack.Channel roles supply their
deeper note/identity ink and checked-pad bodies. Keep those pairs coherent when changing either theme.
HostPresentation checks note/body and checked-indicator contrast; actual material overlays, glow,
focus/selection and sparse/dense content still require image review. Do not lighten channel ink and
then compensate with a separate local palette. The current Dark values are unaffected by the home
preparation; actual values remain source-owned.

## Information density and Sample Lab

Apply the mandatory reference/density gate in [UI design](UI_DESIGN.md#required-reference-and-information-density-gate)
before changing a surface. Standard icon commands retain localized accessible names and short tooltips.
Normal panels omit permanent tutorials, repeated units/formats and diagnostic counters. Exact properties
remain deliberately accessible; errors and shared-edit consequences keep concise feedback.

Sample Lab composes standard controls, a shared BrowserWaveform, compact candidate names/reference
icons, a Shape disclosure and pinned acceptance actions. Recent names/lineage/accepted marks replace
recipe dumps; duration is the sole normal numeric source readout. Recipe values use family-appropriate
precision and appear only on disclosure. At narrow widths history stacks below the scrolling sound
area; acceptance remains reachable. New SampleLab/Reference vectors belong to HostIcons.
Browser hides empty preview and normal success chatter; metadata details are on inspection. Extensions
shows availability, with identity/diagnostics on inspection. Mixer keeps real visual meters and exact
strip controls, with peak/reduction observations in a tooltip and visible overload only. Gain uses one
editable value; dB conversion stays in inspection instead of duplicating percent/linear/dB readouts.
No shared control template, numeric minimum, palette or theme token changes accompany this cleanup.

## Sampler waveform composition

Complementary ramps identify the two actual F2 overlap areas; positive crossfade never moves source
or loop markers. The fifth exact field uses the same parameter-row geometry and local reflow.
The existing Source Editor hosts SamplerPaneContent/SamplerWaveform with standard controls, shared
parameterRow/numeric fields and HostVisual typography/brushes. No palette, global preference, window
registry or shared template is added. The waveform uses Surface.Workspace/Selected/Accent,
Accent.Action for sample flags and Status.Playing for enabled loop flags; muted retained flags use
Text.Secondary. Upper/lower handle lanes keep overlapping ranges addressable; crowded captions yield
to exact labeled fields. A large stereo surface, zoom/pan and responsive wrapped fields keep the
working signal dominant. Real RU/EN Dark/Light ordinary/reduced captures and field alignment are
required; the [F1 report](experiments/SEQ-SAMPLER-EDITOR-F1_REPORT.md) and
[crossfade report](experiments/SEQ-SAMPLER-LOOP-QUALITY-F2_REPORT.md) own measured evidence.

## Browser composition

The local extension disclosure is a host-owned Expander with standard ComboBox/buttons, wrapped
caption diagnostics and local scrolling. Identity/version/purpose/state remain textual; it contributes
no palette, template or independent icon system. Enabled pack locations join Browser's existing list.
Compact Expander headers integrate the chevron into one continuous header surface. Its square uses
the shared inner-control height, fits inside the header border and has only shared horizontal spacing;
all normal/hover/pressed/disabled chevron backgrounds are clear. This applies through the Fluent
compatibility aliases to Browser, Sample Lab and other disclosures, retaining native toggle input.
At reduced heights, disclosure and preview have bounded local scroll areas so the sound list remains
reachable; at ordinary heights preview retains its established controls. This adds no shared-style
or field-alignment exception. R6 uses metadata labels only, not a generic contributed style registry.

Browser uses shared standard ComboBox/TextBox/ListBox, compact vector buttons, localized actions and
local virtualized scrolling. The pinned preview uses the existing caption/monospace hierarchy and
`BrowserWaveform` resolves live HostVisual surface/boundary/accent roles in both themes. It draws only
actual selected mono/stereo min/max envelopes, visually fitted without changing gain. Source names
are ellipsized with full-path tooltips. The original Browser/Refresh vectors belong to HostIcons;
there is no new palette, shared template or field-height exception.

## Standard controls and variants

### Mixer and chassis components

The compact studio direction in [UI design](UI_DESIGN.md#compact-studio-instrument-direction) supersedes
the initial oversized Mixer. `Mixer.ChannelWidth`, `MasterWidth`, `StripHeight`, `FaderHeight`, `PanSize`
and `ControlHeight` own its fixed bank geometry. `Mixer.Channel`, `Well`, `Cap` and `CapShade` supply
theme-specific strip/recess/metal depth; `Material.MixerChannel`, `FaderCap` and `Header` derive brushes.
`HostChassisTexture` draws original bounded low-contrast grain behind pane content, without input authority.
It is never drawn over musical grids or signal plots. No raster/third-party asset is required.

`MixerFader` combines a nonlinear dB track and real stereo peak/RMS ladder, with a held-overload light.
`GainDialControl.Compact` is the shared compact pan variant; its default instrument variant is unchanged.
`MixerResponsePlot` draws calibrated analytical EQ/transfer curves, not measured spectral analysis.
The side inspector uses standard controls, host-owned tabs and local scrolling. Its compact EQ,
Comp, Routing and Delay tabs fit the existing inspector in RU/EN. Delay reuses ordinary parameterRow/
numeric fields for milliseconds and percentages plus a standard Enabled toggle; it adds no template,
palette, global option, explanatory paragraph or strip meter. The strip's D mark shows enabled state. Its disclosure is a
ToggleButton, not an empty expanded content region. The bank stays narrow as the pane grows.

`mixerControl` and `mixerValue` are deliberate small strip variants. The strip field height is below
the ordinary field-alignment minimum: its narrow single-line readout belongs to a mouse-oriented
channel strip, with the fader/dial and exact entry available separately. Do not apply that exception
to forms or contextual processing fields. `mixerValueRow` aligns the readout without extra label air.
Unselected strip buttons are quiet; thin source bands carry identity. Framework tab resource aliases
must use host typography, colors and dimensions, including selected pipes and disabled states.

Shared density changes require catalog, field-alignment and populated workspace checks in both themes.
Keep normal exact-field text/unit centers aligned, review small-strip readability separately, and verify
RU/EN inspector reachability through scrolling. Source remains the single numeric/color authority.

Rack pads use `Rack.StepHeight` for fixed width/minimum/maximum and height, with Auto-sized beat cells.
`Rack.BarPadding` derives the additional bar gap from shared spacing. Rich-note previews still stretch.
The inner cap, highlight/shade overlays and pressed recess retain existing semantic materials;
playing outline and keyboard focus remain independent. Shared ScrollViewer disables overlay auto-hide;
`ScrollBar.VerticalGutter` and `ScrollBar.HorizontalGutter` derive the content-to-bar gap from shared
spacing. This applies to inspectors, lists, popups and standalone timeline scrollbars. Hidden bars
reserve no gutter. Do not add a second per-panel scrollbar-width margin. Responsive content uses
the actual viewport width; Pattern cards do not paint underneath the bar.
Pattern cards use up to two wrapped name lines with ellipsis beyond that, a full-name/gesture tooltip
and a continuously resizable navigator. Its focusable boundary uses shared spacing/border/accent
roles; width limits and collapse behavior are workspace composition geometry. Selection styling and rhythm thumbnails still use the common
Button/material/channel roles; editor menus use the standard ContextMenu rather than a private popup.
Browser and Patterns use the same `SidePanelHost` clipped surface and focusable boundary, with shared
spacing, border and accent resources. Static caption rows use the common control height, never window
chrome. Original `HostIcons.Patterns` phrase cards and `HostIcons.Browser` folder/sound vectors share
the rail's stroke/foreground family; active state means visible for these two toggles. Minimum-content
widths, near-edge collapse and independent ordered allocation belong to
[Workspace](WORKSPACE.md#shared-left-panel-zone), not a control-template exception.

| Component                 | Default / supported classes                                                                  | Guidance                                                                                                                                                                                                          |
|---------------------------|----------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Button                    | Default standard; `primary`, `compact`, `command`/`icon`, `danger`/`critical`, `comfortable` | Standard commands, principal action, dense input action, named vector icon, destructive action, owned-modal action                                                                                                |
| Transport/status Button   | `play` with `active`; `status` with `warning`/`error`/`playing`                              | Existing transport and status surfaces; keep the deliberate semantic exception                                                                                                                                    |
| Numeric input             | TextBox `numeric`; NumericUpDown                                                             | Use committed TextBox sessions where a transaction must be atomic; NumericUpDown remains the existing quick control. Styling does not merge their edit semantics                                                  |
| ComboBox                  | Default                                                                                      | Actual target/occurrence selection; popup and state resources follow the same palette                                                                                                                             |
| ToggleButton              | Default; `step` + `channel0`–`channel7`, `current`                                           | Actual toggles; Rack's retained tactile template adds a bevel, lit body, bottom indicator, captured pressed recess and independent focus frame; `current` outlines the actual playing column using Status.Playing |
| Rail Button               | `rail`, `active`                                                                             | Direct named pane activation; vector icons and real enabled/active state                                                                                                                                          |
| Pattern name              | TextBox `patternName`                                                                        | Integrated committed name, actual text editing/focus without a duplicate form label                                                                                                                               |
| Slider / Gain level       | Default Slider; `GainLevelControl`, `GainDialControl`                                        | Shared roles/materials; captured single-commit gestures, keyboard access, same exact parameter and history                                                                                                        |
| Expander                  | Default                                                                                      | Progressive disclosure, shared header/content resources; specialized content retains its own layout                                                                                                               |
| Menu / ContextMenu        | Default                                                                                      | Retain framework keyboard/popup lifecycle, shared surfaces and states                                                                                                                                             |
| ToolTip / FlyoutPresenter | Default                                                                                      | Brief hints and deliberately opened details, shared typography/padding/boundaries                                                                                                                                 |
| Pane chrome               | `workspacePane`, `active`, `paneHeader`, `paneChrome`, `paneTitle`                           | Active pane is separate from control keyboard focus and musical selection                                                                                                                                         |
| StackPanel spacing        | Default gap; `joined`, `tight`, `compact`, `spaced`, `section`                               | Shared spacing scale; joined chrome/icon groups have no inter-control gap                                                                                                                                         |
| Parameter row             | Grid or horizontal StackPanel `parameterRow`                                                 | Label, editable field, derived unit/value and action share the alignment rules below                                                                                                                              |
| Text                      | `secondary`, `caption`, `section`, `numeric`, `monospace`, `warning`, `error`, `success`     | Hierarchy/status rather than local color assignment                                                                                                                                                               |
| ScrollViewer / ScrollBar  | Default                                                                                      | Scroll content locally; visible shared scrollbar geometry/palette; never add whole-window scrolling to fit chrome                                                                                                 |

`command`/`icon` requires `HostIcons` and localized accessible name/tooltip. Chrome's square corner
and status's low emphasis are deliberate variants; changing ordinary button radius must not round
window chrome. Native file pickers and independently rendered external plugin editors retain their
explicit [UI exceptions](UI_DESIGN.md#first-party-visual-design-rules).
The pre-GUI Single Instance failure uses a narrow native Windows diagnostic (stderr elsewhere):
exclusive ownership failed before Avalonia/document initialization, so the secondary must not construct
the regular application just to report that failure. It is a lifecycle exception, not another component palette.

## Synth instrument components

SynthPaneContent uses existing Recess materials, GainDialControl, standard buttons/ComboBox
and numeric parameter rows in one unboxed instrument surface. Paired oscillator regions retain
four recognizable shape choices even at compact width; selected shape adds a stronger contour and
accent ink. Original waveform selectors/period drawings show actual selected shapes.
Exact values center on the dial axis; a reserved right column carries the unit without shifting
the field. Release centers its icon/text group as one item and follows button state foreground.
SynthEnvelopePlot uses separate logarithmic A/D/R segments and an untimed dashed sustain plateau:
an analytical schematic, never measured output or a fixed note duration. Handles and exact controls
share real parameters/history. Drawing uses live HostVisual resources and resource-change invalidation.
No palette/template or third-party asset is added. Local scrolling and reflowing parameter groups
retain compact reachability. A pinned preview aligns a 40-DIP note field and three 40-DIP-high icon
buttons on one row. HostIcons' original falling-release vector and concise localized Release label
supplement recognizable Play and Stop icons; short localized tooltips/accessibility names supply
complete action names. Held Play adds the existing accent surface and a stronger outline. The
envelope plot absorbs spare vertical space; its minimum remains usable in the scrolling compact body.
Shared styles and Gain semantics remain unchanged.
Actual exact-field/unit alignment and localized/theme/size checks belong to
[the report](experiments/SEQ-SYNTH-F1_REPORT.md).

## Drum instrument components

`DrumVoicePaneContent` composes the shared Panel/Recess material, standard fields/buttons and
`GainDialControl` with parameter-specific normalization/defaults. Dial center text, accent/track roles
and a recessed well are optional host variants; default Gain presentation remains unchanged. Drum
uses exact values once, retaining the distinct Tune frequency. Light well/scale and waveform choices
derive from existing semantic roles, without local HEX colors or another palette.
`DrumEnvelopePlot` uses HostVisual for the editable analytical curves and actual derived waveform.
Amplitude gestures never read pitch-only parameters. Waveform requests cancel superseded work, join
the preceding worker and wait briefly for input to settle; the analytical curve and values update
immediately. `GainDialControl` uses relative angular increments, an insensitive center and immediate
limit reversal in Drum, Gain and Mixer. Shared `GainLevelControl` draws a continuous track with an
outlined thumb and captures the whole X gesture outside its visible bounds. Drum audition velocity
reuses this control and its Escape/capture-loss behavior with a local 0.05–1 mapping, outside Undo.
Instrument descriptors supply only the working knobs, preset selector and hat choke toggle. Portable
preset actions use existing Save/Open vectors and standard buttons; repeated stopped audition remains enabled.
The shared chassis, responsive audition/knob groups and local ScrollViewer preserve comfortable
input dimensions. Rendered exact-text/unit centers and reachability are verified in both languages
and themes under [the instrument report](experiments/SEQ-FIRST-DRUM_REPORT.md).

The Drum audition footer uses the shared `Button.audition` variant: a spacious icon-only Play with
an adjacent square Stop, both 56 DIP high, semantic materials, normal hover/press/focus feedback and
localized tooltip/accessibility names. It stays outside parameter scrolling and stacks note/velocity
below the actions at narrow widths. The component catalog includes both variants and states.
The complete footer row is centered as a group, with symmetric dock padding and common row centers.
The 104×40-DIP note selector and 128×40-DIP audition level use a larger track/thumb from the
`AuditionLevel` metrics; ordinary Gain keeps its `Level` metrics. Narrow footers center both stacked rows.
Drum dials shrink continuously from 76 to 64 DIP using the editor's allocated width/height, independently
of column count. Parameter cells shrink with them, then wrap with separate enter/leave thresholds;
the graph height follows available space within 180–250 DIP. Exact fields and fonts keep
their normal sizes; envelope grab tolerance remains unchanged. Pointer capture freezes control sizes,
while retained text controls preserve a typed draft/focus across resizing. Musical draft state has no
permanent status label; brief warnings occupy the fixed identity row and cannot move the audition footer.
Shared-height two-line parameter label slots keep neighbouring knobs and exact fields aligned across
translated wrapping without shrinking type.

## Field and row alignment

Playlist composes standard source/snap selectors, icon commands, repeat NumericUpDown, Follow toggle
and an owned track settings flyout. M/S heading hit targets use shared control height/corners and
instrument materials; the toolbar provides a keyboard-accessible track editor. Its color chips use
actual palette-bound Border content inside standard buttons, so the button's material template cannot
hide the swatch. Drawing uses HostVisual for track/clip identity, bevel, grid, note durations/pitches,
selection, overview and renderer cursor. Clip/track colors use the existing Rack palette; no new
HEX palette, borrowed screenshot, fake waveform, meter or dormant processor control is added.

Playlist now adds original Start/Selection/Loop vectors and a distinct upward-export/wave vector,
shared with Pattern/Song export in the command overflow. Project Save retains the disk icon.
Range highlights sit under readable ruler labels and supplement the active loop toggle; group
selection uses the existing accent outline. Explicit pane frame hit surfaces reserve resize input
without changing control templates, and the visible corner grip stays separate from music controls.

Playlist's repeat label/actual text centers are checked in the RU/EN Dark/Light ordinary/reduced
window route. The overview/stationary headings and minimum-size wrap are part of rendered review.

Parameter rows use one vertical center line for the label, actual editable text, units and action.
Use `parameterRow`; do not compensate with arbitrary top margins, independent font sizes, or nested
controls whose minimum exceeds the allocated row. Standard numeric text and row labels use the
shared body/numeric typography. NumericUpDown sets `VerticalContentAlignment=Center` on the parent:
its Fluent template forwards that property to the inner TextBox, so styling only the child is insufficient.
The compact numeric template uses the derived inner-height resource.

Verify the actual rendered TextPresenter and label centers, the editable text's containment, row
height and adjacent button hit target. A nominal Height on the outer control is insufficient.
[FieldAlignment](../tools/Seqvium.VisualCheck/FieldAlignment.cs) checks center differences within
one DIP on the real GUI tree; toolbar Gain, Graph quick Gain and Node Editor are included in the RU/EN × Dark/Light
matrix. Apply this check when introducing another parameter row. This is geometric alignment,
not a claim that unrelated typefaces have identical optical baselines.

Toolbar Gain retains the existing numeric template and alignment, with committed fractional input (Enter/focus loss),
local invalid feedback and Escape restoration. Floating 24-DIP corner grips have
a matching 24-DIP hit region; straight frame edges retain the six-DIP band.

## New components, drawing and exceptions

Start with an existing control/style and a specialized composition. Use `{DynamicResource ...}`
in AXAML and `GetResourceObservable` bindings in C# for standard appearance and spacing. Add a
specialized Control only when input/capture/rendering ownership requires it.

DrawingContext controls use [HostVisual](../src/Seqvium.Desktop/Presentation/HostVisual.cs) for live
brushes, metrics and typefaces, and invalidate on resource changes. GraphCanvas and Gain drawing
consume the same semantic resources; do not retain a private HEX palette or theme-only brush cache.
Technical annotations can select an existing warning/error role without adding a new color source.

An exception must have a real interaction/content reason, a named reusable variant when appropriate,
and Dark/Light plus minimum-size evidence. Keep it at the consuming boundary. If the visual contract
changes, update UI_DESIGN and its implementation together; documentation is not a substitute for
resources. New common roles belong in both palettes and the small catalog, with focused checks.

## Verification workflow

SEQ-R9's drawn grid, keys, note bodies/handles, velocity stems and Rack melodic thumbnail use the
same HostVisual palette/material/metric resolution as other musical surfaces. Piano/Draw/Velocity
icons are original HostIcons vectors. Specialized keyboard/grid geometry is deliberately not a
generic parameter-row minimum. Source/note properties use shared standard controls and numeric
templates; actual label/TextPresenter alignment is measured, not inferred from outer height.
Existing Dark role values are preserved; both keyboard palettes and actual sparse/chord/reduced
compositions are reviewed separately. Evidence belongs to the [R9 report](experiments/SEQ-R9_REPORT.md).

[VisualCheck](../tools/Seqvium.VisualCheck/README.md) is a separately invoked developer host, with
isolated preferences/layout/media and no production developer tab. It renders real controls, menus,
tooltips, field rows and drawn levels; fixture pseudo states are explicitly separated from native input.
The catalog changes a common radius/fill temporarily, verifies propagation to template parts and
drawn levels, preserves invalid/disabled exceptions, then restores accepted values. The matrix does
the analogous Graph.Node change and restores it without musical edits.

After a shared style/template change, build affected projects and run focused DesignSystem/Gesture/
Instance tests, the component catalog and rendered field-alignment checks. Select related GUI states/
geometries from the affected controls; full suite and full GUI/native matrix follow the risk/cadence
rules in [TEST_EXECUTION](TEST_EXECUTION.md#required-verification-levels). Inspect screenshots in both themes;
check actual hit targets, focus, disabled/error states, labels/disclosure and localized minimum sizes.
The cheap source guard rejects literal HEX colors outside HostPalette and missing palette/metric/material style
resources.
It does not replace image review or detect every possible programmatic hardcoded color expression.
The compatibility aliases are tied to actual installed Fluent templates: recheck their names and
rendered behavior when upgrading Avalonia. Evidence/acceptance limits belong to
[TEST_EXECUTION](TEST_EXECUTION.md) and [PORTABILITY](PORTABILITY.md).
