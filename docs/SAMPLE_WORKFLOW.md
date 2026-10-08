# Sample workflow

Role: Seqvium's sound exploration and material-transformation contract.
Read when: Designing discovery, generation, audition, sample acceptance, or resampling.
Authoritative for: Browser discovery/audition, project-resource reuse, Personal Library publication/ownership, Sample Lab context, candidate acceptance, durable audio, object sampling and audible capture.
Not authoritative for: Generator algorithms, DSP/render details, package/preset compatibility, library storage/indexing, or final resource file format.

## The creative loop

A sample is a first-class project resource. The intended loop is:

```text
discover/create sound
    -> audition variations
    -> accept as sample
    -> use in pattern/music
    -> transform/render musical material
    -> create a new sample
    -> reuse and transform again
```

The transition between exploration and musical context should be unusually cheap. Sample Lab is
a dedicated creative workspace pane, with behavior/layout owned by [WORKSPACE](WORKSPACE.md).
It is not isolated from the composition. No Sample Lab is implemented yet.

## Browser discovery and ownership

Browser is a discovery/access surface for finding and auditioning available material, not an ownership
model or a universal asset identity. These accepted future semantics distinguish five roles:

| Role | Meaning |
| --- | --- |
| Browser/discovery surface | Presents available sources/material and requests preview or explicit use |
| Source/origin | Filesystem file, installed pack, generated candidate, preset source, project resource or library item from which material is offered |
| Project-managed resource/state | Durable audio or sound/plugin configuration actually accepted into one project; resource and placement identities remain separate |
| Personal Library | User-owned reusable content intentionally retained independently across projects; not one project's media directory or application preferences |
| Preset/template source | Reusable configuration applied to create/update project-owned sound/plugin state under [Extensions](EXTENSIONS.md#preset-sources-and-project-state) |

A source may itself have a durable owner, such as another project or the Personal Library. Presenting it
in Browser does not transfer that ownership. Equal bytes, names or paths are insufficient to collapse
filesystem, pack, personal-library and project meanings into one identity. Future hashing/deduplication
may optimize storage only while preserving distinct ownership, provenance and edit relationships.

Browser should be capable of presenting user-chosen filesystem locations, installed content packs,
current project resources, personal reusable samples and presets where meaningful. Recent/favorite
access may be added later. These can share discovery without sharing ownership; not every source class
is required in the first implementation. Initial discovery can remain filesystem/source-oriented.
The bounded capability owners are in [Roadmap](ROADMAP.md#discovery-and-reuse-ownership).

### Preview, contextual audition and accepted use

| Intention | Project effect |
| --- | --- |
| Raw/solo preview | Transient listening; no project media acceptance, canonical edit or project Undo entry |
| Contextual temporary audition | Reversible listening in an identified musical/processing context; no implicit import, replacement or project Undo entry |
| Explicit use/import/acceptance | Validates its intended destination and commits the appropriate durable project resource/reference/state and use changes through normal project editing |

Repeatedly auditioning `D:\Samples\Kick\kick_17.wav` does not import it or make it saved/recoverable
project material. Preview does not survive project close as an accepted resource; project-bound audition
ends on close. Remembering a Browser location/recent item or retaining a disposable preview cache, if
later offered, would be separate from acceptance and would not guarantee the source's availability.
If the file disappears after preview, further source access/audition/use may be unavailable until
restored; a prior audition is not proof of durable acceptance. Unavailability needs useful source-level
feedback, not a fabricated project-media blocker.

Dragging that file into a project or choosing explicit use/import follows the existing
[acceptance boundary](#acceptance-and-provenance). After successful acceptance, moving/deleting its
original path cannot break project use. The same applies to a used pack or Personal Library sample;
unused source contents need not be accepted. Source provenance may be useful, but is not a required live
dependency under [Project format](PROJECT_FORMAT.md#source-provenance-and-reusable-content).

Where meaningful, existing Browser material should be quickly auditionable in musical context before
acceptance, alongside raw preview. It shares Sample Lab's reversible, target-validated audition boundary;
it does not silently replace accepted material or create a permanent resource merely to preview it.
Q-011 retains concrete substitution/publication, downstream scope and stop/cancel/restoration mechanics;
this requirement does not prescribe a universal contextual-preview feature for every source type.

### Reusing current project resources

An open project needs a discoverable way to find, audition and explicitly reuse its already accepted
imported/generated/resampled samples. A small project-resource view/access path is sufficient; no
catalog, full media-asset-management system or global publication is required. Reuse can create a new
musical placement/reference to existing managed audio without destructively rewriting its other uses.
It is a canonical project edit where it changes the document, under the existing separate identities
and Undo rules. Exact reference, sharing and target scope remain Q-029.

Browser finds existing material; Sample Lab explores/generates/mutates candidates and contextual
alternatives. They interoperate through the same accepted project-resource path:

```text
Browser source -> preview/audition -> explicit project use
Sample Lab candidate -> explicit acceptance -> project resource -> audition/reuse in that project
project/generated/resampled sample -> explicit Personal Library publication
Personal Library -> Browser -> explicit use in a future project
```

Acceptance/rendered sample creation gives immediate reuse inside the current project, without making
it global. There is no separate hidden Sample Lab sample universe. Browser and Sample Lab keep distinct
responsibilities; their exact UI composition remains open under [Workspace](WORKSPACE.md).

## Personal Library and explicit publication

Personal Library is intentionally retained user work/content, initially justified for reusable samples
and user-created presets; other content needs a later demonstrated workflow. Every imported project
file, generated candidate, render or project plugin state must not automatically become a global item.
Accepting generated audio into Project X creates project material only. Creating an accepted resampled
object likewise creates a reusable project resource. Ordinary project Save does not publish either.

An explicit reusable-library action may publish selected project/candidate material or intentionally
retain an external favorite independently of its arbitrary original source. A library item must not
claim independent retention merely by registering a fragile path to disposable project/candidate media.
The action establishes library ownership; copying, registering a safely owned representation, storage
layout and completion/failure mechanics are unselected. No final command wording is selected.

```text
Personal Library sample
    +-> accepted use in Project A -> Project A managed resource
    +-> accepted use in Project B -> Project B managed resource
```

Removing the library item later cannot break either accepted project use. Project cleanup cannot
delete the personal-library original; library ownership is independent even if future storage is shared.
Temporarily unavailable library folders/sources affect Browser access, not healthy self-contained
project uses. Removing a source does not implicitly edit all projects that once used it, nor require
crawling every project. Known active preview/preparation use still needs safe lifetime handling under
[Architecture](ARCHITECTURE.md#document-integrity-and-asynchronous-publication).

Personal-library publication is a user-content mutation separate from project document Undo. A command
that intentionally both accepts/edits project material and publishes reusable content has two effects
and must report their outcomes honestly: project Undo reverses the project edit, not the independent
library publication. Failure must not masquerade as success of both. Generated/rendered preparation
remains async work until the appropriate explicit acceptance/publication boundary. No global library
Undo stack, cross-owner atomic storage transaction or final history UX is implied. Preset application
and explicit saving use the [preset contract](EXTENSIONS.md#preset-sources-and-project-state).

Preference reset cannot erase library samples, user-created presets or other retained reusable content
under [Settings](SETTINGS.md#reset-boundary). Library backup/storage policy remains open.

## Discovery evolution limits

Users need a practical way to find material by source/location and name. Folders, categories,
favorites/tags or search can grow with evidenced need; none selects a mandatory metadata model now.
No global background filesystem crawling, mandatory catalog database, AI tagging, waveform-analysis
index, cloud account/sync, store/marketplace, complex hierarchical catalog or universal tags is implied.

Q-065 retains exact Browser UI/layout, library format, scanning/indexing/watching, metadata, search/
tags/favorites, deduplication/content addressing, waveform caching, preset format/versioning, bindings,
performance/scaling, cross-platform paths and backup/sync/import/export. Linked/live presets require
separate justification if ever proposed. Q-071 retains candidate similarity/locks/history; previewing
or retaining exploration history alone does not publish a reusable-library item.

## Standalone and contextual Sample Lab

Two accepted conceptual entry modes serve the same exploration surface:

```text
Standalone: open Sample Lab -> discover / generate / mutate -> audition -> accept -> add/use material
Contextual: select Kick -> open Sample Lab with target/context -> generate / similar / mutate
           -> audition candidate in current music -> accept explicitly
```

Standalone exploration requires no selected target. Contextual exploration knows the active musical
sound/material and lets the user answer whether a variation improves the current music without
export/import, manual source replacement, rebuilding routing, or remembering previous processing.

Contextual candidates should normally be heard through the processing that would actually affect
the target in the current composition:

```text
candidate source -> existing local processing -> containing processing -> relevant downstream mix
                 -> audition
```

Solo/raw audition may also exist, but contextual audition is first-class. An attractive isolated
preview must not be the only way to judge a candidate that sounds substantially different after
acceptance in the real project. Audition remains temporary and reversible until explicit acceptance;
it must not silently rewrite accepted musical material or a shared audio resource.

Substitution mechanics, stop/cancel/restore, realtime publication and downstream boundaries remain
open (Q-011). Pending generation and acceptance follow the
[async commit gate](ARCHITECTURE.md#async-commit-gate); the cases below specialize validity/reuse.
Q-063 retains concrete history, precondition and cancellation mechanisms, not an open choice about
whether completion alone can mutate the target.

### Contextual generation result validity

Starting contextual generation for Kick #42 captures its original document/lifecycle, logical target
and expected relationship/scope, relevant source/settings and processing context/dependencies, plus
operation intent, relevance and cancellation status. Target may mean a sound definition or a placement
according to the explicitly identified workflow; Q-029 retains exact acceptance scope. Current selection
is never a replacement for that captured destination. Generation produces candidate material, not an
automatic canonical edit; even a valid contextual result needs explicit acceptance. Temporary contextual
audition must also validate its required target/context and use the reversible preview/publication path.

Each following change is assessed independently against that launch state:

| Change before completion | Candidate/acceptance disposition |
| --- | --- |
| Select Snare | Selection alone does not invalidate Kick's inputs or redirect its candidate to Snare. Candidate may remain available for Kick; active contextual audition may need suspension/revalidation. Acceptance still targets the identified Kick scope |
| Edit Kick | If relevant source/settings/context changed, the result is stale for the original request and cannot silently overwrite the edit. An unrelated change need not invalidate known sufficient preconditions. Offer clearly identified old material for fresh explicit acceptance/reuse where meaningful, or recompute for a current contextual result |
| Delete Kick | Targeted acceptance/audition is unavailable. Cancel/invalidate the request, or keep safely owned unattached/suspended candidate work where the workflow explicitly supports it; never bind to a similarly named/positioned or selected object |
| Undo deletion | Restoring the same logical identity may permit revalidation only if relevant dependencies/relationships/context match and the request was retained rather than cancelled/invalidated. Otherwise useful material can only be accepted through a fresh explicit action. If Kick was also edited since launch, Undo of deletion alone does not remove that staleness |
| Switch project | An open original project retains its identity; the candidate never targets the newly active project. Retained exploration may continue, but active audition/context and eventual explicit acceptance need their own valid destination/gate |
| Close original project | No canonical acceptance or target audition may occur after close. Cancel/invalidate project-bound work; independently owned reusable material may survive only under an explicitly justified candidate/export workflow, otherwise discard/lifecycle-clean safely |

Undo of the state that made generation relevant must not let late completion resurrect it. Redo alone
does not renew invalidated/cancelled work; restored preconditions and relevance must be established under
[history rules](ARCHITECTURE.md#history-and-pending-work-relevance). Operation-specific candidate retention,
discard/reuse, dependency validation and suspension controls remain open. Candidate retention alone
does not publish to the Personal Library; no catalog or universal stale-result policy is introduced.
Explicit reuse selects and validates a destination anew.

## Specialized generation in a shared exploration surface

Prefer meaningful specialized generators or modes inside one exploration surface over a gigantic
universal synthesizer displaying every algorithm simultaneously. Possible families include percussion,
clicks/impacts, noise/rhythmic noise, waves/wind/atmospheres, synthetic or physical-model-inspired
plucks, guitar/string-like sounds, and experimental textures. These are ideas, not V1 scope.

The first generator should be local/procedural and cover one or two bounded families in SEQ-R7.
[EXTENSIONS](EXTENSIONS.md#default-generator) owns its default-installed/removable status.

## Intentional and lazy exploration

Random exploration must have structure rather than randomizing every parameter indiscriminately.
The long-term interaction should be capable of:

- Give me another sound, or something similar.
- Vary this more.
- Preserve the attack while changing the tail.
- Lock selected properties while exploring others.
- Return to an earlier candidate and compare several candidates.

These are semantic goals; distance metrics, locks, similarity algorithms, history retention, and
comparison controls are not specified yet. [UX_CONTRACT](UX_CONTRACT.md) owns general feedback and
discoverability principles. Candidate work must remain bounded and outside the realtime thread.

## Acceptance and provenance

Acceptance makes the rendered audio durable project material. The audio is the result; it must remain
available when its generator is removed. Where useful, additionally preserve generator identity,
version, parameters, random seed, and recipe metadata. A seed/recipe is not a substitute for audio.

Ordinary import/drag-and-drop and accepted Sample Lab output create project-managed durable media,
alongside recordings, used pack audio and other accepted material, including in an unnamed project.
Successful acceptance internalizes/manages the resource: moving/deleting the original arbitrary external
file must not break normal project use. Durability does not depend on the generator, pack or external
folder remaining available. Any advanced deliberate external-reference workflow needs explicit separate
justification and clear distinction from ordinary import;
[PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary) owns storage policy.

Project state may claim durable availability only after successful placement in managed durable storage.
Failed copy/conversion/write or interrupted acceptance must preserve the previously valid saved state.
The managed representation may differ from the source; no transcoding/storage format, source-retention
rule or codec library is chosen, and disposable cache is never the only durable copy. Document recovery
does not recreate produced audio; recording/generation must leave room for reconciliation with separately
managed durable material. Retained Undo/recovery/pending work can still need apparently unused media;
no eager deletion follows visible-reference removal. [PROJECT_FORMAT](PROJECT_FORMAT.md#media-and-persistence-integrity)
owns transaction/cleanup contracts and their open mechanisms.

Missing/corrupt individual media normally degrades its affected portions when the document is safely
understandable. Preserve visible broken-resource/item state and allow inspection, repair/relink,
replacement or removal where meaningful, with unaffected work available. Never silently substitute
unrelated audio; [UX_CONTRACT](UX_CONTRACT.md#project-availability-and-dependency-blockers) owns persistent feedback.

Opening an old project must not silently regenerate accepted audio using a newer algorithm and change
the composition. Regeneration with available code is a separate explicit creative action.
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns storage and compatibility, and [EXTENSIONS](EXTENSIONS.md)
owns missing-generator behavior.

A durable sample and its musical placements are distinct. For example, `kick_017.wav` may supply
items A, B, and C; ordinary editing/processing of A must not unexpectedly rewrite the resource or
change B and C. Non-destructive placement processing is the default. Explicit destructive/edit-source
operations may be considered later but are not implied by normal processing or contextual acceptance.
The exact acceptance edit for a target and shared-use behavior still need design.
[ARCHITECTURE](ARCHITECTURE.md) owns identity boundaries; [PROJECT_FORMAT](PROJECT_FORMAT.md) owns persistence.

Successful acceptance groups the intended project resource/reference and target/use changes into one
[logical undo transaction](ARCHITECTURE.md#logical-undo-transactions-and-history-scope), after durable
placement and commit revalidation. Undo removes/reverts accepted active use; it does not mean deleting
the durable audio or every other placement using it. History/recovery/pending owners may retain it.

## Asynchronous preparation and dependency availability

Generation/preparation/render work needed by user-visible operations requires cancellation, visible
state/progress and finite failure handling under [AUDIO_ENGINE](AUDIO_ENGINE.md#bounded-asynchronous-preparation).
Do not wait forever on stalled workers/plugins. In a prepare-before-commit workflow, failure produces
no canonical edit or Undo entry and leaves existing realtime execution intact. An intentional earlier
canonical edit remains undoable if secondary preparation later fails under
[failure semantics](ARCHITECTURE.md#failure-cancellation-and-non-commit). No universal timeout is selected.

Render/resampling freezes and validates/prepares the relevant canonical project revision under
[AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction). Required invalid state or unavailable
dependencies block the affected operation with object-level diagnostics; never silently sample older
playing state or omit required processing and report success. Semantic object boundaries are below;
concrete execution taps and broader capture scope remain open.

Creating a sample separates these boundaries:

```text
identify/freeze required canonical scope and dependencies
    -> prepare/render asynchronously -> durable managed media placement succeeds
    -> revalidate original project/target/operation and sufficient preconditions
    -> commit intended sample/reference/use as one document Undo transaction
```

Render computation and file creation are preparation/resource events, not Undo entries. The commit
validates both the frozen result's identity/provenance and its intended current use; completion cannot
select a new destination. If relevant R10 inputs are now R14, recompute when the request requires the
current object's result. A clearly identified frozen result may instead be explicitly reused where
meaningful; safe rebasing requires operation-specific evidence under the architecture gate.

If durable audio exists but commit is stale, cancelled, invalid or fails, do not attach it incorrectly
or leave half the intended edit accepted. It becomes an explicitly owned unattached candidate/orphan/
reusable artifact, or is lifecycle-cleaned according to the workflow; durable storage success alone
does not claim project acceptance. Q-059 retains storage/cleanup implementation. Undo of a successful
commit reverts active project relationships while lifecycle-aware retention protects media needed by
history/recovery/pending work; it does not reverse a computation or immediately delete its file.

## Resampling

Two accepted operations have different meanings. `Create Sample from Object` and `Capture Audible
Selection` are conceptual names, not final labels, commands, or render taps. Ordinary use must not
require a routing questionnaire; advanced raw/source or before-processing alternatives remain open.

### Create Sample from Object

Create durable audio from the selected object's own semantic/local processing boundary:

```text
Kick source -> Kick local EQ -> Kick local Gain -> new durable sample
```

This answers "Turn this object, including its own processing, into reusable audio." It does not
automatically bake unrelated containing-container, Mixer channel/bus, or Master processing merely because those
are heard downstream. For a selected container, its own boundary includes its contained audible items
and container processing; that does not automatically extend to unrelated downstream context.

The selection's semantic owner determines the stop point, independently of where its controls appear:

| Selected object | Included result / stop point |
| --- | --- |
| Pattern musical content without a placement context | Its instrument/sound-definition uses executing its events; no arbitrary Arrangement placement or downstream Mixer context is inherited |
| Pattern placement or another standalone item | That occurrence's contributions and own item-local paths, including its intentional whole-placement submix/processing if present; before containing-context/global processing |
| Musical container with common processing | Its contained item results through its own explicit container submix/processing; before broader downstream channel/bus/Master processing |

For `Drums Main`, a reusable sample may require combining Kick and Snare into the rendered audio:

```text
Kick selected-object contribution  --+
Snare selected-object contribution -+-> object render output mix -> new durable sample
```

Honor the selected object's independent paths before this final render-output convergence. It creates
a combined audio artifact, not an implicit playback bus or a change to the source Pattern's routing.
The resulting mixed sample no longer exposes its constituent instruments independently; the source
remains preserved. If the selected placement already has a whole-placement submix/Compressor, render
its processed result instead of recovering independent instruments from it.

A container Compressor exposed in Mixer is still included when sampling that container, because it
is that container's own context, and is applied once. The same Compressor is excluded when sampling
an individual contained item. An unrelated global bus or Master is excluded in both cases. Shared
route processing of an aggregate containing contributions beyond the object cannot silently count
as exclusively object-owned. Cross-scope sidechain/control dependencies remain Q-066, not an automatic
extension of the object's audio aggregation boundary.
[ARCHITECTURE](ARCHITECTURE.md#arrangement-context-and-mixer-presentation) owns that relationship;
[NODE_GRAPH](NODE_GRAPH.md#contributions-and-irreversible-mixing) owns signal convergence. Exact taps,
dependency closure, musical range/tails, rate/channel negotiation and replacement edits remain open
under Q-012/Q-021/Q-047/Q-049/Q-057; no renderer or capture mechanism is selected.

### Capture Audible Selection

Capture the broader audible result of a selected musical/time context:

```text
selected sources -> containing processing -> relevant downstream audible context -> captured result
```

This answers "Record the sound I am hearing from this selected context/range." Exact downstream
boundaries, buses/sends/Master inclusion, range/tails, and execution taps require later bounded examples.
The operation is not automatically whole-Master capture. Contextual audition may include broader
downstream processing than an object render; hearing that context does not silently expand the object
sampling boundary.

### Source preservation and rendered replacement

Future candidate sources include an instrument playing a note, a pattern, an arrangement range,
multiple selected sources, and generated audio. Converting them to a sample should preserve the
source by default and make the resulting sample immediately reusable.

An explicit replace/disable-source action may be added later. If offered, its accepted source disable/
removal/replacement, sample reference and necessary processing relationship changes must form one
coherent user-level Undo transaction after async rendering, durable placement and commit validation.
Preparation adds no separate Undo steps. Undo restores the prior source relationships/state and removes
the accepted replacement from active use; retained history/media ownership protects both source and
replacement resources rather than eagerly deleting them. A replacement must not silently reapply the
exact processing already baked into its audio:

```text
source -> EQ -> Gain -> rendered sample
```

must not naively become `rendered EQ/Gain audio -> old EQ -> old Gain`. The edit-state transition
remains open: bypass/removal of the baked local chain, retaining it in history, or another explicit
reversible transformation may be evaluated later. None is selected. Accepting a render does not delete
or mutate its source by default. Start with one bounded source in SEQ-R8 rather than treating the full
candidate list or both complete operations as required initial scope.

Musical bounds, tails, insert/send inclusion, routing capture, normalization, sample rate, channels,
latency compensation, and cancellation are open. [AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction)
owns execution/render semantics; [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) records validation gaps.
