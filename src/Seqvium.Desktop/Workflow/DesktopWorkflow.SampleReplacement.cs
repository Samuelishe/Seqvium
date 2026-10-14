// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    /// <summary>Replaces the shared sampler resource, retaining musical and processing identities.</summary>
    internal Task<bool> ReplaceBeatWavAsync(BeatEditBasis basis, Id<MusicalPart> partId, string path,
        CancellationToken cancellation = default) => Command("Workflow.ImportFailed", async () =>
    {
        var (pattern, _, _, _) = ValidateBeatBasis(basis, requireGrid: false);
        var part = pattern.Parts.Single(p => p.Id == partId);
        var sound = _document.Current.State.Sounds.Single(s => s.Id == part.SoundId);
        if (sound.Algorithm != PcmSampler.Algorithm || sound.Extension is not null ||
            !sound.AdditionalData.IsEmpty || !sound.Parameters.TryGetValue("rootPitch", out var root) ||
            !sound.Parameters.TryGetValue("releaseMilliseconds", out var release))
            throw new InvalidOperationException("Beat.ReplaceUnsupported");
        _status = "Workflow.Importing"; Publish();
        var prepared = await Task.Run(async () =>
        {
            await using var file = File.OpenRead(path);
            var bytes = await WavDecoder.ReadBytesAsync(file, cancellation);
            return (Bytes: bytes, Root: SampleRootMetadata.ReadKnown(path, bytes) ?? root);
        }, cancellation);
        ValidateBeatBasis(basis, requireGrid: false);
        using var request = ProjectMedia.BeginImport(_document, Path.GetFileNameWithoutExtension(path),
            rootPitch: prepared.Root, releaseMilliseconds: release, target: sound.Id,
            ownedMediaDirectory: _mediaDirectory);
        using var stream = new MemoryStream(prepared.Bytes, writable: false);
        await request.PrepareAsync(stream, cancellation);
        ValidateBeatBasis(basis, requireGrid: false);
        request.Accept(cancellation);
        _status = "Workflow.Ready";
        return true;
    });
}
