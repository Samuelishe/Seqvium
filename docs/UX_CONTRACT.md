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

Pleasant direct manipulation, spatial clarity, and restrained feedback should make experimentation
inviting. [UI_DESIGN](UI_DESIGN.md) owns gradual visual guidance and consistency between panes.

The final visual language, bindings, window layout, onboarding, and detailed Piano Roll gestures
remain open. This contract does not prescribe a final visual system.
