# UI design

Role: Evolving visual and interaction design guide.
Read when: Designing visual hierarchy, pane chrome, controls, indicators, or graph presentation.
Authoritative for: Durable visual principles, interaction density, consistency, progressive visual complexity, semantic theme/style resources.
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

Invalid or unpublished graph edits must clearly differ from the graph currently executing under
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution). Use concise status/iconography,
affected node/connection highlighting, restrained color coding, and short contextual/transient
notifications where useful. Keep feedback visible without interrupting normal editing with cascading
modal dialogs; [UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures) owns exceptions
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

Leave room for semantic colors, typography/font roles, common sizing/style resources, and reusable
icon/resource roles where justified. Concepts such as `Surface.Background`, `Text.Primary`,
`Accent.Primary`, and `Status.Warning` illustrate roles only; they are not accepted API identifiers.
Centralization should let future theme evolution avoid manually redesigning hundreds of first-party
plugins. Independently rendered third-party external native editors need not adopt Seqvium themes.

Theme packaging as extensions/plugins/data packages remains open, as do final resource/API and
visual choices. Host ownership and first-party localization consumption belong to
[ARCHITECTURE](ARCHITECTURE.md#host-localization-and-ui-resources); no theme packages or localization
resources are introduced by this direction.
