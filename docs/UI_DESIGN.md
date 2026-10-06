# UI design

Role: Evolving visual and interaction design guide.
Read when: Designing visual hierarchy, pane chrome, controls, indicators, or graph presentation.
Authoritative for: Durable visual principles, interaction density, consistency, and progressive visual complexity.
Not authoritative for: Workflow semantics, pane lifecycle, graph execution, or a final design system.

## Design intent and scope

Seqvium should feel like a creative instrument. Pleasant manipulation, tactile interaction, spatial
clarity, and visual quality are product requirements under [PROJECT_VISION](PROJECT_VISION.md).
This is a small evolving guide, not a complete design system. Deliberate later UI design stages must
establish final colors, typography, pixel dimensions, icons, themes, control library, and window mockups.
None is selected here, and no UI is implemented.

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

Use immediate visual/audio feedback to make manipulation understandable and invite experimentation.
Motion, where useful, should explain a change, continuity, or state with restraint. Coherent interaction
quality is the desired "vibe"; decorative animation and visual noise are not requirements.

[UX_CONTRACT](UX_CONTRACT.md) owns observable interaction and discoverability. Future visual work
should develop this guide from concrete workflows rather than fill in a complete theme spec prematurely.
