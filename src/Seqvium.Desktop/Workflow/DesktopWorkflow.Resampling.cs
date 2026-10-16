// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed record ResamplingState(bool Busy, int Progress, string Name, string Message,
    string? Detail = null, Id<ResourceDescriptor>? Resource = null);

internal sealed partial class DesktopWorkflow
{
    private CancellationTokenSource? _resamplingCancellation;
    private Task<bool>? _resamplingWork;
    private ResamplingState? _resampling;
    private bool _acceptingResample;

    internal Task<bool> ResamplePatternAsync(Guid lifecycle, long generation, Id<GraphAttachment> target,
        CancellationToken cancellation = default) => _owner.Run(() =>
    {
        if (_closed || _busy || _resamplingCancellation is not null ||
            lifecycle != _document.LifecycleId || generation != _document.Generation) return Task.FromResult(false);
        _resamplingWork = ResamplePatternCoreAsync(target, cancellation);
        return _resamplingWork;
    });

    private async Task<bool> ResamplePatternCoreAsync(Id<GraphAttachment> target, CancellationToken cancellation)
    {
        var basis = new ArrangementEditBasis(_document.LifecycleId, _document.Generation);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        lifetime.CancelAfter(TimeSpan.FromSeconds(30));
        _resamplingCancellation = lifetime;
        var token = lifetime.Token;
        _resampling = new(true, 0, "", "Resample.Rendering"); Publish();
        try
        {
            var state = _document.Current.State;
            var attachment = state.GraphAttachments.Single(a => a.Id == target);
            var placement = state.Placements.Single(p => p.ItemContextId == attachment.ContextId);
            var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
            string name = pattern.Name[..Math.Min(pattern.Name.Length, ProjectValidation.MaximumTextLength - 32)] + " (Resampled)";
            // Repeated explicit renders remain individually discoverable without changing musical source names.
            string stem = name;
            for (int suffix = 2; state.Resources.Any(r => r.Name == name); suffix++) name = stem + " " + suffix;
            _resampling = _resampling with { Name = pattern.Name }; Publish();
            using var request = ProjectMedia.BeginImport(_document, name, releaseMilliseconds: 0,
                ownedMediaDirectory: _mediaDirectory, editName: "Create sample from Pattern",
                provenance: $"Pattern resample; Pattern {pattern.Id}; attachment {target}; revision {_document.Current.Revision}; 48000 Hz; local graph before Mixer");
            var sample = await _renderPattern(_document, target, token, percent => _owner.Post(_ =>
            {
                if (_resamplingCancellation != lifetime || token.IsCancellationRequested) return;
                _resampling = _resampling! with { Progress = percent }; Publish();
            }, null));
            token.ThrowIfCancellationRequested(); ValidateArrangementBasis(basis);
            using var wave = new MemoryStream(sample.Wave, writable: false);
            await request.PrepareAsync(wave, token);
            token.ThrowIfCancellationRequested(); ValidateArrangementBasis(basis);
            // New/Open/Close may be awaiting workers inside the ordinary command gate. Never commit into them.
            if (_busy) throw new InvalidOperationException("Resample.Stale");
            _acceptingResample = true;
            MediaAcceptance accepted;
            try { accepted = request.Accept(token); }
            finally { _acceptingResample = false; }
            _resampling = _resampling! with { Busy = false, Progress = 100, Message = "Resample.Ready", Resource = accepted.ResourceId };
            return true;
        }
        catch (OperationCanceledException)
        {
            bool stale = basis.Lifecycle != _document.LifecycleId || basis.Generation != _document.Generation;
            _resampling = _resampling! with { Busy = false, Message = stale ? "Resample.Stale" :
                cancellation.IsCancellationRequested || _resampling!.Message == "Resample.Cancelled"
                    ? "Resample.Cancelled" : "Resample.TimedOut" };
            return false;
        }
        catch (Exception error)
        {
            _resampling = _resampling! with { Busy = false, Message = "Resample.Failed",
                Detail = error is GraphPreparationException graph ? graph.Reason : error.Message };
            return false;
        }
        finally { _resamplingCancellation = null; Publish(); }
    }

    internal Task<bool> CancelResamplingAsync() => _owner.Run(() =>
    {
        if (_resamplingCancellation is not null)
        {
            _resampling = _resampling! with { Message = "Resample.Cancelled" };
            _resamplingCancellation.Cancel(); Publish();
        }
        else { _resampling = null; Publish(); }
        return Task.FromResult(true);
    });

    private async Task CloseResamplingAsync()
    {
        if (_resamplingCancellation is not null)
        {
            _resampling = _resampling! with { Message = "Resample.Cancelled" };
            _resamplingCancellation.Cancel();
        }
        if (_resamplingWork is { } work) await work;
        _resamplingWork = null; _resampling = null;
    }
}
