// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal static string? DuplicateChannelRefusal(ProjectState state, Pattern pattern,
        GraphAttachment attachment, GraphDefinition graph, Id<MusicalPart> partId)
    {
        if (BeatChannelRefusal(state, pattern, attachment, graph) is { } graphRefusal) return graphRefusal;
        var part = pattern.Parts.SingleOrDefault(p => p.Id == partId);
        if (part is null) return "Beat.Stale";
        if (PianoNotes.Refusal(state, pattern, part) is { } refusal) return refusal;
        if (!ProjectEdit.CanDuplicatePart(state, pattern.Id, partId)) return "Beat.DuplicateUnsupported";
        return null;
    }

    internal Task<bool> DuplicateBeatChannelAsync(BeatEditBasis basis, Id<MusicalPart> partId, int? index = null)
        => Command("Workflow.EditFailed", () =>
        {
            var (pattern, placement, attachment, graph) = ValidateBeatBasis(basis);
            if (DuplicateChannelRefusal(_document.Current.State, pattern, attachment, graph, partId) is { } refusal)
                throw new InvalidOperationException(refusal);
            var part = pattern.Parts.Single(p => p.Id == partId);
            var names = pattern.Parts.Select(p => p.Name).Concat(_document.Current.State.Sounds.Select(s => s.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            int suffix = 2;
            string stem = part.Name;
            // Keep a repeated copy's label concise, without interpreting a user-authored trailing number.
            string name;
            do
            {
                string ending = " " + suffix++;
                name = stem[..Math.Min(stem.Length, ProjectValidation.MaximumTextLength - ending.Length)] + ending;
            } while (names.Contains(name));

            return Task.FromResult(_document.Edit("Duplicate channel", edit =>
            {
                var copy = edit.DuplicatePartIndependent(pattern.Id, partId, name,
                    index ?? pattern.Parts.IndexOf(part) + 1);
                WireBeatPart(edit, pattern, placement, attachment, graph, copy);
            }));
        });

    internal Task<bool> MoveBeatChannelAsync(BeatEditBasis basis, Id<MusicalPart> partId, int index)
        => Command("Workflow.EditFailed", () =>
        {
            var (pattern, _, _, _) = ValidateBeatBasis(basis, requireGrid: false);
            return Task.FromResult(_document.Edit("Reorder channel", edit => edit.MovePart(pattern.Id, partId, index)));
        });
}
