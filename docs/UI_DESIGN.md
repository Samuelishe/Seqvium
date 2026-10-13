# UI design

Role: Evolving visual and interaction design guide.
Read when: Designing visual hierarchy, pane chrome, controls, indicators, or graph presentation.
Authoritative for: Durable visual principles, usable responsive layout, interaction density, consistency, progressive
visual complexity, semantic theme/style resources.
Not authoritative for: Workflow semantics, pane lifecycle, graph execution, or implemented token values/component API.

## Design intent and scope

Seqvium should feel like a creative instrument: substantial, tactile and enjoyable to manipulate.
The owner explicitly requests expressive instrument design, including volume, complex controls,
shadows, gradients, lighting and meaningful visualizations. Implementation difficulty is not a
reason to reject an otherwise useful design. The older flat-shell preference is superseded.
[DESIGN_SYSTEM](DESIGN_SYSTEM.md) catalogs the implemented v2 visual API; source owns actual values.
Visual quality and musical correctness are complementary requirements.

## Instrument materials and hierarchy

Use a slate chassis, illuminated jade actions, colored channel identities and sculpted controls.
Depth has a deliberate structure: workspace recess, raised instrument panel, bevelled control,
active light. Highlights, shadows, gradients and controlled glow create character and explain
manipulation. Detailed knobs and graphical editors are encouraged when they support a real parameter.
Avoid making every surface equally luminous or surrounding ordinary settings with elaborate frames.
Materials may be rich without obscuring labels, focus, grid rhythm or actual values.

Dark is the primary studio presentation. Light uses cool pale chassis surfaces and deeper active
channel colors, with the same geometry, materials, states and interaction. It is designed separately
for legibility rather than obtained by inversion. A checked pad has both a lit body and a small
indicator; focus remains an independent outline. Channel hue supplements readable names and numbers.

`HostPalette` owns semantic colors, `HostMetrics` geometry/typography and `HostMaterials` derived
gradients/shadows. Standard controls and DrawingContext compositions use these shared resources.
Instrument-specific composition is encouraged; a general-purpose form is not the default music UI.
No borrowed graphics or screenshot textures are shipped.

## Implemented R3-F1 visual baseline

The retained single-window shell now uses the v2 instrument materials, larger readable shared
typography and one transport row. Project actions, Play/Stop, BPM, meter and history have clear groups.
Version/capability details remain in About; technical information is disclosed deliberately.
The original F1 frame's lifecycle/OS behavior is retained; its dimensions and flat appearance are
superseded by the shared resources. Theme/language switching retains the window and controls.

## Implemented R3-F2 pane presentation

Panels have a substantial raised surface, bevelled header, readable title, shadow and distinct
active outline. Close, collapse, docking and resize retain existing behavior. Invisible resize
tolerance remains independent of the visual boundary under
[Workspace](WORKSPACE.md#resize-hit-targets-and-boundary-ownership).
Inspector/Appearance use shared controls; scrolling and localized tooltips preserve small-pane use.
The left navigation rail opens actual Rack, Graph, Editor, Inspector and Appearance surfaces.
Only implemented actions are offered. User-restored layout remains authoritative.

## Implemented R5-F1 Channel Rack composition

A pristine layout opens the Rack with a useful creation action, without inserting music. At wider
window sizes, a project navigator lists actual playback occurrences with canonical rhythm thumbnails;
it creates/selects Patterns. Narrow windows reclaim that space and retain the occurrence selector.
The compact Pattern name/header, length selector, sixteenth-note ruler and fixed row geometry make
the rhythm readable as music. Whole-bar extension repeats four beat groups without shrinking pads.
The 16–512 sculpted pads share a channel tint, active indicator and glow. The actual current column
uses the shared playing outline independently of checked state and keyboard focus; the footer gives
bar:beat:sixteenth position. Pattern deletion and channel menu deletion use ordinary danger/action
styles with target-aware Undo; the navigation rail toggle reclaims space.
Larger hit targets, persistent names and short contextual commands replace repeated step captions.
The scrollable grid preserves readable geometry when the pane is narrow; its local header/footer
retain creation/import access. Empty and unsupported states reflect actual project content.

Captured dragging paints or erases a continuous row, previews locally and commits once on release.
Escape/capture loss cancels. Row actions fill every four/two steps, rotate or clear through one
canonical transaction. Single-WAV drop and Add sound share managed acceptance. Arrows/Space/Enter
retain accessible step editing. Existing unsupported data is preserved, not converted to fit the UI.
The transport BPM opens an exact committed editor; meter remains a value. A physical Gain dial
complements exact amplitude and the level slider in Node Editor. No second parameter/history exists.
The transfer curve visualizes the actual mathematical setting; it does not pretend to measure audio.

## SEQ-UI-REDESIGN reference assessment

All eleven owner-supplied images were inspected. They are retained outside the repository at
`%LOCALAPPDATA%/Seqvium/DesignReferences/SEQ-UI-REDESIGN`; no reference assets are distributed.
The [review report](experiments/SEQ-UI-REDESIGN_REPORT.md) records implementation/evidence/limits.

| Reference | Useful design decisions | Adaptation / limitation |
| --- | --- | --- |
| Screenshot 1, SONAR step instrument | Bevelled drum pads, beat divisions, lit selected hits, substantial chassis | Keep tactile rhythm; enlarge tiny targets and remove repeated control labels |
| Screenshot 2, SONAR expanded channel | Parameter controls beside a channel and an expandable velocity lane | Use contextual row commands now; velocity requires supported note/intensity execution |
| Screenshot 3, FL Studio | Fast Channel Rack, compact transport, colored steps, instrument identity, graphical note overview | Rail/navigator and real rhythm thumbnails; avoid copying layout or adding unavailable channel knobs |
| Screenshot 4, EON ARP | Strong purple identity, coherent illuminated matrix, graphical musical focus | Transfer rhythm clarity and restrained light; preserve Seqvium's own palette |
| Screenshot 5, blue arpeggiator | Beat grouping and visually distinct parameter lanes | Reserve lanes for real parameters; do not preallocate disabled lanes |
| Screenshots 6–7, Logic | Track colors repeated in the grid, clear time ruler, integrated editing hierarchy | Apply row colors and common ruler; Arrangement remains a separate future surface |
| Screenshot 8, omniGRID | Dense legible rhythm and contextual per-note depth | Take direct editing and disclosure; avoid microscopic text and arbitrary knob inventory |
| FabFilter Pro-Q 4 image | Large purposeful analytical surface, curves, quiet tools around the signal | Actual Gain transfer fill/glow now; live spectrum/EQ require real processing and measurement |
| Serum image | Layered instrument chassis, finely graduated knobs, shape/value feedback | Graduated captured Gain dial; future instruments inherit shared materials with their own composition |
| Reason rack image | Physical panel depth, coherent rack hardware, color/material character | Raised panels, bevels and light; decorative hardware must not displace musical work |

The references justify material richness. Their tiny controls, cryptic abbreviations and excessive
simultaneous options are not requirements. Seqvium keeps their musical character while improving
target size, naming, disclosure, cancellation and canonical editing safety. Final visual acceptance
belongs to the owner; screenshots and automated checks establish evidence, not aesthetic approval.

## Professional DAW Workspace Design Target

This is an accepted long-term visual direction, not authorization to implement future stages or a
pixel-perfect design system. It complements [project vision](PROJECT_VISION.md),
[workspace behavior](WORKSPACE.md) and [UX](UX_CONTRACT.md); their ownership and accessibility contracts
remain intact. Owner-supplied FL Studio, Cubase, SunVox and modern FL Studio images were inspected as
complementary references. They are research context only: no reference images or proprietary assets
are copied into the repository, and no application's palette, icons or exact layout is adopted.

## First-party Visual Design Rules

These rules apply to all subsequent Seqvium first-party surfaces unless an explicit, justified UX
contract requires an exception. They extend the accepted R3 frame and pane behavior; they do not
authorize future editors or choose their layouts.

- **Workspace first:** every persistent field, row and action must justify its space. Keep musical
  working regions dominant; no decorative cards, welcome dashboards or future-editor placeholders.
- **One main command row:** group Project, Transport, History and Selection/parameter actions with
  restrained separators. Horizontal shortage uses explicit overflow/menu and contextual disclosure;
  never automatically multiply permanent toolbar rows or shrink fonts. Import, Play/Stop and access
  to Save must remain reachable at 640x480 DIP. Undo/Redo remain compact icon-only actions.
- **Global musical parameters:** show actual tempo and meter compactly near transport when space
  permits. BPM opens its real committed editor; meter remains read-only. Overflow/Inspector retains
  access at reduced width. Future meter editing should preserve that familiar location.
- **Icons:** use icon-only for recognizable actions, with localized accessible names, tooltips and
  shortcut hints; explain disabled reasons. Ambiguous commands may have short menu labels. Reusable
  first-party vectors share geometry, stroke weight, rounded joins, 16–20 DIP readability and platform
  DPI scaling. No emoji, font-dependent action glyphs, mixed icon families or borrowed application assets.
- **Semantic hierarchy:** Dark uses graphite surfaces and readable light text; Light uses cool neutral
  backgrounds and readable dark text. Central resources distinguish selection, keyboard focus, hover,
  pressed, unavailable, active transport, warning and critical actions. Semantic action
  accents, green transport, amber warning and moderate red critical feedback have distinct purposes.
  Jade action accents and channel colors now identify Seqvium. Color supplements shape/text/state.
  Material depth, shadows, glow and gradients are supported expressive tools; signal displays must
  represent actual data or clearly labelled mathematical relationships.
  Unselected working elements must remain distinguishable from their surrounding surface in both
  themes. Use modest surface separation and readable boundaries; selection/focus is additional
  emphasis, not the only way to locate a node or control. Review the actual canvas composition.
- **First-party dialog chrome:** compact internal confirmations, errors, properties and tools follow
  Seqvium typography, semantic surfaces, borders, control states and spacing. Compact owned modals
  have no ordinary Windows titlebar and no duplicate decorative header. Size height from content,
  center on owner, preserve true modality, cyclic Tab, safe initial focus, Esc/Alt+F4 and focus return.
  Save/Discard/Cancel are visibly distinct; initial focus/default must be safe. Native system file
  pickers are the explicit exception. External independently rendered plugin editors retain the
  [workspace exception](WORKSPACE.md#one-main-application-window).
- **Human labels:** ordinary surfaces show musical/source names and concise actual chain labels.
  UUIDs, internal capability keys, execution provenance and technical device details require explicit
  details/Inspector/diagnostic disclosure. Persistent blockers and canonical/last-valid divergence
  remain visible and meaningful; diagnostics must not dominate the music workspace.
- **Status economy:** use one compact source in the existing status bar. Normal state needs little
  text; failures say what is blocked/retained and expose useful details on request. Avoid duplicate
  opened/ready/inactive explanations in permanent rows.
- **Keyboard/accessibility:** preserve shortcuts, automation identity, legible focus and useful hit
  targets. Overflow preserves actions and focus return; visual density never removes functional access.
- **Field alignment:** parameter labels, editable text, units and actions share one vertical center
  line and coherent typography. Verify the actual inner template/text presenter, rather than adding
  compensating top margins. Parent controls must forward the intended content alignment; new rows use
  the [common row rules and check](DESIGN_SYSTEM.md#field-and-row-alignment).
- **Real GUI review:** inspect rendered windows at 1100x750, 640x480 and maximized/1920x1080 with
  RU/EN, Dark/Light, normal/blocked actions, tooltips, focus, native picker return and owned modals.
  Exercise pane collapse/restore/dock/hide/reopen and real document actions. Unit tests alone are not
  visual acceptance; retain review screenshots locally under the existing evidence policy.

R4-F3-A implements `Presentation/HostIcons`, `Styles/HostControls.axaml` and shared palette roles
`Surface.Hover/Pressed/Selected/Playing/Critical/Accent`, `Status.Playing/Critical`, extending existing
Dark/Light resources. A single retained row presents real project/transport/history actions, selection
and linear Gain (0–1); BPM is editable and meter remains read-only, with narrow-window overflow access. Save As lives
in overflow; selection moves into details when space is limited. The content-sized owned unsaved modal
uses the same resources. The details flyout is a compact view of canonical intent, **not a node canvas**.
The owner conditionally accepts F3-A for continued development; final canvas/interaction acceptance
of F3-B remains pending. See the
[integration evidence](experiments/SEQ-R4-F3-A_REPORT.md).

### Transferable reference principles

- FL Studio: flexible internal composition, compact global controls and simultaneous specialized music
  editing surfaces; take coexistence and fast contextual access, not an exact window arrangement.
- Cubase: arrangement-first hierarchy, useful track density, detailed time structure and integrated lower
  working areas; take meaningful allocation between overview/editing/mixing, not mandatory fixed panes.
- SunVox: compact modular visualization with explicit meaningful node relationships, a distinct inspector
  and little decorative overhead. Its extreme tracker text density is not the application's default
  typography or an instruction to make a tracker/editor now.
- Modern FL Studio: coherent Browser/Arrangement/Mixer composition, shared visual language and useful
  horizontal/vertical space allocation. Do not copy promotional Browser artwork, proprietary assets,
  persistent developer counters or the particular control inventory.

No referenced feature must be implemented or visible by default. Purposeful color grouping, strong
hierarchy and progressive disclosure are shared principles; copying a populated screenshot is not.

### Workspace-first composition and functional density

The workspace is the primary product surface. Persistent chrome, navigation, status and preferences use
only a small justified fraction of available space. Musical content takes precedence over documentation,
development status, onboarding and configuration. Empty workspace is preferable to decorative placeholders.

Favor compact predictable controls, toolbars, tracks, inspectors and editors. Reject oversized cards,
large corner radii, excessive padding/whitespace, repeated explanations, giant ordinary-object headings,
persistent unavailable-future-feature prose and primary-screen framework/version/build details.
Density must preserve readable text, keyboard access, useful pointer targets and correct platform DPI.
Choose dimensions deliberately from hierarchy/content, never microscopic typography or indiscriminate
whole-application scaling. Accessibility and readable minimums are constraints, not density tradeoffs
to silently discard. Shared metrics evolve with verified composition; early shell dimensions are not immutable constraints.

### Specialized musical surfaces and multi-pane composition

Future Pattern, Piano Roll, Arrangement, Mixer, Browser, Graph and Sample Lab share a coherent host
language while keeping task-specific interaction and useful density:

- Arrangement prioritizes tracks, clips, time structure and editing.
- Piano Roll prioritizes notes, pitch/time grids and velocity/parameter editing.
- Mixer prioritizes compact channels, levels and processing controls.
- Browser prioritizes navigable sources and fast audition.
- Graph prioritizes readable nodes, ports, meaningful connections and spatial navigation.
- Pattern and Sample Lab prioritize their actual musical/exploration actions and supported context.

Do not force every surface into identical cards, lists or generic forms. A pane is a substantial working
surface, not a decorative container. The long-term single-main-window workspace must accommodate useful
overlap/floating, clear activation/front behavior, independent sizing, collapse/restore and later
user-controlled docking/magnetism under [WORKSPACE](WORKSPACE.md#flexible-composition-and-activation).
Not every editor must remain visible. Layout preferences remain separate from canonical music.
The bounded F2 host above implements this direction for two optional host surfaces; it grants no
authorization for specialized musical editors or a general docking hierarchy/registry.

### Stable responsive frame

Design deliberately for ordinary desktop work, including one 1920x1080 display, smaller windows and
platform DPI changes within declared evidence. Prefer reflow of small command groups, collapsing
secondary panels, pane-local scrolling, resizable working regions, contextual tools and bounded usable
minimums. The primary application frame stays stable: no default whole-window scrolling, permanent
large headers, global font/control shrinking or unreachable offscreen work areas. Actual timelines,
notes, channels and other editor content may scroll horizontally/vertically where musically appropriate.

### Visual hierarchy, color and control economy

Use deliberate instrument materials, substantial panels, bevelled controls, bounded light and shadow,
readable boundaries and consistent typography. Rich decoration is welcome when it reinforces the
instrument's character and preserves manipulation and visual hierarchy. Dark is the preferred primary working presentation; Light remains coherent
and functional. Distinguish active context, selected material, keyboard focus and disabled/unavailable
states. Sparse accents and purposeful musical colors explain track/clip/pattern/context organization,
activity and relationships, rather than merely decorate. Essential information also needs non-color cues.

Keep frequent actions compact and close to the relevant musical context. Advanced actions may use
context menus, secondary panels or inspectors. Do not give every preference a large button and paragraph;
do not hide essential workflows solely to make screenshots clean. Progressive disclosure controls
complexity without making frequent work laborious or the interface a settings application.

### Honest functionality and GUI review

Never fabricate tracks, editable notes, transport, meters or processing controls ahead of implementation.
A sparse structurally correct R3-F1 host is acceptable; visual richness must arrive from actual useful
functionality. No musical domain change or reopening of R1/R2 follows from these references.

For each substantial GUI change, answer through actual rendered windows/screenshots and interaction:

1. What proportion of visible space is available for actual musical work?
2. Does persistent chrome justify the space it occupies?
3. Are common controls compact and immediately reachable?
4. Can relevant working surfaces coexist without excessive obstruction?
5. Does resizing preserve reachable content and useful minimum dimensions?
6. Is scrolling local to the appropriate musical surface?
7. Is layout understandable with RU/EN text and Dark/Light themes?
8. Are focus, selection, disabled state and active musical context distinguishable?
9. Are elements functional, rather than decoration or future-development descriptions?
10. Does this support sustained music production rather than occasional configuration?

When a substantial editor arrives, review representative dense and sparse content, not only its empty
default. Unit tests are necessary evidence for applicable invariants, not visual acceptance. Record actual
OS/DPI/size/input bounds and missing evidence. Owner visual review remains required; the recorded R3-F1
visual observations describe reserved workspace capacity, not musical work or multi-pane acceptance.
Generated screenshots remain local under [evidence retention](experiments/README.md#evidence-retention),
not product assets or a default Git deliverable.

## Hierarchy, chrome, and density

- Let musical material and the current action lead the hierarchy. Selection and active context should
  be clear without making infrastructure the main visual object.
- Pane chrome should make movement, activation, and available actions discoverable while remaining
  quiet enough for music to dominate. [WORKSPACE](WORKSPACE.md) owns pane behavior.
- Use compact controls for ordinary work and reveal expanded depth deliberately. Compactness must
  preserve readability and useful interaction targets, not compress everything into dense machinery.
- Keep selection, focus, parameter feedback, and pane conventions coherent between surfaces. Different
  creative tasks may need different density without looking like unrelated applications.
- Avoid engineer-tool aesthetics: walls of permanent connectors, diagnostics, and configuration
  fields must not define the ordinary musical workspace.

### Sample Lab interaction hierarchy

Lead with the sound and the next creative action: generation, audition, related variation and explicit
acceptance. Recent comparison/history helps return to useful alternatives without making a branching
tree or parameter console the primary surface. Reveal meaningful supported locks and deeper controls
on demand, with held constraints and their applicability legible. Reference, audible choice, current
project sound and acceptance scope need distinct understandable feedback; color alone cannot carry
lock, stale, pending or unavailable meaning. Temporary history must not look permanently saved.
[Sample workflow](SAMPLE_WORKFLOW.md#intentional-and-lazy-exploration) owns behavior and
[UX](UX_CONTRACT.md#sample-lab-exploration-feedback) owns action/discoverability requirements. Use the
host localization and semantic theme foundation; no final layout, control inventory, icons or gestures
are selected.

### Full node editor visual proposal

**SEQ-NODE-EDITOR-IMPL-F1 — bounded implementation of the accepted first slice.** The first-party editor
has a compact scope/identity row, a task-specific working region and restrained applicable
status. A real EQ curve, instrument controls or analyzer can occupy substantial space; generic cards
or identical generated forms must not limit those workflows. Tabs/presets belong only when functional
for the supported node. [Graph](NODE_GRAPH.md#full-node-editors-proposal) owns capability and the useful
Gain attenuation/transfer-graph slice; [Workspace](WORKSPACE.md#node-editor-placement-proposal) owns
placement and retained sizing.

Gain uses its node name once in the pane title, a short source/scope line, and a graduated physical
dial beside the level slider, exact linear value and derived dB. The dial supports relative vertical
movement, Shift fine adjustment, double-click unity, arrows/Home/End and cancellation; all surfaces
share the same canonical parameter and one draft/commit owner. Unity/Silence sit below the level control. The labelled analytical
transfer graph is a compact, initially collapsed Curve disclosure; its formula is available on
request. This deliberately refines DESIGN-R1 following owner review: primary musical controls take
precedence over permanently displayed teaching text and a large graph. The graph remains distinct
from live signal metering. Preserve semantic RU/EN and Dark/Light resources,
usable controls, independent focus/selection/target cues and local reflow/scrolling at reduced sizes.
Do not enlarge a single value into an empty major surface or show future-feature placeholders. Final
default/minimum dimensions require rendered layout and interaction evidence; an analytical graph is
not evidence for a live analyzer or a future EQ's usability.

Each empty/unavailable pane has at most one useful content hint. GraphCanvas owns its empty graph
message; an empty selector and footer are hidden. Node Editor hides absent identity/scope/status rows
and keeps one short content hint, with a distinct warning for a deleted target. Healthy Gain needs no
idle explanatory footer: reserve that region for drafts, changed-elsewhere feedback, diagnostics and
matching-scope last-valid state. The main toolbar hides occurrence/Gain controls when their data is
absent; an explicit compact selection in Details does not repeat a static selection heading.
Invalid amplitude remains visible in the exact input with a warning and repair path. Hide the
undefined dB/curve instead of repeating an invalid-value label in every control. A draft uses a short
label and a dashed curve; explanatory release/commit text need not occupy the working area.
Changed-elsewhere feedback appears only when an intervening accepted edit cancels a real draft,
and expires after three seconds. Initial loading, idle refresh and own commits do not create it.
For the same four-part target and parameter, the visible expanded Node Editor takes priority over
Graph quick Gain and the toolbar. Otherwise the visible Graph's selected Gain takes priority over
the toolbar's same Gain. Different processing targets retain their controls; hiding/collapsing the
editor restores quick access. Healthy canonical readiness and selected-node facts need no footer.

## Responsive layout and usable minimums

First-party Seqvium UI uses responsive/reflowing layout, not uniform graphical scaling of the
application. Prefer reflow, collapsing secondary content, pane-local scrolling/overflow, resizing flexible
regions, and preserving minimum usable sizes as available space changes.

Do not continuously shrink fonts, icons, mixer strips, knobs, click/touch targets, or other primary
controls below readable/usable minimums to fit more content. A mixer strip may need a minimum useful
width and horizontal scrolling rather than becoming an unreadable sliver. Exact dimensions and
breakpoints remain future visual-design work. Correct platform DPI scaling is still required and is
distinct from shrinking the UI to fit a window. Independently rendered third-party editors are outside
this visual contract; [WORKSPACE](WORKSPACE.md) owns main-window and pane behavior.

Validate content at its supported pane minimum as well as the main-window minimum in both languages
and themes. Check long/zero/invalid values and disclosed content, not only nominal values. Measure
actual template minimums and scroll extents: a compact idle tool must not acquire a scrollbar or
clip an action because invisible template space remains. Disclosures must stretch their working
content to usable width. Preserve deliberate manual resizing and provide scrolling when expanded
content exceeds a user-selected size, without shrinking labels or input targets.

## Processing and node-canvas presentation

F3-B implements a compact first-party canvas using 156-unit nodes, 22-unit port rows, bounded curved
cables with surface under-strokes at crossings and restrained 3-unit body rounding. Source, Gain,
Mix and Output have human type labels, directed input/output labels and source/parameter/layout detail.
Selection, keyboard focus, previews and invalid elements have distinct outlines/text/semantic colors;
an exclamation mark supplements error color. Hover/context feedback explains affected diagnostics.
`Graph.Cable`, `Graph.Grid`, `Graph.Node` and `Graph.NodeBorder` extend HostPalette equally for Dark/Light.
Unselected nodes use a slightly lighter Dark fill, white Light fill and defined boundaries so they
remain visible against the grid. The pane-local row contains explicit target, create/import/frame/delete/context actions;
the main command row remains unchanged. Gain exposes one compact invariant text input on selection,
with linear ratio semantics, retained invalid intent and explicit commit. Technical IDs stay in the
existing requested details only. Pan/zoom affect canvas content, never global typography or document
coordinates; framing a dense graph is an overview, and ordinary zoom/pan retains access at narrow width.
[F3-B GUI evidence](experiments/SEQ-R4-F3-B_REPORT.md) records actual sparse/dense RU/EN Dark/Light
review, input and window-size bounds. Owner visual/manipulation acceptance remains pending.

Processing presence should have a compact, clearly interactive indicator on relevant musical items
and containers. Its exact icon, badge/count, color, label, typography, and motion remain unselected.
The indicator reveals the relevant graph pane under [UX_CONTRACT](UX_CONTRACT.md); full node maps
normally remain hidden until requested.

The canvas should support free two-dimensional node placement, readable connections, and clear
selection. Personal spatial organization is presentation state; [NODE_GRAPH](NODE_GRAPH.md) owns
topology-defined processing. Node settings should offer depth on demand. The compact-chain proposal,
settings/inspector placement, and optional parameter exposure are not final visual contracts.

## Feedback and motion

Make keyboard focus, document selection, active pane and relevant command target sufficiently legible
without treating them as one visual state. Essential invalid/unavailable state and pending operations
need non-color/structural cues where applicable, such as shape/outline, labels, icons and concise
context explanation. Color and transient animation may supplement these cues but cannot be their
only carrier. Pane chrome, controls and shortcut hints should support discoverable keyboard access
under [UX](UX_CONTRACT.md#essential-accessibility-feedback) and
[WORKSPACE](WORKSPACE.md#keyboard-access-and-focus-return). Exact focus visuals, contrast/dimensions,
accessibility-tree representation and APIs need later design/evidence; no certification is claimed.

Invalid or unpublished graph edits must clearly differ from the graph currently executing under
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution). Use concise status/iconography,
affected node/connection highlighting, restrained color with non-color cues, and concise inline/context
explanations. Persistent missing-capability and execution blockers remain visible at affected elements;
optional global count/navigation may help reach them. Disappearing notifications are supplementary,
logs are diagnostic support, and the workspace must not become a diagnostics dashboard. Keep feedback
visible without interrupting normal editing with cascading modal
dialogs; [UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures) owns exceptions
where a modal decision is genuinely required. Exact visuals remain open.

Use immediate visual/audio feedback to make manipulation understandable and invite experimentation.
Motion, where useful, should explain a change, continuity, or state with restraint. Coherent interaction
quality is the desired "vibe"; decorative animation and visual noise are not requirements.

[UX_CONTRACT](UX_CONTRACT.md) owns observable interaction and discoverability. Future visual work
should develop this guide from concrete workflows rather than fill in a complete theme spec prematurely.

## Themes and semantic resources

Dark and Light themes are baseline product directions, with room for additional restrained visual
themes later. A theme is primarily a coherent resource/style set, not a complete novelty UI redesign.
First-party Seqvium UI and Seqvium-native plugin surfaces consume centralized semantic UI resources
rather than hard-code concrete colors/fonts throughout individual plugins.

The R3 shell needs a small host-owned semantic foundation sufficient for the surfaces actually built:

- Surface background and readable primary/secondary text establish hierarchy.
- Selection, focus and outlines distinguish selected targets, keyboard focus and boundaries.
- Accent/action emphasis and warning/error, disabled/unavailable and pending-state treatment support
  the states exposed by the shell; later panes extend this vocabulary only where actual workflows need it.
- Common control sizing/spacing and typography roles preserve readable hierarchy and usable targets
  under the responsive-layout contract.

The implemented v2 vocabulary is cataloged in DESIGN_SYSTEM, with source-owned colors, dimensions,
typography and material recipes. These host keys are not a public extension API. Reusable icon/resource
roles may grow with real editors; no unused future parameter inventory is required in advance.

Dark/Light provide coherent alternative resources for the same UI semantics, not different document
models. Main workspace chrome, Browser/project-resource surfaces, node graph, Sample Lab and first-party
Seqvium-native extension UI share the host roles as those surfaces arrive. Extensions should not
independently recreate each Dark/Light style. Justified future plugin customization may coexist with
the shared foundation; it does not establish a parallel mandatory style system.

Theme changes preserve musical state
under [Architecture](ARCHITECTURE.md#preference-changes-and-locale-aware-presentation)
and observable preference-change safety under [UX](UX_CONTRACT.md#language-and-theme-preference-changes).
Selection, focus, warnings/errors, invalid graph state, disabled actions and pending work retain sufficient
structural/non-color cues under [feedback](#feedback-and-motion). Color alone cannot carry essential
meaning. No screen-reader compatibility, contrast certification or platform validation is claimed.

Host baseline roles must remain usable when a noncritical contributed style/icon is missing or invalid:
use a safe host role, simple host indicator or meaningful label rather than an unreadable/broken essential
control. A complete unavailable contributed theme falls back coherently to a usable host baseline;
the exact choice and preference-retention mechanism remain open. Contributor removal follows
[Extensions](EXTENSIONS.md#ui-resource-contribution-lifecycle); arbitrary malformed external UI is not
promised recoverable. Independently rendered third-party native editors may keep their own theme,
language and visual behavior; surrounding host UI still uses host resources.

Theme packaging as extensions/plugins/data packages, exact resource/API/token schema and visual choices
remain Q-055; additional restrained themes are possible without promising a marketplace, third-party
theme SDK, dynamic compiler, live reload or customization editor. Host localization ownership belongs
to [ARCHITECTURE](ARCHITECTURE.md#host-localization-and-ui-resources); staged introduction belongs to
[Roadmap](ROADMAP.md#localization-and-theme-foundation-ownership). No theme packages or localization
extension resources are introduced by this direction; R3-F1's first-party catalogs/palettes are implemented above.
