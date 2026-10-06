# Workspace

Role: Main-window workspace-pane behavior and layout-state contract.
Read when: Designing pane placement, activation, floating, docking, collapse, or layout restoration.
Authoritative for: Internal pane composition, user-controlled docking, activation/front behavior, layout ownership.
Not authoritative for: Pane-specific editing, final visuals/gestures, framework Z-order API, or project schema.

## One main application window

Major working surfaces should normally be internal **workspace panes** in one main application window.
Examples include Channel Rack / Pattern workspace, Piano Roll, Arrangement, Mixer, Node Graph,
Sample Lab, Browser, and future recording/editor surfaces. They are not independent operating-system
top-level windows in the normal workspace model. Floating here means floating inside the main workspace.

The main workspace/pane infrastructure is core platform responsibility. A plugin may contribute a
surface through host contracts without controlling the host workspace. [ARCHITECTURE](ARCHITECTURE.md)
owns platform boundaries; each musical owner defines its pane's work. No UI exists yet.

## Flexible composition and activation

Where appropriate, a pane should support moving/resizing, floating above other panes, activation by
interaction, docking/snapping with another pane, separating from a docked group, and minimizing or
collapsing into an unobtrusive internal shelf/strip.

Clicking or otherwise interacting with a partially obscured pane must activate it and bring it in
front of overlapping panes. This is observable behavior; Z-order/Z-index is a possible implementation
mechanism, not a selected framework contract.

Minimization/collapse and restoration must preserve the pane's working state rather than destroy its
editing context. Exact retained selection, per-pane state, and lifecycle details need design for each
surface. The workspace must not require every possible pane to remain open simultaneously.

## User-controlled docking and magnetism

Each pane should have its own magnetic/docking preference: it may allow docking/magnetism or remain
freely floating. Exact settings may be boolean or richer; no schema is chosen.

User intent takes priority over automatic layout. Snapping must be predictable and easy to escape.
Approaching an edge alone must not aggressively capture a pane or fight free placement. Dock previews
may be explored later. Dock-group identity, allowed relationships, gestures, and nesting remain open;
do not assume an unlimited IDE-style docking hierarchy.

## Layout ownership and restoration

Workspace layout is primarily application/user workspace state, not musical project content. Opening
a project should not normally rewrite the user's preferred pane arrangement. Pane layout is distinct
from project-side node/connection data or graph-editor layout described in [NODE_GRAPH](NODE_GRAPH.md).
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns document serialization, not user workspace preferences.

Named/saved workspaces or optional project-specific layouts may be considered if real usage shows
value; they are not initial requirements. Exact persistence of positions, sizes, minimized state, and
dock relationships remains open.

Restored geometry must eventually be clamped/adapted safely to changed main-window size, DPI, or
display environment so panes remain reachable. Main-window composition must stay responsive and
respect [UX_CONTRACT](UX_CONTRACT.md). Final visuals, layout algorithms, storage, and pane-specific
geometry policies remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
