# Design System v2

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

## Token catalog

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
| Playlist         | `Playlist.LaneHeight`, `Playlist.RulerHeight`, `Playlist.HeaderWidth`, `Playlist.OverviewHeight`                                                                                       | Musical lane/header hit geometry and the derived song overview                                        |
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

## Standard controls and variants

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

## Drum instrument components

`DrumVoicePaneContent` composes the shared Panel/Recess material, standard fields/buttons and
`GainDialControl` with parameter-specific normalization/defaults. Dial center text, accent/track roles
and a recessed well are optional host variants; default Gain presentation remains unchanged. Drum
uses exact values once, retaining the distinct Tune frequency. Light well/scale and waveform choices
derive from existing semantic roles, without local HEX colors or another palette.
`DrumEnvelopePlot` uses HostVisual for the editable analytical curves and actual derived waveform.
The shared chassis, responsive audition/knob groups and local ScrollViewer preserve comfortable
input dimensions. Rendered exact-text/unit centers and reachability are verified in both languages
and themes under [the instrument report](experiments/SEQ-FIRST-DRUM_REPORT.md).

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
