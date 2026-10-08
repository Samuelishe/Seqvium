# UI design

Role: Evolving visual and interaction design guide.
Read when: Designing visual hierarchy, pane chrome, controls, indicators, or graph presentation.
Authoritative for: Durable visual principles, usable responsive layout, interaction density, consistency, progressive visual complexity, semantic theme/style resources.
Not authoritative for: Workflow semantics, pane lifecycle, graph execution, or a final design system.

## Design intent and scope

Seqvium should feel like a creative instrument. Pleasant manipulation, tactile interaction, spatial
clarity, and visual quality are product requirements under [PROJECT_VISION](PROJECT_VISION.md).
This is a small evolving guide, not a complete design system. Deliberate later UI design stages must
establish final colors, typography, pixel dimensions, icons, theme details, control library, and window mockups.
Dark/Light baseline direction is accepted below; exact visuals remain unselected and no UI is implemented.

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
application. Prefer reflow, collapsing secondary content, scrolling/overflow, resizing flexible
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
visible without interrupting normal editing with cascading modal dialogs; [UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures) owns exceptions
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
illustrate roles only; they are not accepted API identifiers. Reusable icon/resource roles may be added
where justified; the shell does not need every future graph or Sample Lab token in advance.

Dark/Light provide coherent alternative resources for the same UI semantics, not different document
models. Main workspace chrome, Browser/project-resource surfaces, node graph, Sample Lab and first-party
Seqvium-native extension UI share the host roles as those surfaces arrive. Extensions should not
independently recreate each Dark/Light style. Justified future plugin customization may coexist with
the shared foundation; it does not establish a parallel mandatory style system.

Theme changes preserve musical state under [Architecture](ARCHITECTURE.md#preference-changes-and-locale-aware-presentation)
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
resources are introduced by this direction.
