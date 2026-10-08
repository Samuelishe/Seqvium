# UI design

Role: Evolving visual and interaction design guide.
Read when: Designing visual hierarchy, pane chrome, controls, indicators, or graph presentation.
Authoritative for: Durable visual principles, usable responsive layout, interaction density, consistency, progressive
visual complexity, semantic theme/style resources.
Not authoritative for: Workflow semantics, pane lifecycle, graph execution, or a final design system.

## Design intent and scope

Seqvium should feel like a creative instrument. Pleasant manipulation, tactile interaction, spatial
clarity, and visual quality are product requirements under [PROJECT_VISION](PROJECT_VISION.md).
This is a small evolving guide, not a complete design system. Deliberate later UI design stages must
establish final colors, typography, pixel dimensions, icons, theme details, control library, and window mockups.
Dark/Light baseline direction is accepted below; R3-F1 implements a restrained initial shell, not the final visual
system.

## Implemented R3-F1 visual baseline

The shell uses compact flat chrome with project title/unsaved state, one narrow canonical tempo/meter
and preference toolbar, a quiet full-width workspace and a narrow audio-inactive/status strip. Dashboard
cards, hero headings and introductory/future-feature paragraphs are absent. Version and bounded
capability information appear only in the functional About flyout, not in the ordinary workspace.
System-default font/fallback and simple textual window glyphs require no borrowed assets.

`HostPalette` provides identical Dark/Light keys: `Surface.Background/Raised/Workspace`,
`Text.Primary/Secondary`, `Border.Default`, `Focus.Active`, `Accent.Action`, `State.Unavailable`,
`Status.Warning`. No speculative error/pending indicator is displayed. Shared `HostMetrics` supplies
`Type.Body/Caption` (12/11 DIP), `Chrome.Height` (30), `Toolbar.Height` (30), `Status.Height` (22),
`Control.Height` (24), `Control.Corner` (2) and `Space.Gap` (6). These are bounded initial shell metrics,
not a final pane API or a mandate to shrink future editor content to this density.
XAML consumes semantic brushes rather than per-control theme colors. Fluent supplies base control state
behavior, while host styles supply shell surface/accent/focus. Color is accompanied by text/shape/state.
Tests verify complete roles and text contrast; actual Dark/Light screenshots and small-window reachability
are evidence only for the implemented Windows shell. Theme switching retains window, controls and focus.

## Implemented R3-F2 pane presentation

Optional Inspector/Appearance surfaces use 28-DIP headers, 1-DIP boundaries, minimal rounding and the
existing semantic resources/body typography. Active panes have a distinct outline and stronger title;
keyboard-focused actions retain the separate visible control focus treatment. The actions, collapse,
close and resize affordances are compact; dock actions live in a deliberately opened menu. Floating
resize strips are 5 DIP with 10x10-DIP corners taking priority. Title/actions/content are inset clear
of those regions; the 28-DIP title row retains compact typography. The bottom-right marker is inside
its actual corner hitbox; the remaining bottom edge uses a vertical cursor. Header title space uses
a move cursor; action buttons own arrow cursors with normal hover/pressed/focus styling. Docked panes
expose only their horizontal inner divider. Close (×) and Collapse (−) remain distinct. A 28-DIP
bottom strip appears only while a pane is visible/collapsed, with an upward collapse marker and readable
localized titles. Hidden/default panes occupy no persistent workspace surface. Inspector rows contain
real project facts; Appearance offers two real preference actions, without decorative cards or fake editors.
Pane-local scrolling preserves readable content at minimum size. RU titles use ellipsis/tooltips when
needed. No palette, font scaling scheme or full control framework is introduced. The
[F2 visual observations](experiments/SEQ-R3-F2_REPORT.md#visual-observations-and-local-review-artifacts)
record actual overlap, dock/floating, collapse, small geometry and Light. Generated captures are local
review artifacts, not committed assets; the professional DAW target below remains the review direction.

## Professional DAW Workspace Design Target

This is an accepted long-term visual direction, not authorization to implement future stages or a
pixel-perfect design system. It complements [project vision](PROJECT_VISION.md),
[workspace behavior](WORKSPACE.md) and [UX](UX_CONTRACT.md); their ownership and accessibility contracts
remain intact. Owner-supplied FL Studio, Cubase, SunVox and modern FL Studio images were inspected as
complementary references. They are research context only: no reference images or proprietary assets
are copied into the repository, and no application's palette, icons or exact layout is adopted.

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
to silently discard. R3-F1's compact metrics are starting values, not immutable future dimensions.

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

Use restrained predominantly flat surfaces, thin separators, subtle surface contrasts, minimal rounding
and consistent typography. Dark is the preferred primary working presentation; Light remains coherent
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

## Processing and node-canvas presentation

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

These are responsibility categories, not final tokens, palette values, dimensions, fonts or a complete
catalog. Concepts such as `Surface.Background`, `Text.Primary`, `Accent.Primary` and `Status.Warning`
illustrate future categories only; the bounded implemented host keys above are not a public extension API. Reusable
icon/resource roles may be added
where justified; the shell does not need every future graph or Sample Lab token in advance.

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
