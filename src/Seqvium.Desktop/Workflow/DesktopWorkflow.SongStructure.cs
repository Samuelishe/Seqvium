// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal Task<bool> EditPlaylistSelectionAsync(ArrangementEditBasis basis,
        IReadOnlyList<Id<ArrangementClip>> patterns, IReadOnlyList<Id<AudioClip>> audio,
        long tickDelta = 0, int laneDelta = 0, bool copy = false, bool delete = false, bool duplicate = false)
    {
        var patternIds = patterns.ToArray();
        var audioIds = audio.ToArray();
        return Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit(delete ? "Delete Playlist clips" : copy || duplicate ? "Copy Playlist clips" : "Move Playlist clips",
                e => e.EditPlaylistSelection(patternIds, audioIds, tickDelta, laneDelta, copy, delete, duplicate)));
        });
    }

    internal Task<bool> PastePlaylistAsync(ArrangementEditBasis basis, ArrangementClip[] patterns, AudioClip[] audio,
        long ticks, int lane) => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        return Task.FromResult(_document.Edit("Paste Playlist clips", e => e.PastePlaylistClips(patterns, audio, new(ticks), lane)));
    });

    internal Task<bool> MakeArrangementVariationAsync(ArrangementEditBasis basis, Id<ArrangementClip> id, string name)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit("Make Playlist Pattern variation", e => e.MakeArrangementVariation(id, name)));
        });

    internal Task<bool> EditSectionAsync(ArrangementEditBasis basis, Id<ArrangementSection>? id,
        string name, long start, long length, int color) => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        return Task.FromResult(_document.Edit(id is null ? "Create Playlist section" : "Edit Playlist section", e =>
        {
            if (id is { } existing) e.UpdateArrangementSection(existing, name, new(start), new(length), color);
            else e.AddArrangementSection(name, new(start), new(length), color);
        }));
    });

    internal Task<bool> SectionCommandAsync(ArrangementEditBasis basis, Id<ArrangementSection> id, string command)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit(command + " Playlist section", e =>
            {
                if (command == "Duplicate") e.DuplicateArrangementSection(id);
                else if (command == "Delete") e.DeleteArrangementSection(id);
                else e.MoveArrangementSection(id, command == "Later");
            }));
        });
}
