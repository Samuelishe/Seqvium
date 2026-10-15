# Workspace

Role: Main-window workspace-pane behavior and layout-state contract.
Read when: Designing pane placement, activation, floating, docking, collapse, or layout restoration.
Authoritative for: Main-window chrome/keyboard window actions, pane reachability/escape and focus return, internal
panes, user-controlled docking, activation/front behavior, layout ownership.
Not authoritative for: Pane-specific editing, final visuals/gestures, framework Z-order API, or project schema.

## Retained Sample Browser

`sample-browser.main` / `sample-browser` is the ninth retained pane, initially hidden, with a preferred
left dock and a compact 320-DIP default width. Existing layouts add it hidden. It shares ordinary
floating/docking/resizing/collapse/hide/restore and independent user-layout persistence. The content
uses local virtualized file scrolling with a pinned metadata/waveform/action footer; narrow panes
retain accessible controls rather than scaling their text. FullHD supports Browser, Rack and Playlist
together. While Browser occupies the left dock, Pattern cards yield their width and the existing
occurrence selector returns; hiding/undocking Browser restores navigation. This is view composition,
not musical selection/history. Browser remains available with the calm empty Rack via Ctrl+B/rail.

Folder navigation/selection are session presentation; pinned/recent/last locations use independent
[access preferences](SETTINGS.md#browser-access-preferences). No native tool window, second project,
audio engine or media ownership model is introduced. Hide/collapse cancels temporary work/preview;
shutdown joins Browser operations before document/audio cleanup.

## Retained compact Mixer pane

`mixer.main` is a retained workspace pane, reachable from the rail/Panes menu and a Rack sound's
Open Mixer action. It uses the existing floating/docking/collapse/layout persistence and focus owner.
Default floating bounds occupy roughly one fifth of FullHD, not the whole application. Strip widths
and fader height remain fixed; growing the pane reveals more channels instead of inflating controls.
The channel bank scrolls locally and Master stays in its own pinned column. A footer toggle reveals
the selected channel's processing/routing inspector at the side. Small panes scroll each area locally;
close the inspector to recover bank space. No music/pane scale transform is used.

Selecting a strip is context, not a document edit. Controls commit through the shared workflow/history,
and workspace changes cancel unfinished gestures. Source assignments belong to music; Playlist lanes
remain organization/audibility, not Mixer IDs. Compact shared chrome, Rack/Playlist geometry and Pattern
cards support simultaneous desktop tools. [UX](UX_CONTRACT.md#implemented-mixer-workflow) owns musical
gestures; [design](UI_DESIGN.md#compact-studio-instrument-direction) owns the new density direction.

## Implemented R3-F1 host boundary

The Desktop entry point obtains user-wide exclusive ownership before creating Avalonia, preferences,
document or audio state. A second launch requests ordinary activation of the existing window and exits;
unresponsive IPC gives a bounded diagnostic instead of another workspace. See
[application lifecycle](ARCHITECTURE.md#desktop-process-ownership).

One actual main OS window hosts identity, compact canonical project summary, preference actions and
`WorkspaceRegion`. R3-F2 adds the bounded internal host below while retaining the accepted F1 frame.

On Windows the shared custom instrument header has named/focusable minimize, maximize/restore and close
buttons. Native styles and Avalonia decoration roles preserve title dragging and border resizing;
Alt+Space opens the actual OS menu after Alt release, Win+Up/Down and Alt+F4 retain OS behavior.
The menu uses installed user32 only. It follows OS language, independently of shell RU/EN.
Other platforms keep native decorations/actions; custom header action buttons are hidden there.
No TopMost, repeated foreground forcing or global hotkey registration is used. Normal initial activation
is left to the platform. Header contrast/border, active-state tooltip and visible keyboard focus distinguish state.

Default startup is maximized with a 1400x900-DIP normal restore size; minimum 640x480 DIP. Header, toolbar and status
geometry use
HostMetrics; their v2 instrument materials use HostMaterials. The remaining area belongs to music; the main frame has no
scrolling. Long titles/status use ellipsis
and full tooltips, not global shrinking. About is a functional dismissible flyout with version/scope,
not a permanent workspace explanation. Windows PerMonitorV2/layout scaling are enabled, but actual F1 evidence is
single-monitor
96 DPI, not mixed/high-DPI acceptance. Tab reaches window and preference actions; Enter/Space execute them.
[F1 report](experiments/SEQ-R3-F1_REPORT.md) records bounded real-window evidence and remaining checks.

## R4-F3-A commands and owned modal lifecycle

The single-window chrome/toolbar/status contract remains, with v2 shared dimensions and materials.
One command row groups project, transport and history with an actual occurrence selector/Gain,
committed transport BPM editor, read-only meter and
existing preference/pane actions. Reduced width moves secondary commands/selection/parameters into
explicit overflow/details; Inspector retains musical parameters. No new editor pane or toolbar row
is added. Project replacement updates retained Inspector content without changing pane identity. New
uses the empty-document composition; opening music can restore the retained musical layout.
Technical details require deliberate disclosure; the existing status strip carries persistent blockers.

Native storage pickers remain OS-owned modals. The unsaved first-party confirmation is an owned,
content-sized Avalonia modal with no system decorations, centered on its owner; initial focus is Cancel,
Tab cycles, Esc/Alt+F4 cancel, and focus returns safely. Main window remains alive on Cancel/failed Save.
New project is a toolbar command and Ctrl+N. Like Open, it offers Save/Discard/Cancel for dirty work;
Cancel, cancelled Save As and failed Save retain the current document and playback. Successful New
joins audio/preparation and creates an unnamed clean document with default tempo/meter, empty music,
fresh lifecycle/history, no file association and no audio startup.
Shutdown waits outstanding import/action and joined document/audio cleanup before workspace/preference
shutdown. Ctrl+N/I/O/S/Shift+S, Ctrl+Z/Y, F5/Shift+F5 and Ctrl+Esc complement existing R3 pane/window keys.
[UI design rules](UI_DESIGN.md#first-party-visual-design-rules) apply to all future first-party dialogs;
external editor windows and native file pickers retain their explicit exceptions.

## R4-F3-B retained graph pane

The bounded host additionally contains `node-graph.main` / `node-graph`, titled Node Graph (RU: Граф
узлов), with a 960x520-DIP default floating rectangle and 300x260 minimum. It uses the same retained
move/resize/collapse/hide/reopen/dock/activation machinery; there is no extra Windows pane window or
general surface registry. Panes, project overflow and Ctrl+G explicitly open the selected occurrence's
attachment; requesting the same target focuses the retained canvas without resetting its viewport.
The pane's own target selector explicitly changes editing target and playback selection. Other global
selection changes do not redirect graph editing. A layout-restored pane without a target asks for one.

Editing identity is lifecycle plus attachment, resolved against a coherent immutable workflow observation.
Deletion retains the unavailable attachment identity for possible Undo, with no eligible editing commands;
Undo may restore that target but never revives a cancelled gesture. Project replacement clears target
and selection even when saved IDs match. Every pane refresh first adopts one complete observation,
including refreshes triggered by shell notifications before the pane's queued workflow callback.
Retained content unsubscribes on host disposal and drops old document observations on replacement.

Pane gestures remain header/boundary-owned. Canvas owns middle/Space+left/background pan, wheel zoom
around pointer (0.35–2), node and port drag, selection and context actions. Home frames; +/- zoom,
Alt+Tab selects successive nodes, arrows nudge (Shift = ten units), Delete removes selection and
Apps/Shift+F10 opens context actions through Avalonia's context request on key release. Context menus
also select nodes/cables and create/bind/connect/disconnect for keyboard access. Text fields own local
input/Undo; Gain commits with Enter/check.
Canvas focus returns after graph actions; pane-actions focus retains R3 geometry chords. Competing
pane actions, reflow, deactivation, Escape and capture loss cancel canvas proposals. Viewport and
selection are session presentation, absent from musical history and user layout files.
Right press focuses/selects the canvas hit without opening a menu. The single handled ContextRequested
path opens at the pointer after release, or at canvas centre for a keyboard request. The menu stays
open for pointer movement, nested hover and left-click selection; action, Escape, outside click,
deactivation or explicit pane/target/document cancellation closes it. Escape closes the whole graph
menu and returns safe canvas focus. Toolbar Add and Actions anchor their menu below their own button;
toolbar-created nodes retain the existing canvas-centre creation position. Canvas and pane capture-loss
handlers cancel only their own captured gestures, not ordinary child/button implicit capture release.
Opening/dismissing menus changes no canonical state or history; graph commands retain existing Undo
transactions. The [repair evidence](experiments/SEQ-R4-F3-B_REPORT.md#context-menu-lifecycle-and-node-creation-repair)
separates native input from deterministic commands and controlled actual-window lifecycle checks.
Old two-pane version-1 layouts remain readable; missing graph entries start hidden.
[Node graph](NODE_GRAPH.md#implemented-r4-f3-b-first-party-editor) owns editing and DSP semantics.

## R5-F1 retained step sequencer

`step-sequencer.main` / `step-sequencer` is one static pane, titled Channel Rack (RU: Каналы).
Empty startup/New opens only this musical pane without creating content. Musical layout visibility
is retained separately while the document is empty and reapplied when musical content is opened.
Panes or the left Rack rail action opens it. Default floating bounds are 960×390 DIP, minimum 360×290; the application's
640×480 minimum is unchanged. Its own horizontal/
vertical scrolling preserves the 16–512-step content in narrow floating/docked panes. Existing header,
activation, focus, collapse/hide/reopen, boundary resizing and left/right docking are reused.
Older layouts omit this pane and restore it hidden; only geometry/visibility/docking persist in user
layout, with no musical/editor model there.

The left rail opens existing Rack/Graph/Node Editor/Inspector/Appearance panes without replacing them.
At window width >= 1000 DIP a project navigator lists actual occurrences with Pattern thumbnails.
Its selection/create commands use DesktopWorkflow; navigation itself does not edit the document.
Cards use up to two name lines, full-name/gesture tooltips and a width that grows with the host.
Selection leaves pane visibility/focus alone; double-click/Enter and the contextual editor/channel
menu open an editor. Shared session editor intent belongs to PatternEditorNavigation, not layout storage.
A rail toggle hides/restores it; the session choice is preserved across window resizing. Below that
width it is hidden, reclaiming workspace space. It is session presentation, not persisted
musical content or a separate layout registry.

The pane follows the workflow's selected occurrence. Its selector appears only for multiple choices;
the Pattern name is editable canonical content. Focus returns to an enabled step, otherwise Create.
Hidden/collapsed content stays alive and cancels drafts/actions. Generations/selection/replacement
invalidate held actions; reopening resolves current state. Picker return resolves its frozen musical
target rather than whichever object later becomes selected. [UX](UX_CONTRACT.md#implemented-r5-f1-step-sequencing)
owns editing/refusals. No dynamic instance registration or second transport is introduced.

## Retained Playlist pane

`arrangement.main` / `arrangement` is the fixed Playlist pane, opened by rail or Panes. It shares
the retained floating/docking/collapse/hide/restore lifecycle and independently stored workspace
layout. Historical layouts add it hidden; empty startup opens only a compact Rack. The pane cancels
clip,
overview and track-editor drafts on deactivation, hide/collapse, shutdown or invalid authority.
Reopening retains content/view and adopts the current document. Focus returns to the timeline.

Source choice and song transport/track/clip tools wrap within the pane; only its timeline scrolls.
Track headings remain stationary during horizontal navigation. The overview, fit and follow affect
view state without editing music or inventing audio. Musical identity/commands and renderer status
remain workflow-owned under [Playlist UX](UX_CONTRACT.md#implemented-playlist-arrangement).
There is no dynamic pane registry, external window or second project/transport.

## Retained Piano Roll pane

SEQ-R9 adds type `piano-roll`, singleton instance `piano-roll.main`, default 1080×580 and minimum
360×320 DIP. It is the seventh retained capability, initially hidden; older layouts acquire its
hidden default without changing existing entries. Rail/Panes opens the selected Pattern/Part;
Rack label double-click, melody preview and row menu explicitly open that Part. Returning to Rack
does not convert musical data. Hide/collapse cancels captured drafts, retaining controls/camera and
selection within the session; disposal withdraws local actions. Music remains project-owned, view
state session-only, pane geometry independent user-layout state.

Main-window adaptive navigation and canvas containment settle after actual arrange. Hiding navigation
sets its column to zero, and queued workspace reflow invalidates arrange so visible pane bounds match
state bounds after repeated actual-window reductions. This is reachability/layout repair, not music editing.
[Piano UX](UX_CONTRACT.md#implemented-seq-r9-piano-roll) owns gestures and shared-target semantics.

## Drum Voice editor placement

The Drum Voice uses the existing singleton Node Editor, with a useful 720×740-DIP initial floating
composition clamped to the available workspace. Opening a different drum target can enlarge an old
compact Gain pane; repeated same-target opens retain the user's size. The existing 300×210 pane
minimum remains supported through local vertical scrolling and reflow, rather than shrinking knobs
or fields. The sound/processing target remains explicit and independent of playback selection.
Gain's compact default/disclosure and ordinary pane lifecycle are unchanged. Layout saves geometry,
not instrument drafts, audition pitch/velocity or a document target. See
[interaction](UX_CONTRACT.md#drum-voice-sound-design) and [evidence](experiments/SEQ-FIRST-DRUM_REPORT.md).

## Node editor placement proposal

**SEQ-NODE-EDITOR-IMPL-F1 — one reusable internal pane accepted and implemented for the first slice.**
The [graph owner](NODE_GRAPH.md#full-node-editors-proposal) states capability;
[Architecture](ARCHITECTURE.md#node-editor-state-and-lifetime-proposal) owns target identity/lifecycle.
Q-032 remains open for independent editors and future Workspace composition.

| Alternative                                    | UX and architectural consequence                                                                                                                                                                                       | Recommendation                                                                                |
|------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| One reusable internal Node Editor pane         | Explicit target, existing resize/dock/collapse/focus behavior; one node at a time makes comparison sequential                                                                                                          | Accepted and implemented first slice; another explicit open replaces the target               |
| Independent internal panes for different nodes | Simultaneous EQ/instrument comparisons and independently retained tabs/views; demands distinguishable target titles, duplicate-open activation, bounded creation/retirement, reachable overflow and resource ownership | Keep as a later option; do not emulate instances by preallocating arbitrary static slots      |
| External OS editor windows                     | Appropriate native-editor integration when embedding is unsafe/impractical; platform-specific parent/focus/resize/close behavior and more window-management overhead                                                   | Preserve the accepted third-party exception; do not make it the default first-party mechanism |

The current registry is static in more than content creation: `WorkspaceState.Definitions`, definition
lookup, restore/reflow and stable keyboard traversal; `WorkspaceHost` constructs fixed strip actions;
`WorkspaceLayoutStore` recognizes only registered IDs/types and bounds saves by definition count.
Unknown saved entries mark fallback and preserve the original file. `WorkspacePane` also specializes
refresh, focus and cancellation by content type. Multiple node instances therefore require coherent
instance registration/removal, host/strip reconciliation and a layout compatibility decision, not
just a control factory. Do not introduce these costs or a public surface API for the first singleton.

The fixed `node-editor.main` / `node-editor` identity uses retained pane machinery with narrow content
refresh/focus/cancel integration. A human node/processing-scope breadcrumb appears in content and
accessibility labels; pane identity differs from target identity. The default is 360×220 DIP with
a 300×210-DIP pane minimum. Gain's optional Curve uses the available width; opening it raises a
smaller visible floating pane to 350 DIP high, and closing restores its preceding height unless the
user resized it meanwhile. Docked panes retain their workspace height. Local scrolling preserves
expanded content/diagnostics at smaller user-selected sizes. This is the initial content
default, not a maximum for future specialized editors. Docking remains explicit;
existing half-workspace dock limits and conflict behavior apply, so rich editors may need to float.

Repeated Open Editor for the same logical target restores/activates the existing surface and retains
view state. Another explicit node request cancels transient input and binds the new target;
selection does not follow automatically. Changing the Graph pane target or playback selection alone leaves
the Node Editor attached to its original target, with a clear scope label and no playback retargeting.
Close pane means Hidden as today, not node deletion; collapse/hide cancels drafts/gestures but retains
the session target and safe view context. Reopen resolves the current observation. Return focus to
the live invoking graph when useful, otherwise to a reachable host action; never retain a disposed
control as authority. Escape first cancels local editing before workspace escape handles it.

Save only geometry/visibility/docking in the user layout; node target, draft, tab/scroll context and
document lifecycle remain session-local in this slice. Existing layouts missing the new entry start
it hidden; a restored visible editor has no target until explicitly opened. An older binary reading
the new entry enters its existing preserve-original fallback. A retained prior three-pane binary
verified fallback, write refusal and exact preservation of the added entry's original bytes;
this is not lossless downgrade editing. Dynamic per-node layout persistence is deferred.

## Main window and music layout

A missing main-window placement opens maximized, with a useful normal restore size. Version-1
workspace layout optionally stores normal window dimensions in DIPs, screen position in pixels and
maximized state, independently of the usable workspace dimensions. Saved pane geometry is replayed
against the settled startup workspace; a temporary normal-sized arrange must not permanently shrink
panes saved in a maximized window. Minimized is never a startup state.
Restore selects an available monitor and clamps the rectangle to its working area; saved pane order,
docks and floating sizes retain their existing adaptation. Single-monitor Windows restoration is
verified; mixed DPI and monitor changes remain wider evidence gaps.

Pristine startup and New show one compact Rack near the workspace's upper left. Other retained panes
are hidden while the document is empty; their geometry and the preceding musical composition survive.
Opening music restores that composition. With no preceding layout, a Song opens Rack above Playlist
(or beside it on a short wide workspace); a Pattern project keeps Rack. Panes → Arrange Rack + Playlist
deliberately applies this two-pane composition. It changes only those panes' placement, docking and
visibility, never music. Empty-document persistence retains the last musical composition with current
main-window placement. Shutdown joins main-window and pane storage together.

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

A left click on free main Workspace background deactivates the active pane and focuses the workspace
canvas. Geometry, front order, graph selection, explicit Node Editor target and playback target remain
independent and unchanged. The nullable active identity survives layout save/restore and hiding another
pane; the next pane interaction activates it normally. Resize-boundary arbitration retains priority
over free-space clicks. No inactive/empty-selection status is added.

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
[F2 correction evidence](experiments/SEQ-R3-F2_REPORT.md#resize-hit-targets-and-boundary-arbitration) records actual
Windows interaction/cursors and recorded visual observations. The owner accepts F1 and the current
bounded F2 workspace usability/interaction behavior. R3's completion audit finds no missing foundation prerequisite;
high/mixed DPI, cross-monitor and other OS runtime are not accepted from pure geometry tests.

### Resize hit targets and boundary ownership

The visible border remains 1 DIP. Floating edge tolerance is 6 DIP inward and 6 DIP outward from the
actual rendered rectangle (12 DIP total); corner regions extend 24 DIP inward and 6 DIP outward on
both axes (18x18 DIP). Corners take precedence over ordinary edges of the same pane. The workspace
clips outward tolerance at its perimeter; controls and visible neighbours can reduce the available
region. The pane content now reserves the existing inward tolerance with an inset and explicit transparent
frame hit surfaces, plus a visible corner grip. Drawing/scroll content cannot own those frame surfaces;
ordinary controls outside the reserved frame keep their input ownership.

`WorkspaceHost` resolves hover and pointer-down through one bounded resolver using actual arranged
pane rectangles in workspace DIPs, visible state, back-to-front order and the operations available
for the placement. It does not derive boundaries from retained floating geometry for docked panes.
Selection rules, in order:

1. Buttons, text/edit controls, selecting/range controls, scroll thumbs, menu items and other focusable
   control sources/ancestors retain input ownership. Their normal cursor/action wins over resize.
2. Hidden/collapsed panes and coordinates outside the usable workspace are unavailable.
3. If the pointer is on a visible pane rectangle, only the topmost rectangle at that point may supply
   a boundary. Another pane's outward tolerance never reaches through that working surface.
4. A candidate whose corresponding edge/corner point is covered by the interior of a higher pane is
   unavailable even when its expanded tolerance reaches an otherwise empty gap. Merely touching
   rectangle boundaries does not obscure either pane's inward target.
5. In empty workspace where tolerances compete, choose the nearest available edge/corner point (Euclidean distance); an
   exact tie chooses the frontmost pane. The current visible front order
   resolves coincident seams too. Active identity itself is not a hit-test input.

Hover shows the chosen direction cursor and accents only the intended edges at the existing 1-DIP
boundary. This cue stays below higher panes; it neither activates/reorders a pane nor changes focus,
document selection or command targets. Layout/front-order changes refresh it with the pointer stationary.
Pointer-down resolves before activation, freezes instance identity and edges in one gesture, then
activates/brings forward and captures immediately. An inactive pane resizes on that first press.
Activation/reordering and crossing another boundary cannot retarget capture. Original anchors,
blocked-travel reversal, focus already inside the pane, exact placement rollback and layout commit
on release are preserved. Escape cancels resize without moving focus out of the pane; capture loss,
deactivation, reflow and competing operations retain the same cancellation rules.

### Independent, aligned and shared boundaries

**Independent floating panes:** nearby, touching or aligned edges establish no shared ownership or
size constraint. Each visible side retains its own inward region. At an exact touching seam the front
pane wins; in an empty gap the distance/front-order rule applies. Hover identifies the intended pane
and edges before capture; only that pane resizes.

**Magnetic alignment (future):** visual edge snapping would be an alignment convenience. It must not
automatically establish a persistent dock relationship or shared size constraint. Magnetic snapping
is not implemented by this correction.

**Explicit linked/docked boundaries (future):** when a supported layout declares one structural boundary
between two panes, it should own one workspace splitter rather than competing local resize handles.
Such a splitter may resize both adjacent panes within their declared minimum sizes. Its ownership and
hit testing must be independent of activation; pointer-down freezes splitter identity. No arbitrary
dock groups, docking trees or coupled floating resize are implemented here.

**Current limited left/right docks:** the two widths remain independent, each at most half the usable
workspace. Each exposes only its horizontal inner edge. When the inner edges meet, the left/right
visible sides select their respective panes; the exact seam selects the front pane. Nearby dividers
with competing tolerance in the gap use distance, then front order. A gesture changes only its chosen
dock width, preserving its workspace anchor and the neighbour's width. Shrinking opens a gap; expansion
at the half-width limit saturates normally. This is deterministic independent-boundary resizing, not
an implemented shared splitter or coupled divider.

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
