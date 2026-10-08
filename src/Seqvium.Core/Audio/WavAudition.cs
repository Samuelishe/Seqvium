// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Raw/solo one-shot owner, optionally bound to a document lifetime. Never imports or edits.
/// Control calls are serialized; await requests before disposal, join output before disposing this owner.</summary>
public sealed class WavAudition : IDisposable
{
    private readonly ProjectDocument? _context;
    private bool _ended, _disposed;
    public const float InitialGain = 0.05f;
    public RealtimeSampler Processor { get; }

    public WavAudition(int sampleRate, int channels, int maximumPacketFrames, ProjectDocument? context = null)
    {
        context?.CheckAvailable();
        Processor = new(sampleRate, channels, maximumPacketFrames);
        Processor.SetGain(InitialGain);
        _context = context;
        if (context is not null) context.Closed += EndLifetime;
    }

    public WavAuditionRequest BeginExternal(string path)
    {
        CheckAvailable();
        string source = Path.GetFullPath(path);
        long authority = Replace();
        return new(this, authority, source, null, null, default);
    }

    public WavAuditionRequest BeginResource(ProjectDocument document, Id<ResourceDescriptor> resource)
    {
        CheckAvailable();
        if (!ReferenceEquals(_context, document))
            throw new ArgumentException("Accepted-resource audition must be bound to that document lifetime.",
                nameof(document));
        var frozen = SourceAccess.Capture(document);
        if (!frozen.Current.State.Resources.Any(item => item.Id == resource))
            throw new InvalidOperationException("Resource does not belong to the current project.");
        long authority = Replace();
        return new(this, authority, null, document, frozen, resource);
    }

    internal bool Available => !_ended && !_disposed && !Processor.IsTerminated;

    private void CheckAvailable()
    {
        if (!Available) throw new InvalidOperationException("Audition lifetime has ended.");
    }

    private long Replace()
    {
        Processor.Stop(); // Old preview ends even if the replacement fails to prepare.
        return Processor.InvalidatePreparation();
    }

    public long Stop()
    {
        CheckAvailable();
        Processor.InvalidatePreparation();
        return Processor.Stop();
    }

    /// <summary>Ends source/lifecycle authority immediately on the control owner; packet Stop follows.
    /// Reclamation stays control-side and disposal still requires joined output.</summary>
    public void EndLifetime()
    {
        if (_ended || _disposed) return;
        if (Processor.IsTerminated)
        {
            _ended = true;
            return;
        }

        Stop();
        _ended = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_context is not null) _context.Closed -= EndLifetime;
        _disposed = true;
        _ended = true;
        Processor.Dispose();
    }
}

/// <summary>Single-use transient preparation. Caller owns a supplied stream. One candidate is admitted;
/// await completion before Publish/Dispose. Cancellation remains effective after publication.</summary>
public sealed class WavAuditionRequest : IDisposable
{
    private readonly WavAudition _owner;
    private readonly long _authority, _generation;
    private readonly string? _path;
    private readonly ProjectDocument? _document, _frozen;
    private readonly Id<ResourceDescriptor> _resource;
    private CancellationToken _cancellation;
    private PreparedSampler? _plan;
    private bool _started, _ended, _reserved;

    internal WavAuditionRequest(WavAudition owner, long authority, string? path, ProjectDocument? document,
        ProjectDocument? frozen, Id<ResourceDescriptor> resource)
    {
        _owner = owner;
        _authority = authority;
        _path = path;
        _document = document;
        _frozen = frozen;
        _resource = resource;
        _generation = document?.Generation ?? 0;
    }

    public Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareCore(null, cancellationToken);

    public Task PrepareAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_path is null) throw new InvalidOperationException("Stream preparation is external audition only.");
        return PrepareCore(source, cancellationToken);
    }

    private async Task PrepareCore(Stream? source, CancellationToken cancellationToken)
    {
        if (_started || _ended) throw new InvalidOperationException("Audition preparation is single-use.");
        _started = true;
        _cancellation = cancellationToken;
        if (!_owner.Available) throw new InvalidOperationException("Audition lifetime has ended.");
        if (!_owner.Processor.TryBeginWork())
            throw new InvalidOperationException("Audition preparation capacity is occupied.");
        _reserved = true;
        try
        {
            DecodedPcm pcm;
            if (source is not null)
            {
                var bytes = await WavDecoder.ReadBytesAsync(source, cancellationToken).ConfigureAwait(false);
                pcm = await Task.Run(() => WavDecoder.Decode(bytes), cancellationToken).ConfigureAwait(false);
            }
            else if (_path is not null)
                pcm = await SourceAccess.DecodeExternalAsync(_path, cancellationToken).ConfigureAwait(false);
            else
                pcm = await Task.Run(() => ProjectMedia.Decode(_frozen!, _resource), cancellationToken)
                    .ConfigureAwait(false);

            using (pcm)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _plan = SamplerPreparation.PrepareOneShot(pcm, _owner.Processor.SampleRate, _owner.Processor.Channels);
            }
        }
        catch
        {
            ReleaseReservation();
            throw;
        }
    }

    public SamplerPublication Publish(CancellationToken cancellationToken = default)
    {
        if (_ended || _plan is null) throw new InvalidOperationException("No prepared audition result.");
        bool valid = _owner.Available && (_document is null || !_document.IsClosed &&
            _document.Generation == _generation && _document.Current.Revision == _frozen!.Current.Revision);
        // The preparation token owns the whole preview lifetime; the additional token gates this publication only.
        var result = _owner.Processor.Publish(_plan, _authority, valid,
            _cancellation.IsCancellationRequested || cancellationToken.IsCancellationRequested, true, _cancellation);
        if (result != SamplerPublication.Accepted) _plan.Dispose();
        _plan = null;
        _ended = true;
        ReleaseReservation();
        if (result == SamplerPublication.Accepted) _owner.Processor.StartPrepared(_authority);
        return result;
    }

    public void Dispose()
    {
        _plan?.Dispose();
        _plan = null;
        _ended = true;
        ReleaseReservation();
    }

    private void ReleaseReservation()
    {
        if (!_reserved) return;
        _reserved = false;
        _owner.Processor.EndWork();
    }
}
