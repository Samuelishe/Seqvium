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

Repeated placements normally share a pattern; an explicit independent-variation operation should
make the change in sharing understandable. Arrangement tracks and mixer channels have different
purposes and must not force a misleading one-to-one conceptual model.

Sample-specific solo/in-pattern audition, acceptance, candidate history, and source-preserving
resampling are owned by [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md). Missing capabilities should be
represented explicitly according to [EXTENSIONS](EXTENSIONS.md), with user work retained.

## In-window workspace and graph depth

Major surfaces normally use internal workspace panes in one main window. Interaction activates a pane
and brings it ahead of overlapping panes, including when only part is visible. Moving/resizing, internal
floating, separation from docked groups, and collapse to an unobtrusive shelf/strip should preserve
working state on restoration. [WORKSPACE](WORKSPACE.md) owns the detailed pane contract and layout state.

Docking/magnetism is a per-pane user choice, predictable and easy to escape; automatic layout must not
fight free placement. Opening a project should normally retain the user's workspace preference.

The core graph should expose a compact node surface with deeper settings on demand. Ordinary creation
must not require learning graph internals; advanced branches/merges remain discoverable. [NODE_GRAPH](NODE_GRAPH.md)
owns graph semantics, ports, and the editing/execution boundary. No final node visuals are selected.

## Interaction quality

Prioritize strong visual hierarchy, restrained intentional identity, readable typography, clear
selection/loading/error states, excellent drag-and-drop, useful context menus, sensible shortcuts,
and good note-editing interaction. Scaling and DPI correctness and responsive input belong in
verification when UI exists. Avoid an engineer-only interface.

The final visual language, bindings, window layout, onboarding, and detailed Piano Roll gestures
remain open. This contract does not prescribe a final visual system.
