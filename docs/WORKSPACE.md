# Workspace

Role: Main-window workspace-pane behavior and layout-state contract.
Read when: Designing pane placement, activation, floating, docking, collapse, or layout restoration.
Authoritative for: Main-window chrome/keyboard window actions, pane reachability/escape and focus return, internal
panes, user-controlled docking, activation/front behavior, layout ownership.
Not authoritative for: Pane-specific editing, final visuals/gestures, framework Z-order API, or project schema.

## Implemented R3-F1 host boundary

One actual main OS window hosts identity, compact canonical project summary, preference actions and
`WorkspaceRegion`. R3-F2 adds the bounded internal host below while retaining the accepted F1 frame.

On Windows the restrained custom 30-DIP header has named/focusable minimize, maximize/restore and close
buttons. Native styles and Avalonia decoration roles preserve title dragging and border resizing;
Alt+Space opens the actual OS menu after Alt release, Win+Up/Down and Alt+F4 retain OS behavior.
The menu uses installed user32 only. It follows OS language, independently of shell RU/EN.
Other platforms keep native decorations/actions; custom header action buttons are hidden there.
No TopMost, repeated foreground forcing or global hotkey registration is used. Normal initial activation
is left to the platform. Header contrast/border, active-state tooltip and visible keyboard focus distinguish state.

Initial client size is 1100x750 DIP; minimum 640x480 DIP. A compact 30-DIP toolbar and 22-DIP status strip
leave the rest to the quiet workspace; the main frame has no scrolling. Long titles/status use ellipsis
and full tooltips, not global shrinking. About is a functional dismissible flyout with version/scope,
not a permanent workspace explanation. Windows PerMonitorV2/layout scaling are enabled, but actual F1 evidence is
single-monitor
96 DPI, not mixed/high-DPI acceptance. Tab reaches window and preference actions; Enter/Space execute them.
[F1 report](experiments/SEQ-R3-F1_REPORT.md) records bounded real-window evidence and remaining checks.

## Implemented R3-F2 internal panes

The toolbar's Panes action deliberately opens Project Inspector or Appearance. Both are optional and
hidden in a missing/default layout. Inspector projects actual open `ProjectDocument` name/status,
tempo/meter and Pattern/sound/resource counts; Appearance invokes the existing language/theme actions.
No invented music, editor controls, audio startup or dashboard is introduced.

`WorkspaceState` owns only placement, visibility, docking permission, active identity and bounded
back-to-front ordering. Type IDs (`project-inspector`, `appearance`), singleton instance IDs (`project-inspector.main`,
`appearance.main`) and localized title keys are distinct. These two capabilities
support one instance each; this is not a policy for all future editors or a plugin registry/interface.
`WorkspaceHost` lazily creates and retains pane/content controls until host disposal. Collapse, hide,
overlap, docking and preference refresh do not replace them. The content owner projects its document
or holds its own controls/scroll context; geometry operations never select or edit musical material.
Keyboard focus and semantic command targets are not fields of the layout model.

Interaction with exposed content/header activates and brings that pane forward. Front order is a
permutation of the bounded existing entries, rendered with compact indices; repeated activation never
increments an unlimited z-index. The bottom 28-DIP strip reaches every visible/collapsed pane even if
it is fully covered. Hidden panes leave the strip and reopen through Panes, retaining the same content
within the session. Collapse removes only the working surface, with an upward marker on its strip action.

Floating title/header content moves within the workspace. All four edges and four corners resize;
each resize keeps its opposite edge/corner fixed and clamps only the dragged coordinates. Blocked
pointer travel is discarded, so reversing from a boundary or minimum changes the visible edge immediately.
Movement and workspace reflow use separate containment rules. Captured gestures retain their starting
rectangle, pointer origin, active edges and workspace size, and save on release, not every move.
Pane buttons own arrow cursors and never start a move/resize; resize retains focus already inside content.
Escape, capture loss,
application deactivation, workspace resizing or a competing pane operation cancel the gesture and
restore its starting placement. Native main-window chrome is outside this host. All geometry uses
workspace DIPs, no screen coordinates; the canvas clips safely and preserves the entire header and
working rectangle. Current minima are 280x210 DIP (Inspector) and 280x170 DIP (Appearance); content
may scroll locally at its minimum. A theoretical workspace smaller than its minimum contains the pane
within the available area; the normal main-window minimum remains 640x480.

Dock left/right and Float are explicit pane-menu actions. Each edge holds at most one visible pane;
an occupied-edge request returns its previous occupant to its retained floating placement. Opposite
edges each use at most half the workspace width and full usable height, so both remain usable at the
supported window minimum. A dock's inner-edge grip resizes width while preserving independent floating
coordinates/width/height. The separate dock width is stored within its actual permitted range and is
relimited on reflow; rendering does not silently clamp a larger hidden width. Left/right workspace anchors
remain fixed. Docked panes have horizontal inner-divider resizing only; undocking restores all floating
edges/corners and the last valid floating rectangle. Floating surfaces may overlap docks. Collapsed/hidden dock
placements consume no
edge; restoring them applies the same conflict rule. Disabling Allow docking undocks that pane and
disables its dock actions without changing another pane's preference. There is no magnetic snapping,
hover-to-dock, nested layout tree, dock group, detached OS pane or named workspace profile.

Tab reaches pane/strip/menu actions; Enter/Space execute them and normal menu arrows/Home/Enter choose
actions. F6 / Ctrl+Tab switch available panes in stable capability order; Shift reverses traversal and
collapsed entries restore. Hidden entries require deliberate reopening. With the pane-actions button
focused, Ctrl+Arrows move a floating pane and Ctrl+Shift+Arrows resize (docked width only). Ctrl+W closes
a pane only while focus is inside the workspace. Close is labeled "Close pane" / "Закрыть панель",
with the pane title in tooltip/accessibility names and Ctrl+W as the close accelerator. It retains the
Hidden state/content; Collapse remains a distinct action. Escape cancels a gesture, dismisses a menu or returns
to Panes without a focus trap. Restore uses a live visible pane-actions button; hiding the final pane
returns focus to Panes. No disposed-control focus reference is retained. These local bindings introduce
no global hook or shortcut manager and preserve Alt+Space, Win+Up/Down and Alt+F4.

Layout restore adapts positions using the saved usable workspace dimensions while retaining readable
DIP sizes, then clamps negative/nonfinite/oversize positions and sizes. Window resizing recomputes usable
bounds and edge widths; it does not shrink typography or introduce whole-window scrolling. Last floating
geometry is separate from a docked rectangle. Storage/failure/shutdown semantics are owned once in
[Settings](SETTINGS.md#implemented-r3-f2-workspace-layout-storage).
[F2 correction evidence](experiments/SEQ-R3-F2_REPORT.md#geometry-and-chrome-correction) records actual
Windows interaction/cursors and screenshots. Owner interactive acceptance is still pending;
high/mixed DPI, cross-monitor and other OS runtime are not accepted from pure geometry tests.

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
owns platform boundaries; each musical owner defines its pane's work. The initial two host panes are
implemented; specialized musical panes remain future work.

## Main-window chrome and foreground behavior

The main Seqvium window should not have an ordinary prominent system title bar. Minimize, maximize,
and close controls live in Seqvium's own window chrome/body. The window must not be TopMost by default
or aggressively steal focus from other applications; avoid unnecessary foreground activation/focus
grabs. Internal pane activation below is distinct from activating the application over another app.

This first-party direction does not prescribe independently rendered external plugin editor visuals.
Custom chrome must preserve normal keyboard-accessible window actions, including move, resize,
minimize, maximize/restore and close where appropriate to the platform. Essential controls must not
be pointer-only. Exact bindings, Avalonia/platform mechanics, accessibility APIs and dimensions remain
open; OS window behavior and platform/DPI interaction need later validation. Custom chrome is not
evidence of cross-platform correctness or accessibility compliance.

## Keyboard access and focus return

Essential workspace actions need keyboard reachability: enter/switch useful panes, invoke relevant
actions, restore collapsed or obscured working surfaces, and leave panes/overlays without a focus trap.
An intentionally modal interaction may scope input, but must provide a clear completion/cancellation
and return path. Activation through keyboard interaction must make the working pane reachable/visible
under the activation contract below. Neither every visual object nor every graph node needs a global
tab stop; detailed traversal and escape bindings remain open.

Follow [UX focus and targets](UX_CONTRACT.md#focus-selection-and-command-targets): pane activation,
keyboard focus, document selection and current command target are distinct. Restoring or focusing a
pane must not silently change its document target merely because a different pane/selection was last
active. Target-following versus retention and multiple graph panes remain Q-035 decisions.

Returning from a detached/native editor, or closing an overlay, should restore a useful still-valid
host focus/context predictably, without stale target resurrection or aggressive foreground grabs.
If the prior host target/pane is unavailable, use a safe understandable host context without silently
redirecting an editing command. Host routing must respect external editor input ownership under UX;
concrete native focus capture/restoration and accessibility integration still require platform evidence.

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

Each pane has its own docking permission in the bounded F2 host; broader magnetic gestures remain future
work. A pane may remain freely floating independently of other panes.

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
F2 persists the bounded placement/visibility/permission/order representation above; named arrangements
and richer relationships remain future work.

Workspace preferences/layout belong in the platform-appropriate user configuration area under
[SETTINGS](SETTINGS.md#user-configuration-and-project-state). Application/plugin preference reset
must not delete projects or project-managed media. Language/theme and the F2 pane-layout files remain
independently owned under Settings.

Restored geometry must be clamped/adapted safely to changed main-window size, DPI, or
display environment so panes remain reachable. Main-window composition must stay responsive and
respect [UX_CONTRACT](UX_CONTRACT.md). Final visuals, layout algorithms, storage, and pane-specific
geometry policies beyond the bounded F2 implementation remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
