# Settings and diagnostics

Role: Application/user configuration, bounded reset, and production diagnostic policy.
Read when: Designing preferences, configuration storage/reset, or product logging controls/storage.
Authoritative for: User/project configuration separation, implemented host configuration paths/formats and failure
safety, logical device-selection UX, preference-reset limits, quiet bounded production diagnostics.
Not authoritative for: Project serialization, workspace interaction/geometry, localization/theme APIs, or
logging implementation.

R2-F4 implements session-only logical audio selection. The unified Settings center owns global
preferences and application shortcuts; the independent preference/layout files below remain compatible.
Broad application/plugin reset, persistent audio selection and production logging controls remain future work.

## Unified global settings center

Implemented SEQ-SETTINGS-F1: the toolbar Settings action (default Ctrl+,) opens one owned compact
window. General contains explicit language/theme choices, Controls contains behavior and application
keyboard shortcuts, and About retains the brand composition, scope and assembly-derived version.
Changes apply immediately; Close/Escape keep applied values. Opening cancels active workspace gestures;
closing returns focus to the originating live control. Settings contains no musical document or Undo.
The Appearance pane and toolbar language/About actions are retired. Its version-1 layout identity is
retained only as a hidden compatibility token; it never creates preference controls or a pane tab.

New global preferences must enter this center under their real subsystem owner. Extend a useful page
or add a page with implemented content; do not add placeholder VST/audio I/O/MIDI categories. Those
subsystems will add their meaningful global preferences here when available. Keep environment/device
intent separate from reproducible project sound/timing/instance state. Browser's contextual Auto preview
checkbox is a quick access to the same persistent Controls preference, not a second setting.

The title-row sun/moon action opens an explicit theme menu with the current item marked. It never
cycles or alternates themes. A compact disclosure chevron and localized Choose theme label identify
the button as a selector; the tooltip also names the current theme. General and the quick menu
enumerate the same ThemeCatalog. Additional
built-in or user themes must extend this selection model with stable persisted identities and coherent
availability fallback, rather than creating a binary toggle or a cycle. Current implementation ships
only Dark/Light; user-theme loading/packaging remains unimplemented under UI design/Q-055.

### Remappable application commands

HostCommands owns stable nonlocalized IDs, labels, groups and defaults. Controls offers search by
command/chord, key capture, explicit Assign, clear, per-command default and keyboard-only Reset shortcuts.
Only effective differences from defaults have a small dot/bold chord and an individual reset icon
with a default-chord tooltip. Default rows have no reset button; column alignment remains stable.
Explicitly assigning the default does not mark it customized; clearing a default binding does.
A conflict names the current owner; explicit Reassign clears the old owner and applies the new binding
atomically in session. Restoring one default uses the same conflict decision. Reset shortcuts restores
all command defaults without changing behavior, language, theme, layout, Browser locations or extensions.

The real MainWindow dispatcher and toolbar shortcut/automation hints consume the current bindings.
Bindings are single chords: Ctrl (optionally Shift) plus supported letters/comma, Ctrl+Escape, or
unmodified/Shift function keys except F4/F6/F10. Alt/Meta/AltGr, text clipboard/selection, editor duplicate,
search, Browser refresh, pane close/navigation, arrows and plain typing/Space remain reserved.
Ctrl+Z/Y and document Undo/Redo never steal text history, including after remapping; transport function
keys remain available while editing text. No global OS keyboard hook is installed.

This completed first stage covers New/Open/Save/Save As/Import WAV, Undo/Redo, Play/Stop/Panic,
Browser, Graph and Settings. Editor-local Piano/Playlist/Rack/Graph gestures and pane traversal/close
remain fixed and are disclosed under Editor & system keys. A future scoped stage must model overlapping
editor contexts, command availability and text/focus arbitration before allowing their remapping;
it must not merely add editable labels. Multiple alternatives/chord sequences, mouse gestures,
controllers, MIDI mapping, imported profiles and macro commands are not implemented.

### Control preference storage

`controls.json` sits beside existing preferences/layout in the platform user configuration directory.
Version 1 stores `autoPreview` (default false) and `shortcuts`, an optional map of stable command IDs
to canonical chord strings or null (unassigned). Omitted commands use current defaults. Existing
`preferences.json` language/theme bytes/schema and workspace schema remain independently owned;
there is no project-format change or migration of old preference files.

Reads run on workers with a 32 KiB + sentinel byte bound and depth 8. Missing files use defaults.
Malformed/oversize/unreadable/future versions, unknown command IDs, reserved/invalid chords and conflicting
effective bindings fall back safely, preserve original bytes and block writes for the store lifetime.
Session changes remain functional with a visible notice. Writes are serialized, flush a unique sibling
temporary, preserve `.previous`, replace atomically and remove only their own temporary. Shutdown joins
pending writes. Reset cannot bypass a damaged-file write refusal. Power-loss durability, competing
external writers, hostile links and preference repair/profile migration remain unestablished.


## Extension enablement

`extensions.json` lives beside preferences/layout and Browser locations. Version 1 stores only a
bounded list of disabled package IDs (at most 256, 32 KiB), including temporarily absent packages.
Unlisted compatible deployment packages default enabled when Browser initializes the local catalog.
The deployment root and trust rules are owned by [Extensions](EXTENSIONS.md#implemented-r6-local-foundation),
not user preferences. No plugin installer, reset or deletion action is introduced.

Worker reads/writes preserve invalid/unsupported originals and refuse overwriting them for this
session. Unsafe settings do not automatically enable modules; explicit session choices remain usable
with a visible persistence notice. Valid writes flush an exclusive sibling temporary, preserve
`.previous` and replace the file. Browser shutdown joins pending changes. These settings contain no
audio, project dependencies, sound parameters or opaque plugin state; disabling/resetting preferences
cannot delete accepted media.

## Browser access preferences

`browser-locations.json` lives alongside the existing user preference/layout files, including isolated
VisualCheck configuration. Version 1 stores at most 16 pinned roots, 16 recent folders and one last
folder (or Project resources), with 32 KiB and 2048-character path bounds. Paths are remembered access,
not retained audio, project dependencies or Personal Library ownership. Forget location removes only
that preference; it never deletes a source folder or project media.

Reads and serialized writes run on workers; malformed/unreadable/unsupported/oversize originals are
preserved and block writes for this session. A visible Browser notice leaves navigation usable.
Valid changes flush a unique same-directory temporary, preserve `.previous` and replace atomically.
Shutdown joins pending writes. This adds no Reset command, indexing database or preference-schema
change. Language/theme and music/history remain independently owned.

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
translate user project names or invalidate pending imports. The Settings center above owns their UI.
A future reset must be restricted to independently owned configuration; it cannot delete project files,
accepted WAV, user-created content or recovery. The `.previous` file is preferences only, not music recovery.

## Implemented R3-F2 workspace layout storage

`WorkspaceLayoutStore` owns `workspace-layout.json` alongside `preferences.json` in the same
platform user configuration directory. It does not change the preference schema, project JSON, Undo,
Save, device intent, media roots or recovery. Missing layout starts with the registered panes hidden;
the host then opens the appropriate empty or musical workspace. Older layouts initialize each missing pane hidden.
Version 1 stores usable workspace `width`/`height`, `activePaneId`, and a back-to-front `panes` array.
Optional `window` contains normal `x`/`y` in screen pixels, `width`/`height` in DIPs and `maximized`.
Omission is valid for older layouts; malformed supplied values use the same preserve-original fallback.
Optional `navigation` and `browserPanel` store each sidebar's preferred usable `width` in DIPs,
`collapsed` boolean and optional `visibleWidth` for a released, partly clipped viewport. Browser's
workspace pane entry remains authoritative for visibility; its optional sidebar placement owns width.
Old version-1 files omit these additions without fallback; old Browser geometry seeds its initial width.
Malformed supplied values preserve the original file under the same write-refusal policy. Transient
drag widths and automatic window-pressure fitting never replace these preferences.
[Workspace](WORKSPACE.md#shared-left-panel-zone) owns resize, clipping, collapse and reopening.
Maximized/minimized geometry never replaces the normal restore rectangle. Main-window movement,
settled resize, pane commits and joined shutdown persist this independent user configuration.
Each entry contains `instanceId`, `typeId`, `visibility` (`hidden`/`visible`/`collapsed`), floating
`x`/`y`/`width`/`height`, `dock` (`floating`/`left`/`right`) and `allowDocking`. The optional version-1
`dockedWidth` is independent of the retained floating rectangle and stores actual permitted dock width.
Older version-1 files omit it: state initializes dock width from floating width within the current
half-workspace limit, without a read-fallback warning or schema migration. Malformed/nonfinite supplied
fields retain the existing fallback/write-refusal policy; finite geometry is limited by state. IDs are stable English
tokens, not localized titles. Controls, focus references, project targets and runtime graphs are absent.
[Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes) owns placement/interaction invariants.

Reads cap actual bytes at 32 KiB plus one sentinel, JSON depth at 16, and input entries at 16; only
the registered independent instances can enter state. Unknown/duplicate entries are skipped, malformed
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

Ordinary Desktop launches now obtain user-wide [process ownership](ARCHITECTURE.md#desktop-process-ownership)
before reading/writing preferences/layout. A secondary forwards activation and exits, including bounded
safe refusal on IPC failure. This reduces accidental competing Desktop writers; it does not establish
project-file exclusion against external tools, recovery or crash/power-loss storage guarantees.
VisualCheck's internal composition gives preferences/layout and unnamed media an isolated directory;
no production environment/command-line bypass or setting is added.

The implemented file policies above cover language/theme, registered first-party pane placements,
main-window restoration and Pattern navigation width/collapse.
Application keyboard bindings and Browser auto preview use the separate control file above; other
application/plugin settings remain future work.

Node Editor adds the fixed `node-editor.main` / `node-editor` entry under the same version-1 layout.
Older layouts omit it and start it hidden. Only pane placement/visibility/docking is persisted; target,
draft and document lifecycle remain session-local. Older binaries treat the added entry as unknown
and use the existing preserve-original fallback, rather than losslessly saving a downgraded layout.

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
discovery beyond R6's bounded deployment root remain open under [EXTENSIONS](EXTENSIONS.md).

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
