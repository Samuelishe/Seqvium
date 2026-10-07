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

This direction applies to normal major Seqvium-authored surfaces. Arbitrary third-party/native plugin
editors need not be internal Workspace Panes or visually match Seqvium. When internal overlapping-pane
integration is unsafe, impractical, or unsuitable for the required integration model, a plugin editor
may use a normal top-level OS window. This is an accepted exception, not a change to the first-party
workspace direction. Seqvium owns the plugin/editor relationship and normal lifecycle under
[EXTENSIONS](EXTENSIONS.md#external-plugin-lifecycle-and-editors); the plugin developer owns its UI.

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

## Sample Lab and processing-graph targets

Sample Lab is a dedicated standalone workspace pane, movable/resizable under the general pane
contract, retaining useful exploration state. It can work without a target or against selected
musical context; [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns those entry modes and audition/acceptance.
Contextual work does not require a new top-level OS window. No special Sample Lab docking policy
is selected.

The active musical target/context is project/application interaction state, distinct from pane geometry
and layout. Collapsing/restoring a useful working surface should preserve its context under the general
retention contract; target lifetime, selection-following, and cross-project behavior remain open.

Opening an item's/container's processing indicator should open/focus its relevant Node Graph pane.
If a graph pane already displays that target, normal behavior should favor focusing/updating that
existing surface over uncontrolled duplicate windows. Whether multiple independent graph panes may
show different targets remains open; neither a hard single-instance rule nor unlimited instances
are accepted. [NODE_GRAPH](NODE_GRAPH.md) owns target graph semantics, not pane geometry.

## Layout ownership and restoration

Workspace layout is primarily application/user workspace state, not musical project content. Opening
a project should not normally rewrite the user's preferred pane arrangement. Pane layout is distinct
from project-side node/connection data or graph-editor layout described in [NODE_GRAPH](NODE_GRAPH.md).
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns document serialization, not user workspace preferences.

The speculative named/saved workspace and optional project-specific layout possibilities are retained
as [I-001](IDEAS.md#i-001--optional-saved-workspace-arrangements); they are not initial requirements.
Exact persistence of positions, sizes, minimized state, and dock relationships remains open.

Workspace preferences/layout belong in the platform-appropriate user configuration area under
[SETTINGS](SETTINGS.md#user-configuration-and-project-state). Application/plugin preference reset
must not delete projects or project-managed media; exact configuration paths/formats remain open.

Restored geometry must eventually be clamped/adapted safely to changed main-window size, DPI, or
display environment so panes remain reachable. Main-window composition must stay responsive and
respect [UX_CONTRACT](UX_CONTRACT.md). Final visuals, layout algorithms, storage, and pane-specific
geometry policies remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
