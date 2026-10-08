# UX contract

Role: Product contract for observable interaction.
Read when: Designing editing, audition, workspace behavior, or user-facing actions.
Authoritative for: Progressive complexity, input/command and focus semantics, baseline accessibility expectations,
feedback, discoverability, general workflow semantics.
Not authoritative for: Pixel-perfect design, DSP behavior, serialization, or current implementation.

These are intended requirements except the explicitly implemented bounded R3-F1/F2 host below; musical UI remains future
work.

## Mouse-first creation and complementary input

**Mouse-first, keyboard-efficient** means direct pointer manipulation is a first-class, polished path
for creation and editing, and basic workflows do not require memorized shortcuts. Users must be able
to place, move, resize, and select notes/events; edit steps; move/resize clips; select waveform ranges
for trimming/processing; manipulate node graphs; adjust parameters; and organize the workspace directly
with the pointer. Drag/drop, selection, context operations and parameter manipulation remain excellent.

The ordinary computer keyboard is also a first-class path for precise/repetitive editing, navigation,
selection changes, action invocation, modifiers, Undo/Redo and focus/accessibility access. Its efficiency
is complementary, not secondary in quality or dependent on a separate musical/document model.
Exact gestures and bindings remain open; this direction does not make Seqvium keyboard-first or
shortcut-dependent.

An on-screen musical keyboard should support audition/simple playing. Future MIDI devices, MIDI
recording, and realtime note input complement pointer editing and converge on compatible musical
event data. Audio-interface/microphone recording is also long-term direction, with execution and
latency owned by [AUDIO_ENGINE](AUDIO_ENGINE.md).

A typing keyboard / QWERTY piano is not default global behavior and must not consume ordinary command
space by default. It may later be offered as an optional, explicitly activated musical-input mode,
with clear mode/focus boundaries and a discoverable exit. Entering, leaving or changing its context
must not make normal application commands unpredictably unavailable. Exact piano layout, mode-specific
command coexistence and routing remain open.

### Semantic actions and input composition

An action describes the requested outcome and intended target/scope, independently of how it is
invoked. Pointer gestures, keyboard bindings, menus/context menus and future accessibility/action
surfaces may request the same operation under the
[application boundary](ARCHITECTURE.md#semantic-actions-and-input-boundary).

For example, dragging a clip, a keyboard nudge and a context-menu Move action express the same
placement-edit semantics, including applicable placement/context relationships. Keyboard movement
must not invent a second edit model or bypass canonical validation/history. Input surfaces may differ
in preview or granularity; the accepted edit and its Undo meaning belong to the operation.

The following is an illustrative movement case, not accepted bindings or step sizes:

| Input example           | Requested intention                                     |
|-------------------------|---------------------------------------------------------|
| Left / Right            | Precise/small nudge of the intended clip/item selection |
| Modifier + Left / Right | Larger/coarser nudge of that same target                |

Separate intentional presses normally remain separate edits; a continuous held-key repeat may form
one bounded same-intent session under [Undo grouping](#undo-grouping-and-interaction-preview).
Changing target, an unrelated command or completing the interaction ends grouping; elapsed time alone
does not define intent. Input device alone must not produce radically inconsistent history for the
same canonical operation. No repeat delay/rate, grouping time constant or step size is selected.

Modifiers are legitimate gesture/command composition: they may adjust precision, magnitude or mode,
including constrained movement, temporary snapping or selection extension. Resolve conflicts
deliberately across relevant contexts, rather than assigning contradictory behavior ad hoc per control.
No global Shift/Ctrl/Alt meaning, exact modifier or final interaction mechanics is fixed here.

### Focus, selection and command targets

Keep these concepts distinct even where they correlate:

| Concept                    | Meaning                                                       |
|----------------------------|---------------------------------------------------------------|
| Keyboard focus             | The surface/editor currently receiving keyboard input         |
| Musical/document selection | Selected project material in the relevant editing context     |
| Active workspace pane      | The pane currently activated under the workspace contract     |
| Current command target     | The explicit target/scope to which a requested action applies |

Keyboard interaction should move focus or selection through relevant timeline items, notes/events,
graph elements, panes and menus/actions where useful. Every visual object need not be an individual
tab stop; contextual navigation and action access should avoid tab-stop noise. Common graph selection,
deletion, navigation, opening settings and command invocation must have room for keyboard access
without precision pointer work. Free spatial layout does not imply mouse-only interaction. Detailed
node/timeline navigation and a full keyboard graph editor are not defined here.

Routing must respect the current input context and intended command target. Typing/editing in a text
field, rename editor, numeric value editor or search/browser field must not also trigger unrelated
global project edits. Context-local input and explicitly applicable host actions need deliberate
precedence; an ambiguous, absent or unavailable target must not silently redirect a destructive edit
to another selection/pane. This is semantic routing, not a chosen focus manager or event API.

Third-party/native editors own their local input; typing there must not accidentally trigger unrelated
host edits. Re-entering the host must predictably restore a useful, still-valid host context rather
than use stale targets or steal focus. [WORKSPACE](WORKSPACE.md#keyboard-access-and-focus-return)
owns pane reachability, escape and detached/native-editor return behavior. Concrete routing, input
capture and platform integration remain open.

### Binding identity and platform conventions

Semantic action identity is independent of physical keys and default bindings. Preserve room for
user-configurable bindings without making domain code know physical keys or making later rebinding
require rewriting editing operations. Binding preferences follow the
[user configuration boundary](SETTINGS.md#user-configuration-and-project-state).

Actions remain portable across Windows, Linux and macOS, while default bindings may differ to respect
platform conventions and reserved/system shortcuts. Identical physical shortcuts on every OS are not required;
[PORTABILITY](PORTABILITY.md#product-target-and-current-evidence) owns target and evidence limits.
No complete binding editor, persistence format, conflict-resolution UI, import/export, profiles,
chords/sequences or exact platform defaults is selected.

## Progressive complexity and discoverability

Expose controls relevant to the current musical task. Advanced capability may be available without
permanently occupying the primary workspace. A simple default must allow deeper editing rather than
cap the user's work. Important actions must not exist only as undocumented shortcuts. Useful commands
need discoverable paths where appropriate through menus, context menus, visible/contextual controls,
a deliberately opened workspace or later command/search surfaces; shortcut hints can teach efficient
alternatives. Final menus and a command palette are not designed here.

Relevant musical items/containers should visibly indicate processing with a compact, clearly
interactive control. Graph complexity normally stays hidden; opening that indicator reveals the
relevant Node Graph workspace pane. No final icon, count, badge, color, label, or typography is chosen.
[UI_DESIGN](UI_DESIGN.md) owns presentation principles, and [NODE_GRAPH](NODE_GRAPH.md) owns graph semantics.

## Immediate feedback and minimum ceremony

Enabling a step, moving a note, adjusting a filter, or auditioning a candidate should reveal the
musical consequence quickly. Interaction should remain responsive while audio execution continues
independently; exact latency targets need evidence rather than invented numbers.

The ordinary add path should approach:

```text
+ Instrument -> choose instrument -> play/write music
```

Internal routing may exist, but ordinary defaults should work without creating and wiring unrelated
objects manually. A sound should be audible before a user has mastered the full workstation model.

## Editing semantics

Step Sequencer and Piano Roll should eventually provide compatible views of musical events. Moving
between them must not require recreating a phrase. The intended model and shared pattern references
are owned by [ARCHITECTURE](ARCHITECTURE.md).

Patterns are user-named reusable units that may contain parts for multiple instruments. Users choose
granularity, such as separate bass/lead patterns or a combined groove. Organizational instrument/channel
groups are separate from pattern membership and identity; names/grouping must not force musical structure.

Repeated placements normally share Pattern musical content; editing it changes all those placements.
Musical-content independence and sound-definition independence are separate intentions. Conceptual
`Make Pattern Variation` detaches/copies musical content without implicitly detaching every sound,
resource, or processing relationship; conceptual `Make Sound Independent` addresses the sound definition.
Placement-local position, processing, or future fades do not modify every use of the Pattern/resource.
There must not be one ambiguous linked/unlinked state or only a hidden unlink-everything operation.
Make meaningful sharing understandable, for example by indicating multiple uses of a Pattern or sound
definition. Exact commands, indicators, and UI remain open. [ARCHITECTURE](ARCHITECTURE.md) owns identities.
Arrangement tracks and mixer channels have different purposes and must not force a misleading
one-to-one conceptual model.

Audio resources and musical placements also have distinct identities. Ordinary processing of one
item should be non-destructive and should not silently change every use of shared audio. Users
normally reason about item-local and containing-container processing, while global mixer/bus/master
responsibilities remain separate. [ARCHITECTURE](ARCHITECTURE.md) owns that bounded local model and
the semi-free Arrangement direction; no final "Layer" term or Track schema is accepted.

Atomic events do not automatically expose full local DSP graphs. A low-ceremony action should let a
user select one event and make an independently processed standalone fragment/item when needed,
without learning internal decomposition. Exact command and transformation remain open.

Sample Lab supports standalone and contextual exploration. Contextual audition is temporary and
reversible until explicit acceptance and should preserve relevant existing downstream processing.
Object sample creation includes the object's own semantic/local processing boundary; audible-selection
capture serves a broader selected musical/time context. The operations are distinct and their names
are conceptual. Replacing an object with audio containing baked processing must be undoable and must
not silently reapply that exact baked chain. Specialized audition, acceptance, candidate history,
and resampling semantics are owned by [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md). Missing capabilities should be
represented explicitly according to [EXTENSIONS](EXTENSIONS.md), with user work retained.

### Edit target and sharing feedback

Opening an editor from a placement must make its editing scope understandable. Musical editing names
the referenced Pattern and affects its shared musical content; it does not silently create a private
copy. Placement editing instead affects that occurrence's position, supported local range/timing and
item-local processing. Moving one occurrence does not move other uses or reorganize its definition.
Sound settings identify the sound definition/use; containing effects identify the processing context;
media-level work identifies the durable resource. Focus or selection alone cannot establish ownership.

Use stable contextual target/scope and meaningful sharing information so users can anticipate affected
uses, with deeper relationship detail available when needed. A persistent target indication, an
understandable account of other uses and explicit independence actions are useful directions, not
selected controls, icons, counts or editor composition. Ordinary edits should follow predictable
targets without constant modal questions; meaningful shared-state consequences must remain visible.
When scope is ambiguous or unavailable, resolve it before mutation rather than silently widen it.

For three uses of Pattern A, a Snare note edit affects all three. Conceptual `Make Pattern Variation`
for B gives B independent notes while A/C retain the original and sounds remain shared unless separately
changed. Conceptual `Make Sound Independent` instead gives the intended sound use independent settings;
notes and immutable samples can remain shared. Each independence action is one coherent Undo edit,
including its reference changes. Different effects on two placements require neither of these actions.
Exact reference/detachment and opaque-plugin support remain open under
[architecture](ARCHITECTURE.md#independent-sound-and-local-processing).

A `Drums` container's preferred purpose may guide convenient defaults without forbidding an otherwise
compatible Bass Pattern or claiming ownership of its synth. Genuine unsupported placement relationships
are understandable constraints; do not silently convert or duplicate content. Arrangement organization
and explicit processing assignment have different consequences under the
[bounded container model](ARCHITECTURE.md#compatible-material-and-bounded-organization).

Deleting a selected placement removes that use. Deleting a definition, container or resource is a
different scope with known dependencies. Explain affected uses and any block/defer or deliberate choice
needed to resolve them; do not silently cascade to unrelated content/media or redirect uses by name.
Predictable local deletion need not require a modal questionnaire. Undo restores the intended canonical
relationships, subject to ordinary validation; exact deletion/confirmation mechanics remain open under
[deletion scope](ARCHITECTURE.md#deletion-scope-and-retained-relationships).

### Undo grouping and interaction preview

Input source does not determine Undo semantics: a pointer drag is one logical Move transaction, a
single nudge normally one discrete edit, and a held nudge may be one bounded same-intent session.
All express canonical edit intentions; gesture updates or repeated input events are not independent
history policy. The contract below remains authoritative for interaction grouping and preview.

Undo steps follow understandable user intentions under
[ARCHITECTURE](ARCHITECTURE.md#logical-undo-transactions-and-history-scope). One continuous clip drag
is one editing interaction, even if hundreds of pointer updates occur over two seconds. During the
gesture, show the proposed position/relationship as an explicitly transient, model-bound preview of
the canonical target. The gesture itself leaves canonical state unchanged until final commit;
other independent accepted edits may still proceed. Preview is not an invisible alternative project
truth and must not leak into Save/recovery as an accepted move. Temporary audible feedback, if provided,
also remains preview under the graph/audio boundary.

Committing a changed final result validates the target and relevant preconditions and records one
coherent Undo transaction from the accepted starting state to the final placement/relationship. A
no-change result creates no Undo step. Cancellation/Escape discards the proposal and restores feedback
to current canonical state without a history entry; it must not roll back unrelated accepted edits.
If the target/context changed incompatibly during the interaction, cancel or require an explicit
reconciled intention rather than overwrite newer work. Event throttling and UI framework mechanics
remain open.

Five separate nudge commands normally produce five distinct edits; five unrelated parameter changes
remain distinct. Bounded coalescing can be justified only by an explicit, understandable same-intent
session, such as one continuous held-nudge interaction or one committed text/value editing session
on the same target/scope. A different target, unrelated command or completed interaction ends that
group. Do not merge unrelated actions merely because they happen close in time or merge later async
completion with intervening edits. Exact supported sessions/bounds remain workflow decisions; elapsed
milliseconds alone never define user intent. A multi-object command may deliberately form one Undo
transaction. Explicit async acceptance also creates one coherent edit regardless of preparation time.

Undo of shared Pattern content restores the one shared content, affecting all references consistently;
it does not imply independent placement copies. Variation, placement-local processing and independent
sound-definition edits have distinct targets under
[ARCHITECTURE](ARCHITECTURE.md#separate-sharing-identities); Q-029 retains exact sharing/detachment UI.
Document Undo concerns canonical project work. Workspace/user preferences, transient audition and
derived execution follow the [history scope](ARCHITECTURE.md#logical-undo-transactions-and-history-scope).
No keyboard shortcut, history size, storage implementation or persistence policy is selected here.

### Processing context and mix feedback

Shared versus independent performance can change audible mono/legato/retrigger and voice-stealing
behavior, even with one shared preset. Expose that meaningful consequence when an operation changes
performance interaction to provide independent processing; do not present it as a sound-neutral
implementation optimization. [ARCHITECTURE](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains)
owns the rule; final labels, defaults and controls remain open.

Pattern membership must not misleadingly imply one mixed instrument output. A multi-instrument
placement can retain different instrument paths; shared sound definitions can serve overlapping
placements with different processing. The signal model belongs to
[ARCHITECTURE](ARCHITECTURE.md#signal-ownership-and-processing-contexts), and
[NODE_GRAPH](NODE_GRAPH.md#contributions-and-irreversible-mixing) owns convergence semantics.

Users must be able to distinguish processing of an independent contribution from intentional
processing of a whole placement/container submix. The latter's result cannot offer independent
instrument routes recovered from the aggregate. Do not hide this loss behind a control that appears
to preserve separate routing. Clear contextual scope/result feedback must make it understandable
without requiring the user to open a graph or answer a routing questionnaire. Exact controls remain open.

Arrangement and Mixer may show controls for the same processing context. Their target and shared
edits must be understandable; opening the other view must not look like adding a second effects chain.
An additional independent channel/bus stage, when explicitly routed, is a distinct target. Displaying
local processing in Mixer does not turn it into unrelated global processing or expand an object render.

Moving Kick from a Compressor context into Distortion/Delay changes processing membership and may
also change its assigned route; expose those meaningful consequences. Pure organizational regrouping
at unchanged musical time retains processing/routing and must not alter sound. Musical containment,
organization and audio context must be distinguishable with progressive disclosure; final names,
indicators, gestures and route-assignment workflow remain open. The architecture owner traces the
[move](ARCHITECTURE.md#moving-material-between-contexts), including possible effects on other submix members.

### External dependency feedback

Ordinary Kick/Bass creation and basic mixing must work without manually wiring every signal or opening
a full graph. Advanced dependencies are discoverable on demand and use the same mouse-first,
keyboard-efficient semantic actions. [NODE_GRAPH](NODE_GRAPH.md#cross-context-signal-and-control-relationships)
owns their meaning; no final wiring gestures, panels, menus, icons or global graph view are selected.

When users intentionally create an external dependency, they can understand which source/occurrence
and signal boundary influences which processor/control target, whether it is audible routing, a send,
detector input or parameter control, and whether it is valid. Kick driving Bass compression must not
look like Kick being added to Bass audio. Changed tap/processing membership can change detector behavior
even if logical source identity survives; a move, delete or reassignment must reveal meaningful affected
relationships without requiring a giant graph. Visual position/selection never silently retargets them.

Missing endpoints or unsupported required inputs remain understandable persistent dependency state
under [blocker feedback](#project-availability-and-dependency-blockers). Explain the affected processing/
operation, its unavailable requirement and meaningful repair/reattach/remove direction. A Bass object
render can require external Kick influence while excluding Kick audio; show why a required dependency
blocks that result. Last-valid playback is visibly distinct from canonical work awaiting valid execution.

### Audio timeline editing

Ordinary audio timeline editing must support trimming start/end, splitting, and moving/rearranging the
resulting pieces while preserving the original durable audio resource by default. A split primarily
changes edit structure; it must not inherently modify source audio destructively. A separate explicit
crop/consolidate/render operation may later create/commit a genuinely new shorter resource.

Keep these edit intentions distinct rather than overload one gesture ambiguously:

| Intention   | Meaning                                                             |
|-------------|---------------------------------------------------------------------|
| Trim        | Change audible/visible source range without changing playback speed |
| Loop/repeat | Extend material by repeating it                                     |
| Stretch     | Change playback duration/time mapping                               |

An ordinary trim must not accidentally change playback speed. Project-wide tempo changes and local
clip stretch are also distinct; [ARCHITECTURE](ARCHITECTURE.md#project-tempo-and-audio-time) owns the
tempo-following versus fixed/source-time model. Final labels, defaults, gestures, modifiers, and tools
remain UI design work; no time-stretch algorithm is selected.

A natural source end may allow a local effect tail. Deliberately shortening/trimming the clip/item's
right boundary means its own audible result ends there, including item-local processing tails. A tiny
de-click may avoid a click without substantially extending that tail. This does not erase arbitrary
shared downstream container/bus/Mixer/Master state;
[AUDIO_ENGINE](AUDIO_ENGINE.md#source-boundaries-and-effect-tails) owns those scope distinctions and
loop/seek/playback Stop/Record Stop mechanics.

An explicitly selected export range is hard by default. A future explicit `Include effect tails`-like
option may extend it for naturally permitted tails, never restore project audio deliberately cut by
editing. Exact label/UI and finite tail-completion policy remain open under
[AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction).

## Discovery, audition and reusable content

Finding and auditioning material must make its scope understandable: temporary preview, accepted project
use and explicit Personal Library publication are distinct intentions under
[Sample workflow](SAMPLE_WORKFLOW.md#browser-discovery-and-ownership). Browser provides access to sources;
it does not turn all displayed material into project or global library content. A small discoverable
find/audition/use path, including reuse of current project samples, is sufficient initially.

Raw preview and contextual temporary audition do not enter project Undo or implicitly import/replace
accepted material. Explicit project use follows one coherent project edit where appropriate. Applying
a preset edits project-owned state; explicitly preserving a sample/preset across projects is a separate
user-content mutation. A combined action must expose its separate outcomes; project Undo normally
concerns the project effect. Exact controls, publication wording and global-history UX remain open.

An unavailable Browser source needs useful missing/unavailable state. Its provenance going offline must
not mark a healthy managed project resource or already applied preset state as broken. Missing actual
project media or required processing still follows the dependency-blocker contract below.

### Sample Lab exploration feedback

Keep ordinary exploration sound-first: generate, listen, request a related variant, revisit/compare
recent candidates and explicitly accept. Detailed parameters are optional; supported locks and deeper
controls appear progressively. These intentions need discoverable pointer paths and efficient keyboard
access through the same semantic actions, without choosing widgets, bindings or a final layout.

Make the generation family/reference, active held constraints, audible comparison choice/context and
acceptance destination distinguishable. Reference/family changes must explain retained or unavailable
locks; pending results retain their request identity rather than stealing a later selection. Explain
stale inputs and unavailable operations concisely. History is visibly temporary, not a permanent-save
claim. Acceptance into the project and independent Personal Library publication have separate outcomes.
Returning from audition restores current canonical sound, preserving intervening project edits; deleted
targets are not restored by preview. Specialized limits belong to
[Sample workflow](SAMPLE_WORKFLOW.md#intentional-and-lazy-exploration), with concrete Q-011/Q-029/Q-071
mechanisms/UI still open. Candidate navigation and lock changes do not enter document Undo/Redo.

## Project lifecycle and durable work

New project creation is a first-class document operation. Valuable unnamed/never-saved work must be
eligible for crash recovery without an existing final project path. Explicit Save is the user-confirmed
saved state; recovery maintains a separate rolling current working snapshot. After abnormal termination,
offer recovery rather than silently overwrite the last explicit saved version. Recovery existence or
acceptance alone must not replace the normal project file.
[PROJECT_FORMAT](PROJECT_FORMAT.md#recovery-state) owns recovery and integrity contracts.

Recovery feedback must identify the project or unnamed document, the captured work and its relationship
to the last explicit Save where one exists, and whether the candidate is usable, locally degraded or
unsafe/corrupt. A recovered R15 after saved R10 is recovered unsaved work; accepting it is not Save.
For unnamed recovered work, subsequent Save As chooses its first saved destination only on success.
For named recovered work, make the intended Save/Save As destination understandable before it can
replace the older saved version. Opening the older Save alone is not consent to discard newer recovery.
If an older valid candidate/Save is used as fallback, identify that fact and possible missing recent work.
No exact dialog, revision-number display, history browser or candidate-comparison UI is selected.

Save/storage failures must leave a concise persistent indication of unsaved work or unavailable/stale
recovery protection, with meaningful retry/continue/Save As direction where available. A failed Save
cannot look successful; an operation with uncertain completion must describe uncertainty until the
available coherent result can be established. Recovery success does not clear explicit-unsaved status.
Concurrent edits beyond a Save's captured revision remain unsaved. Missing material explains the actual
affected dependency and available repair/relink/replace/remove direction; do not imply that temporary
Browser source unavailability or missing generator code destroyed healthy accepted project audio.
Recording interruption identifies safely available material and gaps without promising unpersisted
samples. Keep feedback concise and primarily non-modal under
[recoverable failures](#graph-state-and-recoverable-failures); logs alone are insufficient. Exact windows,
status presentation, retry and reconciliation interactions remain open.

Ordinary media import/drag-and-drop accepts a durable project-managed resource. After successful
acceptance, moving/deleting the original arbitrary source file must not break normal project use.
Failed acceptance or Save/Save As/collect/relocate must preserve prior coherent project state under
[PROJECT_FORMAT](PROJECT_FORMAT.md#media-and-persistence-integrity). A deliberate advanced external
reference, if offered, needs separate justification and clear distinction from ordinary import.

## Project availability and dependency blockers

Distinguish document access, playback readiness, recording readiness and device/backend availability.
A safely understandable project remains accessible for available editing when suitable audio output
is absent; explain the unavailable operation and supported recovery direction as current UI state.
Input and output readiness may differ; an open editor must not imply that playback or recording works.
[PORTABILITY](PORTABILITY.md#portable-documents-and-environment-availability) owns platform claim limits;
Q-062/Q-069 retain exact device startup/loss/recovery and runtime-configuration mechanisms.

A safely understandable project normally opens degraded for local capability/resource failures:
missing/disabled/incompatible or recoverably failed ordinary plugins, missing/corrupt managed media,
failed media integrity validation/decoding and unavailable ordinary execution capabilities. Retain its
work and allow available document editing; hard project-open refusal belongs to critical document/schema/
corruption conditions preventing safe interpretation under [PROJECT_FORMAT](PROJECT_FORMAT.md#opening-and-migration).

Block operations whose dependency closure requires the broken capability, while unrelated editing
and healthy paths remain usable where semantics permit. Never silently omit required musical or
processing dependencies and report success. Explain the missing requirement concisely at the actual
affected object/node/instance/resource with restore/remove/replace direction. Broken media must remain
represented, with inspection, replacement/relink/repair or removal where meaningful; never silently
substitute unrelated audio. Exact wording/UI is unselected.

Persistent blockers must be represented as current UI state: affected-element marker, icon/badge,
restrained state color with non-color cues and concise inline/context explanation. Optional global
blocker count/navigation may help reach affected objects. A missing/corrupt media clip/item/resource
needs this persistent indication, potentially a placeholder and human-readable explanation, rather than
only a transient toast or log event. Disappearing notifications alone are
insufficient; logs remain diagnostic support. Keep the musical workspace clear rather than turning it
into a diagnostics dashboard. [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns visual treatment.

This feedback also applies when available source capabilities or resources cannot realize the requested
independent execution. Identify affected uses and the unmet requirement, preserve editable intent and
explain supported recovery/fallback direction. Do not claim a silently merged, stale or substituted
result is correct. [AUDIO_ENGINE](AUDIO_ENGINE.md#execution-state-lifetime-and-resource-integrity) and
[EXTENSIONS](EXTENSIONS.md#independent-execution-capability) own execution/fallback constraints; final
overload and plugin-host UX remain open.

Behavior-affecting migration presents a concise summary and user choice before applying forced
fallback/default substitution or other nontrivial transformation, including older-version save
compatibility consequences under the format owner. Global destructive operations check known active
dependencies; package uninstall is blocked/deferred
under [EXTENSIONS](EXTENSIONS.md#instance-removal-and-package-uninstall).

Normal audio preferences select logical input/output devices/endpoints under
[SETTINGS](SETTINGS.md#audio-device-selection), with backend/driver integration internal to the platform.

## Graph state and recoverable failures

The editable graph is canonical; last-valid derived execution may continue in the current session
during candidate preparation/validation under
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution). When editable and executing graphs
differ because the candidate is invalid or not yet published, that state must be obvious to the user.
Prefer concise graphical feedback: status/iconography, affected node/connection highlighting,
restrained color and concise contextual explanation. Transient notification may supplement persistent
state, not replace it. Valid newer revisions converge automatically without manual Apply.

Save retains canonical work even when invalid; reopening restores it with blockers and affected
execution unavailable until fixed. Export freezes/validates/prepares canonical state and blocks required
invalid/missing dependencies, never silently exporting older playing state. User-visible preparation
requires cancellation, visible state/progress and finite failure handling while UI stays responsive,
without a universal timeout under [AUDIO_ENGINE](AUDIO_ENGINE.md#bounded-asynchronous-preparation).
Async completion revalidates its project/target/context before commit under
[ARCHITECTURE](ARCHITECTURE.md#document-integrity-and-asynchronous-publication); it cannot attach a
result for a deleted object to current selection. Accepted canonical edits enter document Undo even
while execution is preparing or using the visibly identified last-valid revision. Pending, stale,
failed or cancelled work must not appear as a successful edit; explicit reuse, where available, is a
separate choice. Concrete history/commit mechanisms remain Q-063.

User-facing failures should generally be concise and recoverable. Cascading modal `MessageBox`
dialogs must not be the normal error experience. Modal decisions remain available when genuinely
required, such as choices that cannot safely be inferred or destructive actions without a better
interaction. [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns exact visual treatment.

## Live processing feedback

Future Live / Low-Latency mode must visibly communicate when the live processing path differs from
full intended processing. Switching it must not silently rewrite the project/graph; returning to normal
mode restores full processing. Final/offline render uses the intended full path rather than inheriting
temporary live bypass state. [AUDIO_ENGINE](AUDIO_ENGINE.md#live--low-latency-direction) owns direction
and unresolved latency/reporting/compensation/bypass policy, not a selected UI control.

## Application preferences and reset

The future Settings experience supports bounded application/plugin-global preference resets under
[SETTINGS](SETTINGS.md#reset-boundary). Configuration reset must not delete user projects,
project-managed audio/media, user-created samples/presets or Personal Library content. Exact button
layout and confirmation flow remain open.
Production diagnostics ordinarily need at most a simple enable/disable preference where useful;
developer logging levels belong outside ordinary GUI settings under [SETTINGS](SETTINGS.md#production-diagnostics).

### Language and theme preference changes

R3-F1 applies its two language/theme actions immediately after button activation. Existing controls,
keyboard focus and document remain alive; no deferred restart is required in this bounded shell.
RU entries fall back individually to usable English; if an essential host entry is also unusable,
short independent English action/name labels remain available rather than exposing keys. Other missing
text gets a meaningful localized unavailable explanation. Blank, control-containing or overlong entries
are unusable. Dotted keys and first-party catalogs are host-owned, not an extension resource SDK.
Unnamed display is localized only while canonical name is null; user names and canonical numeric values
never translate. This shell has no editable localized numeric input or active musical gesture.

Changing host language/theme with a project open is a presentation preference change, not a musical
edit. Preserve project content/relationships, ongoing editing intent, focus/selection and pending
operations without committing, discarding or reinterpreting user input merely to refresh presentation.
UI behavior must be predictable: make clear when the new preference takes effect and any necessary
deferral. Applying safely at a defined boundary is permitted; instantaneous hot switching, rebuilding
every window or mandatory restart is not an accepted policy. Concrete behavior requires later UI evidence.

Missing translation/style resources use understandable host fallback without hiding working unrelated
capabilities. Different contributor language support may produce a local fallback without changing other
host surfaces. Independent native editors may retain their own language/theme; host-owned surrounding
UI follows the host preference. [Architecture](ARCHITECTURE.md#host-localization-and-ui-resources) owns
identity, per-resource fallback and locale/data separation; [UI design](UI_DESIGN.md#themes-and-semantic-resources)
owns semantic styles and non-color feedback. [Settings](SETTINGS.md#user-configuration-and-project-state)
retains preference ownership; exact storage/reset mechanics remain Q-053.

## In-window workspace and graph depth

R3-F2 implements the first two optional host panes with explicit open/activate, pointer movement/size,
collapse versus reversible Close (internally Hidden), bounded left/right docking and retained content. Pane activation
is not musical
selection or editing authority. The strip and Panes menu provide discoverable restoration/reopening;
F6/Ctrl+Tab, local pane actions, Escape and Ctrl+W supply complementary keyboard paths without global
hooks. Restore selects a valid live pane context, and hiding the last surface returns to the host action.
Language/theme refresh retain existing controls, working context and layout. Exact bindings and scope
are owned once in [Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes); broader editor/native-editor
routing and accessibility remain open, not established by these two non-musical surfaces.

Major surfaces normally use internal workspace panes in one main window. Interaction activates a pane
and brings it ahead of overlapping panes, including when only part is visible. Moving/resizing, internal
floating, separation from docked groups, and collapse to an unobtrusive shelf/strip should preserve
working state on restoration. [WORKSPACE](WORKSPACE.md) owns the detailed pane contract and layout state.

This direction governs normal major Seqvium-authored surfaces. Third-party/native plugin editors may
use top-level OS windows when internal embedding is unsafe or impractical; their visual design need
not match Seqvium. [WORKSPACE](WORKSPACE.md) owns this exception and [EXTENSIONS](EXTENSIONS.md) owns
the host/editor lifecycle boundary.

Docking/magnetism is a per-pane user choice, predictable and easy to escape; automatic layout must not
fight free placement. Opening a project should normally retain the user's workspace preference.

When requested, the graph opens as a free-form spatial canvas with compact nodes and deeper settings
on demand. Coordinates do not define processing order; connections express dependencies. Ordinary
creation must not require learning graph internals; advanced branches/merges remain discoverable.
[NODE_GRAPH](NODE_GRAPH.md) owns graph semantics, ports, and the editing/execution boundary.
Compact-chain editing and node-settings presentation remain proposals/open UX questions.

## Essential accessibility feedback

Keyboard focus, selected targets and active context must be understandable. Important invalid state,
unavailable commands and pending operations need sufficient non-color/structural feedback where
applicable; color or transient animation alone is insufficient. Use persistent state and concise
contextual explanation appropriate to the action, without a second visual system or diagnostic-heavy
workspace. [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns visual treatment; specific blocker and
preparation workflows above retain their specialized behavior.

Essential window/workspace actions must remain keyboard-accessible despite custom chrome under
[WORKSPACE](WORKSPACE.md#keyboard-access-and-focus-return). These are baseline future requirements,
not accessibility certification or implemented support. Screen-reader/accessibility-tree integration,
platform APIs, detailed navigation, DPI/minimum-size evidence and cross-platform acceptance remain
open under Q-064 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).

## Interaction quality

Prioritize strong visual hierarchy, restrained intentional identity, readable typography, clear
selection/loading/error states, excellent drag-and-drop, useful context menus, sensible shortcuts,
and good note-editing interaction. Scaling and DPI correctness and responsive input belong in
verification when UI exists. Avoid an engineer-only interface.

First-party UI uses responsive/reflowing layout rather than uniformly scaling the application into
unreadability; [UI_DESIGN](UI_DESIGN.md#responsive-layout-and-usable-minimums) owns usable minimums.
The main window uses custom unobtrusive chrome without default TopMost or aggressive foreground/focus
activation under [WORKSPACE](WORKSPACE.md#main-window-chrome-and-foreground-behavior). These host
principles do not prescribe independently rendered third-party editor visuals.

Pleasant direct manipulation, spatial clarity, and restrained feedback should make experimentation
inviting. [UI_DESIGN](UI_DESIGN.md) owns gradual visual guidance and consistency between panes.

The final visual language, bindings, window layout, onboarding, and detailed Piano Roll gestures
remain open. This contract does not prescribe a final visual system.
