// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private WavAudition? _browserAudition;

    private void ValidateBrowserSource(BrowserSource source)
    {
        if (source.Directory || (source.Path is null) == (source.Resource is null))
            throw new InvalidOperationException("Browser.SourceUnavailable");
        if (source.Resource is { } id && (source.Lifecycle != _document.LifecycleId ||
            !_document.Current.State.Resources.Any(r => r.Id == id)))
            throw new InvalidOperationException("Beat.Stale");
    }

    private Task<DecodedPcm> ReadBrowserPcmAsync(BrowserSource source, CancellationToken cancellation)
    {
        ValidateBrowserSource(source);
        if (source.Resource is { } resource) return SourceAccess.ReadResourceAsync(_document, resource, cancellation);
        return Task.Run(() =>
        {
            string temporary = Path.Combine(Path.GetTempPath(), $"seqvium-preview-{Guid.NewGuid():N}.wav");
            try
            {
                try { AudioFileDecode.Normalize(source.Path!, temporary, cancellation); }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    if (error.Message.StartsWith("AudioClip.", StringComparison.Ordinal)) throw;
                    throw new IOException("AudioClip.DecodeFailed", error);
                }
                return AudioMedia.ReadTemporaryWave(temporary, cancellation);
            }
            finally { File.Delete(temporary); }
        }, cancellation);
    }

    internal async Task<BrowserAudioInfo> InspectBrowserSourceAsync(BrowserSource source, CancellationToken cancellation)
    {
        var pending = await _owner.Run(() => Task.FromResult(ReadBrowserPcmAsync(source, cancellation)));
        using var pcm = await pending.ConfigureAwait(false);
        return await Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            var waveform = AudioWaveform.FromPcm(pcm, cancellation);
            cancellation.ThrowIfCancellationRequested();
            return new BrowserAudioInfo(pcm.SampleRate, pcm.Channels, pcm.Frames, waveform);
        }, cancellation).ConfigureAwait(false);
    }

    internal Task<bool> PreviewBrowserSourceAsync(BrowserSource source, CancellationToken cancellation = default)
        => Command("Browser.PreviewFailed", async () =>
        {
            ValidateBrowserSource(source);
            if (_coordinator?.ReadStatus().Execution.Playing == true)
            {
                _status = "Browser.StopTransport";
                return false;
            }
            await CloseNoteAuditionAsync();
            long generation = _playGeneration;
            long documentGeneration = _document.Generation;
            // Decoding failure still replaces the old preview. No project media directory is created.
            using var pcm = await ReadBrowserPcmAsync(source, cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (generation != _playGeneration || documentGeneration != _document.Generation) return false;
            _noteOutput = _openOutput();
            try
            {
                _noteSession = new();
                _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
                _browserAudition = new(_noteOutput.Sampler, _document);
                using var request = source.Resource is { } resource
                    ? _browserAudition.BeginResource(_document, resource)
                    : _browserAudition.BeginExternal(source.Path!);
                await request.PrepareAsync(pcm, cancellation);
                ValidateBrowserSource(source);
                if (generation != _playGeneration || request.Publish() != SamplerPublication.Accepted)
                {
                    await CloseNoteAuditionAsync();
                    return false;
                }
                _status = "Browser.Previewing";
                return true;
            }
            catch { await CloseNoteAuditionAsync(); throw; }
        });

    internal Task StopBrowserPreviewAsync() => _owner.Run(() =>
    {
        _browserAudition?.Stop();
        if (_status == "Browser.Previewing") _status = "Workflow.Stopped";
        Publish();
        return Task.FromResult(true);
    });

    internal Task<bool> UseBrowserAudioAsync(ArrangementEditBasis basis, BrowserSource source,
        MusicalPosition position, int lane, CancellationToken cancellation = default)
    {
        if (source.Path is { } path) return ImportAudioClipAsync(basis, path, position, lane, cancellation);
        return Command("Workflow.ImportFailed", async () =>
        {
            ValidateArrangementBasis(basis);
            ValidateBrowserSource(source);
            using var request = await AudioMedia.PrepareReuseAsync(_document, source.Resource!.Value, cancellation);
            ValidateArrangementBasis(basis);
            var id = request.Accept(position, lane, cancellation);
            var clip = _document.Current.State.AudioClips.Single(c => c.Id == id);
            _audioWaveforms = _audioWaveforms.SetItem(clip.SoundId, request.Waveform);
            if (_document.Current.State.Patterns.IsEmpty)
            {
                _songMode = true;
                _coordinator?.SelectArrangement(true);
            }
            _status = "Workflow.Ready";
            return true;
        });
    }

    internal Task<bool> UseBrowserRackAsync(ArrangementEditBasis basis, Id<GraphAttachment>? target,
        BrowserSource source, CancellationToken cancellation = default) => Command("Workflow.ImportFailed", async () =>
    {
        ValidateArrangementBasis(basis);
        ValidateBrowserSource(source);
        // Preserve the established pitched WAV source limits and resource model.
        if (source.Path is { } path && !Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
            source.Resource is { } resource && !_document.Current.State.Resources.Any(r =>
                r.Id == resource && r.ManagedLocator?.StartsWith("wav/", StringComparison.Ordinal) == true))
            throw new InvalidOperationException("Browser.RackWavOnly");
        if (target is { } attachmentId)
        {
            var (pattern, _, attachment, graph) = ValidateBeatBasis(new(basis.Lifecycle, basis.Generation,
                attachmentId, _document.Current.State.Placements.Single(p => p.ItemContextId ==
                    _document.Current.State.GraphAttachments.Single(a => a.Id == attachmentId).ContextId).PatternId));
            if (BeatChannelRefusal(_document.Current.State, pattern, attachment, graph) is { } refusal)
                throw new InvalidOperationException(refusal);
        }
        else if (_document.Current.State.Settings.Meter is not { Numerator: 4, Denominator: 4 })
            throw new InvalidOperationException("Beat.MeterUnsupported");
        string name = Path.GetFileNameWithoutExtension(source.Name);
        GraphAttachment? created = null;
        void CreateUse(ProjectEdit edit, MediaAcceptance media)
        {
            var state = _document.Current.State;
            var attachment = target is { } id ? state.GraphAttachments.Single(a => a.Id == id) : null;
            if (attachment is null)
            {
                var patternId = edit.AddPattern("Pattern", new(BeatSteps.BarTicks));
                var placementId = edit.AddPlacement(patternId, default);
                created = attachment = edit.CreateItemGraph(placementId);
                var gain = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(360, 0));
                var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(540, 0));
                edit.SetGraphGain(attachment.GraphId, gain.Id, .25m);
                edit.ConnectGraphPorts(attachment.GraphId, gain.Id,
                    gain.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id, output.Id, output.Ports.Single().Id);
                edit.SetGraphOutput(attachment.Id, output.Id);
                var part = edit.AddPart(patternId, name, media.SoundId);
                var node = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                edit.BindGraphSource(attachment.Id, node.Id, placementId, part);
                edit.ConnectGraphPorts(attachment.GraphId, node.Id, node.Ports.Single().Id, gain.Id,
                    gain.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
            }
            else
            {
                var placement = state.Placements.Single(p => p.ItemContextId == attachment.ContextId);
                var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
                AttachBeatSound(edit, pattern, placement, attachment, state.Graphs.Single(g => g.Id == attachment.GraphId),
                    media.SoundId, name);
            }
        }
        _status = "Workflow.Importing";
        Publish();
        if (source.Resource is { } existing)
        {
            using var request = ProjectMedia.BeginReuse(_document, existing, name, releaseMilliseconds: 0);
            await request.PrepareAsync(cancellation);
            ValidateArrangementBasis(basis);
            request.Accept(CreateUse, cancellation);
        }
        else
        {
            var bytes = await Task.Run(async () =>
            {
                await using var file = File.OpenRead(source.Path!);
                return await WavDecoder.ReadBytesAsync(file, cancellation);
            }, cancellation);
            ValidateArrangementBasis(basis);
            using var request = ProjectMedia.BeginImport(_document, name,
                rootPitch: SampleRootMetadata.Read(source.Path!, bytes), releaseMilliseconds: 0,
                ownedMediaDirectory: _mediaDirectory);
            using var stream = new MemoryStream(bytes, false);
            await request.PrepareAsync(stream, cancellation);
            ValidateArrangementBasis(basis);
            request.Accept(CreateUse, cancellation);
        }
        if (created is not null)
        {
            _selected = created.Id;
            _coordinator?.SelectTarget(created.Id);
        }
        _status = "Workflow.Ready";
        return true;
    });
}
