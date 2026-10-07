# UX contract

Role: Product contract for observable interaction.
Read when: Designing editing, audition, workspace behavior, or user-facing actions.
Authoritative for: Progressive complexity, feedback, discoverability, general workflow semantics.
Not authoritative for: Pixel-perfect design, DSP behavior, serialization, or current implementation.

These are intended requirements. No application UI exists yet.

## Mouse-first creation and complementary input

Users must be able to place, move, resize, and select notes/events; edit steps; move/resize clips;
select waveform ranges for trimming/processing; manipulate node graphs; adjust parameters; and
organize the workspace directly with the pointer. Exact gestures and bindings remain open.

An on-screen musical keyboard should support audition/simple playing. Future MIDI devices, MIDI
recording, and realtime note input complement pointer editing and converge on compatible musical
event data. Audio-interface/microphone recording is also long-term direction, with execution and
latency owned by [AUDIO_ENGINE](AUDIO_ENGINE.md).

The ordinary computer keyboard remains primarily for application shortcuts/commands. A typing
keyboard / QWERTY piano must not define default semantics or occupy large parts of the shortcut
space. If considered later, it must be optional and explicitly activated.

## Progressive complexity and discoverability

Expose controls relevant to the current musical task. Advanced capability may be available without
permanently occupying the primary workspace. A simple default must allow deeper editing rather than
cap the user's work. Useful hidden capability still needs a discoverable path through contextual
controls, menus, shortcuts, or a deliberately opened workspace.

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

Moving material into a processing context subjects it to that context's meaningful behavior: a Kick
moved from a Compressor container into a Distortion/Delay container changes sound. Pure organizational
grouping must not silently change sound. The UI must distinguish processing contexts from organizational
groups while keeping musical containment and mixer routing distinct; final terminology remains open.

Sample Lab supports standalone and contextual exploration. Contextual audition is temporary and
reversible until explicit acceptance and should preserve relevant existing downstream processing.
Object sample creation includes the object's own semantic/local processing boundary; audible-selection
capture serves a broader selected musical/time context. The operations are distinct and their names
are conceptual. Replacing an object with audio containing baked processing must be undoable and must
not silently reapply that exact baked chain. Specialized audition, acceptance, candidate history,
and resampling semantics are owned by [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md). Missing capabilities should be
represented explicitly according to [EXTENSIONS](EXTENSIONS.md), with user work retained.

### Audio timeline editing

Ordinary audio timeline editing must support trimming start/end, splitting, and moving/rearranging the
resulting pieces while preserving the original durable audio resource by default. A split primarily
changes edit structure; it must not inherently modify source audio destructively. A separate explicit
crop/consolidate/render operation may later create/commit a genuinely new shorter resource.

Keep these edit intentions distinct rather than overload one gesture ambiguously:

| Intention | Meaning |
| --- | --- |
| Trim | Change audible/visible source range without changing playback speed |
| Loop/repeat | Extend material by repeating it |
| Stretch | Change playback duration/time mapping |

An ordinary trim must not accidentally change playback speed. Project-wide tempo changes and local
clip stretch are also distinct; [ARCHITECTURE](ARCHITECTURE.md#project-tempo-and-audio-time) owns the
tempo-following versus fixed/source-time model. Final labels, defaults, gestures, modifiers, and tools
remain UI design work; no time-stretch algorithm is selected.

A source/input end, a continuing effect tail, and an explicitly requested hard cut are different
intentions. The user must be able to request the hard boundary as well as ordinary source edits;
[AUDIO_ENGINE](AUDIO_ENGINE.md#source-boundaries-and-effect-tails) owns processing semantics, including
loop/seek/playback Stop/Record Stop and faithful export rules. Ending source input must not implicitly
mean every running effect is destroyed. The manual export-range tail policy remains explicitly open.

## Project availability and dependency blockers

A safely understandable project normally opens degraded when an ordinary required plugin is missing,
disabled, incompatible or has a recoverable activation/materialization failure. Retain its work and
allow available document editing; hard project-open refusal belongs to critical document/schema/
corruption conditions under [PROJECT_FORMAT](PROJECT_FORMAT.md#opening-and-migration).

Block operations whose dependency closure requires the broken capability, while unrelated editing
and healthy paths remain usable where semantics permit. Never silently omit required musical or
processing dependencies and report success. Explain the missing requirement concisely at the actual
affected object/node/instance with restore/remove/replace direction; exact wording/UI is unselected.

Persistent blockers must be represented as current UI state: affected-element marker, icon/badge,
restrained state color with non-color cues and concise inline/context explanation. Optional global
blocker count/navigation may help reach affected objects. Disappearing notifications alone are
insufficient; logs remain diagnostic support. Keep the musical workspace clear rather than turning it
into a diagnostics dashboard. [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns visual treatment.

Behavior-affecting migration presents a concise summary and user choice before applying forced
fallback/default substitution or other nontrivial transformation, including older-version save
compatibility consequences under the format owner. Global destructive operations check known active
dependencies; package uninstall is blocked/deferred under [EXTENSIONS](EXTENSIONS.md#instance-removal-and-package-uninstall).

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
result for a deleted object to current selection. Exact undo/commit boundaries remain open.

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
[SETTINGS](SETTINGS.md#reset-boundary). Configuration reset must not delete user projects or
project-managed audio/media. Exact button layout and confirmation flow remain open.
Production diagnostics ordinarily need at most a simple enable/disable preference where useful;
developer logging levels belong outside ordinary GUI settings under [SETTINGS](SETTINGS.md#production-diagnostics).

## In-window workspace and graph depth

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
