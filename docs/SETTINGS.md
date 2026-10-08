# Settings and diagnostics

Role: Application/user configuration, bounded reset, and production diagnostic policy.
Read when: Designing preferences, configuration storage/reset, or product logging controls/storage.
Authoritative for: User/project configuration separation, implemented host configuration paths/formats and failure
safety, logical device-selection UX, preference-reset limits, quiet bounded production diagnostics.
Not authoritative for: Project serialization, workspace interaction/geometry, localization/theme APIs, or
logging implementation.

R2-F4 implements session-only logical audio selection. R3-F1/F2 implement the bounded preferences and
workspace layout files below plus the optional Appearance pane; broader Settings/reset/logging UI is absent.

## Implemented R3-F1 host preferences

The shell persists only language (`en`/`ru`) and theme (`dark`/`light`) in version-1 JSON, default EN/Dark:
Windows `%LOCALAPPDATA%/Seqvium/preferences.json`; macOS `~/Library/Application Support/Seqvium/preferences.json`;
Linux absolute `$XDG_CONFIG_HOME/Seqvium/preferences.json`, otherwise `~/.config/Seqvium/preferences.json`.
This file owns no pane layout, project settings/music, media, device intent or recovery snapshot.

Reads are bounded to 4096 bytes, tolerate missing/malformed/unreadable/unsupported settings, fall back
per field when possible and never clean/delete the original. Corruption/access failure produces a localized
notice; missing settings simply use defaults. Explicit changes serialize writes through one owner gate,
write/flush a same-directory unique temporary file, preserve previous bytes in `preferences.json.previous`
before replacement and remove only the writer's temporary. Oversize originals or failed backup/replacement
refuse persistence; the applied in-session preference remains usable with a visible warning.
The main window awaits pending preference persistence on close. Power-loss durability, multiple writer
processes, hostile links and a multi-version backup history are not established guarantees.

Language/theme apply immediately, preserve controls/focus and do not edit canonical revision/history,
translate user project names or invalidate pending imports. No Reset command or full Settings UI exists.
A future reset must be restricted to independently owned configuration; it cannot delete project files,
accepted WAV, user-created content or recovery. The `.previous` file is preferences only, not music recovery.

## Implemented R3-F2 workspace layout storage

`WorkspaceLayoutStore` owns `workspace-layout.json` alongside `preferences.json` in the same
platform user configuration directory. It does not change the preference schema, project JSON, Undo,
Save, device intent, media roots or recovery. Missing layout means two optional hidden panes.
Version 1 stores usable workspace `width`/`height`, `activePaneId`, and a back-to-front `panes` array.
Each entry contains `instanceId`, `typeId`, `visibility` (`hidden`/`visible`/`collapsed`), floating
`x`/`y`/`width`/`height`, `dock` (`floating`/`left`/`right`) and `allowDocking`. The optional version-1
`dockedWidth` is independent of the retained floating rectangle and stores actual permitted dock width.
Older version-1 files omit it: state initializes dock width from floating width within the current
half-workspace limit, without a read-fallback warning or schema migration. Malformed/nonfinite supplied
fields retain the existing fallback/write-refusal policy; finite geometry is limited by state. IDs are stable English
tokens, not localized titles. Controls, focus references, project targets and runtime graphs are absent.
[Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes) owns placement/interaction invariants.

Reads cap actual bytes at 32 KiB plus one sentinel, JSON depth at 16, and input entries at 16; only
the two known independent instances can enter state. Unknown/duplicate entries are skipped, malformed
fields fall back independently, invalid geometry is adapted/clamped and dock conflicts remain reachable.
Unsupported/stale versions, malformed/oversize/unreadable data leave the app usable. No read repairs
the original file. If any read used fallback, this store refuses every write for the session and shows
a localized persistent notice, preserving original bytes at their original path even during shutdown.
Valid independent entries still restore. Repair/migration UI and multi-version schema migration are absent.

Completed pane actions, pointer release and resize settling (250 ms) request immutable snapshots.
`WorkspaceLayoutPersistence` serializes writes and coalesces only the latest pending snapshot, never
allocating a queue per pointer move. Disk work runs off the UI thread. The writer flushes a unique
same-directory temporary, copies prior bounded bytes to `.previous`, replaces the layout, then cleans
only its own temporary. Backup/replacement failure retains the last file and shows a warning without
disabling this session's panels. Shutdown first cancels gestures/stops the resize timer and new requests,
then joins the final layout and preference writes before disposing pane controls/document. This is
normal-process failure evidence, not crash/power-loss durability, hostile-link or multi-process guarantees.
Neither layout failure nor future configuration reset may delete musical work/media/recovery.

## User configuration and project state

The implemented file policies above cover only language/theme and the two first-party pane placements.
Other application/plugin settings remain future work.

Per-user/application configuration belongs in the platform-appropriate user configuration area:
`AppData` conceptually on Windows, with platform equivalents elsewhere. This includes application
preferences, selected language, theme, workspace preferences/layout, device/user preferences, and
plugin-global preferences where provided. Device selection is an environment preference; any associated
setting affecting project sound, timing, musical meaning, or reproducible behavior is project-owned.
Paths/formats beyond the implemented host files remain open.

Any setting that can change one project's sound, timing, musical meaning, or reproducible behavior
belongs to that project. This includes tempo, time signature, instrument/plugin instance state,
routing, processing state, stretch/time behavior, project-specific audio settings, and later automation
or track-specific behavior where applicable. These values are not live references to application
preferences: changing global defaults must not silently alter previously saved projects.
[PROJECT_FORMAT](PROJECT_FORMAT.md#project-state-and-user-preferences) owns their
persistence; [WORKSPACE](WORKSPACE.md#layout-ownership-and-restoration) owns workspace behavior.
Bundled/plugin executable files and preferences are separate concerns. Exact plugin installation/
discovery locations remain open under [EXTENSIONS](EXTENSIONS.md).

Host localization ownership/Russian-English direction belongs to
[ARCHITECTURE](ARCHITECTURE.md#host-localization-and-ui-resources); Dark/Light semantic theme resources
belong to [UI_DESIGN](UI_DESIGN.md#themes-and-semantic-resources). Settings does not select their formats.

### Defaults for new projects and instances

User/application configuration may provide defaults for new projects. At creation, a project receives
its own values and owns them independently; later changes to defaults do not mutate existing projects.
User-configured default inventories remain open; R1's constructor fallback is selected below.

R1's `ProjectDocument.Create` accepts immutable `ProjectSettings` defaults and uses 120 quarter-note
BPM / 4/4 when none are supplied. Settings belong to canonical state, edit history and JSON Save;
there is no preferences store, reset UI or live global link. This constructor fallback is a bounded
foundation default, not a final new-project template policy.

Plugin-global preferences may include editor size, UI preferences, and default presets/preferences for
new instances where genuinely global. Instance state affecting saved music is project state. Global
plugin defaults apply to newly created instances and must not silently rewrite existing project
instances. Exact default inventories and storage formats remain open.

## Audio device selection

Normal Settings exposes meaningful logical endpoints: `Audio Output: Steinberg UR12`, and separately
`Audio Input: Line (Steinberg UR12)` or `Audio Input: USB Microphone`. Input/output selection is
logically separate where platform/device architecture supports it. An analog/condenser microphone
through an interface is represented through its OS capture endpoint; future channel choice is distinct
from logical endpoint selection. Do not fabricate per-channel endpoints from device-level discovery.
A USB microphone may appear as its own OS input device.

Ordinary users choose devices, not backend libraries or engine implementations. Backend/API/driver
integration is internal platform responsibility under
[AUDIO_ENGINE](AUDIO_ENGINE.md#device-inputoutput-and-recording-direction). Do not add ordinary
`Choose backend: libfoo / libbar / WASAPI implementation X` merely because adapters exist. A future
advanced troubleshooting override requires a separately justified exceptional feature.

Exact backend/API/ASIO constraints, clock domains, rate/channel/buffer adaptation and recovery remain
open (Q-062/Q-069 and related audio questions). Selecting an endpoint does not settle project audio
intent versus negotiated runtime facts; device changes while recording follow subsystem lifetime and
known-active-dependency safety.

F3 preview's initial gain 0.05 is session-only, not persisted project/preference state or system volume.
Discovery locations are supplied explicitly; no recent-location store exists. Detailed WASAPI capture/
observations are opt-in developer diagnostics, disabled by default for ordinary audition under
[Audio](AUDIO_ENGINE.md#r2-f3-transient-one-shot-execution). No logging/settings UI is introduced.
R2-F4 implements separate input/output session intent: follow an OS default role or retain an explicit
opaque endpoint ID. Names are never identity. Discovery/resolution and active output facts remain
separate from intent; missing/inactive explicit IDs never silently fall back. Default following resolves
at deliberate open, without automatic migration of an active stream. Input selection opens no stream
and does not stop output. Output selection joins/releases its old session before a replacement can open.
Availability refresh or device failure changes no project settings/music/history. Session selection is
not yet stored as a global preference; Windows IDs never enter portable project JSON. Exact failures
and lifetimes belong to [Audio](AUDIO_ENGINE.md#r2-f4-logical-endpoints-and-independent-selection).

## Reset boundary

The future Settings experience must permit bounded operations conceptually equivalent to resetting:

- Seqvium/application preferences;
- a specific plugin's global preferences;
- all application/plugin-global preferences.

Configuration reset must not delete user projects or project-managed audio/media. Personal Library
samples, user-created presets and other intentionally retained reusable content are user work, not
application/plugin-global preferences or disposable caches. Reset must not erase them even if their
future storage shares a platform user-data area. Discovery-location preferences may be configuration;
resetting those preferences must not delete the content they point to. Exact library rediscovery,
storage/backup policy and reset UI remain open.

Project-affecting plugin instance state remains project state, separate from preference resets and
reusable preset sources under [Extensions](EXTENSIONS.md#preset-sources-and-project-state).
[Sample workflow](SAMPLE_WORKFLOW.md#personal-library-and-explicit-publication) owns intentional reusable
content retention. Exact command names, button layout, scope controls, and confirmation flow remain
open under [UX_CONTRACT](UX_CONTRACT.md).

Recoverable working documents, including unnamed/unsaved projects, are user work rather than disposable
preferences or diagnostic logs. Preference reset must not discard their recovery state. Recovery is
separate from explicit Save; cadence/retention controls and storage paths remain unselected under
[PROJECT_FORMAT](PROJECT_FORMAT.md#recovery-state). Bounded production-log cleanup does not define
recovery retention or authorize deleting the only known recent unsaved recovery copy.

## Production diagnostics

Persistent capability/execution blockers are primarily visible current UI state under
[UX_CONTRACT](UX_CONTRACT.md#project-availability-and-dependency-blockers); logs are diagnostic support,
not the primary user communication.

Normal user-facing production logging is deliberately quiet: meaningful operational problems,
primarily major warnings, errors, and critical failures. Permanent INFO/DEBUG/TRACE streams must not
define the normal public product. Where useful, ordinary graphical settings expose at most a simple
diagnostics/logging enable/disable preference, not a professional logging-level configuration panel.

Production log storage must bound file size, rotation/count, and retention/lifetime. Normal use must
not create unbounded multi-gigabyte logs. Exact sizes, counts, durations, storage format, and paths
remain open; this policy does not select a logging dependency or implementation.

Detailed developer Debug/Trace logging may exist, activated only through developer-oriented
configuration, command-line/environment/config mechanisms outside ordinary GUI settings.
[DEVELOPMENT](DEVELOPMENT.md#developer-diagnostics) owns that activation boundary.
Logging must respect [AUDIO_ENGINE](AUDIO_ENGINE.md#accepted-realtime-constraints), not perform blocking
storage work on the realtime path. Concise user-facing failure presentation belongs to
[UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures) and [UI_DESIGN](UI_DESIGN.md).

Exact settings/reset and production-log mechanisms remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
