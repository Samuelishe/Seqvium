// SPDX-License-Identifier: Apache-2.0
using System.Collections.Concurrent;

namespace Seqvium.Desktop.Workflow;

/// <summary>Small host control loop. File publication and device joins never block the GUI.
/// Keep this context alive until all asynchronous preparation and borrower lifetimes have ended.</summary>
internal sealed class WorkflowOwner : SynchronizationContext, IAsyncDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly Thread _thread;
    public WorkflowOwner()
    {
        _thread = new(() =>
        {
            SetSynchronizationContext(this);
            foreach (var (callback, state) in _queue.GetConsumingEnumerable()) callback(state);
        }) { IsBackground = true, Name = "Seqvium document owner" };
        _thread.Start();
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        // Disposed coordinator progress callbacks have no work; completion closes the queue only after join.
        try { _queue.Add((d, state)); }
        catch (InvalidOperationException) when (_queue.IsAddingCompleted) { }
    }

    public Task<T> Run<T>(Func<Task<T>> operation)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(async _ =>
        {
            try { completion.SetResult(await operation()); }
            catch (Exception error) { completion.SetException(error); }
        }, null);
        return completion.Task;
    }

    public async ValueTask DisposeAsync()
    {
        _queue.CompleteAdding();
        await Task.Run(_thread.Join);
        _queue.Dispose();
    }
}
