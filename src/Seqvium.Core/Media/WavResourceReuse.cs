// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Explicit independent sound configuration over one existing accepted resource.
/// No physical copy, new resource identity, automatic part retarget or shared-sound mutation.</summary>
public sealed class WavResourceReuse : IDisposable
{
    private readonly ProjectDocument _document, _frozen;
    private readonly Id<ResourceDescriptor> _resource;
    private readonly string _name;
    private readonly decimal _rootPitch, _releaseMilliseconds;
    private readonly long _generation;
    private CancellationToken _cancellation;
    private bool _started, _prepared, _ended;

    internal WavResourceReuse(ProjectDocument document, Id<ResourceDescriptor> resource, string name,
        decimal rootPitch, decimal releaseMilliseconds)
    {
        _frozen = SourceAccess.Capture(document);
        if (!_frozen.Current.State.Resources.Any(item => item.Id == resource))
            throw new InvalidOperationException("Resource does not belong to the current project.");
        ProjectValidation.Require(
            !string.IsNullOrWhiteSpace(name) && name.Length <= ProjectValidation.MaximumTextLength,
            "Invalid sound name.");
        PcmSampler.ValidateConfiguration(rootPitch, releaseMilliseconds);
        _document = document;
        _resource = resource;
        _name = name;
        _rootPitch = rootPitch;
        _releaseMilliseconds = releaseMilliseconds;
        _generation = document.Generation;
    }

    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        if (_started || _ended) throw new InvalidOperationException("Reuse preparation is single-use.");
        _started = true;
        _cancellation = cancellationToken;
        await Task.Run(() =>
        {
            using (ProjectMedia.Decode(_frozen, _resource))
            {
            }
        }, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        _prepared = true;
    }

    public MediaAcceptance Accept(CancellationToken cancellationToken = default)
    {
        if (!_prepared || _ended) throw new InvalidOperationException("Reuse has no prepared result.");
        CheckCancellation(cancellationToken);
        _document.CheckAvailable();
        if (_document.Generation != _generation || _document.Current.Revision != _frozen.Current.Revision)
            throw new InvalidOperationException("Stale resource reuse; current project authority changed.");
        // As with F1 import, verify current integrity before the owning edit. Never reuse degraded media as healthy.
        using (ProjectMedia.Decode(_document, _resource))
        {
        }

        CheckCancellation(cancellationToken);
        Id<SoundDefinition> sound = default;
        _document.Edit("Reuse project WAV as independent sound", edit =>
        {
            CheckCancellation(cancellationToken);
            sound = edit.AddSound(_name, PcmSampler.Algorithm, [_resource]);
            edit.ConfigurePcmSampler(sound, _resource, _rootPitch, _releaseMilliseconds);
            CheckCancellation(cancellationToken);
        });
        _ended = true;
        return new(_resource, sound);
    }

    private void CheckCancellation(CancellationToken token)
    {
        if (_cancellation.IsCancellationRequested || token.IsCancellationRequested)
        {
            _ended = true;
            _cancellation.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
        }
    }

    public void Dispose() => _ended = true;
}
