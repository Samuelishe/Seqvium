# Settings and diagnostics

Role: Application/user configuration, bounded reset, and production diagnostic policy.
Read when: Designing preferences, configuration storage/reset, or product logging controls/storage.
Authoritative for: User/project configuration separation, preference-reset limits, quiet bounded production diagnostics.
Not authoritative for: Project serialization, workspace behavior, exact paths/formats, localization/theme APIs, or logging implementation.

These are accepted future requirements. No settings UI, configuration files, or logging implementation exists.

## User configuration and project state

Per-user/application configuration belongs in the platform-appropriate user configuration area:
`AppData` conceptually on Windows, with platform equivalents elsewhere. This includes application
preferences, selected language, theme, workspace preferences/layout, device/user preferences, and
plugin-global preferences where provided. Device selection is an environment preference; any associated
setting affecting project sound, timing, musical meaning, or reproducible behavior is project-owned.
Exact filesystem paths and serialization formats remain open.

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
Default tempo is a possible example, not a selected default setting or value.

Plugin-global preferences may include editor size, UI preferences, and default presets/preferences for
new instances where genuinely global. Instance state affecting saved music is project state. Global
plugin defaults apply to newly created instances and must not silently rewrite existing project
instances. Exact default inventories and storage formats remain open.

## Reset boundary

The future Settings experience must permit bounded operations conceptually equivalent to resetting:

- Seqvium/application preferences;
- a specific plugin's global preferences;
- all application/plugin-global preferences.

Configuration reset must not delete user projects or project-managed audio/media. Project-affecting
plugin instance state remains project state, separate from these preference resets. Exact command names,
button layout, scope controls, and confirmation flow remain open under [UX_CONTRACT](UX_CONTRACT.md).

## Production diagnostics

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
