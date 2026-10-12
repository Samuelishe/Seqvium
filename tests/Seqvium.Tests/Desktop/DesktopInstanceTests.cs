// SPDX-License-Identifier: Apache-2.0
using Seqvium.Desktop.Lifecycle;
using System.IO.Pipes;
using Xunit;

namespace Seqvium.Tests;

public sealed class DesktopInstanceTests
{
    [Fact]
    public async Task InvalidClientAndCancelledRequestDoNotEndPrimaryListener()
    {
        string id = Guid.NewGuid().ToString("N");
        using var first = new Primary(id, _ => Task.CompletedTask); await first.Ready;
        using var secondary = new DesktopInstance(id);
        using (var client = new NamedPipeClientStream(".", secondary.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
        {
            await client.ConnectAsync(TestContext.Current.CancellationToken);
            await client.WriteAsync(new byte[] { 255 }, TestContext.Current.CancellationToken);
            var response = new byte[1];
            Assert.Equal(0, await client.ReadAsync(response, TestContext.Current.CancellationToken));
        }
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken); await cancelled.CancelAsync();
        Assert.False(await secondary.RequestActivationAsync(TimeSpan.FromSeconds(3), cancelled.Token));
        Assert.True(await secondary.RequestActivationAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
    }
    private sealed class Primary : IDisposable
    {
        private readonly ManualResetEventSlim _stop = new();
        private readonly Thread _thread;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Exception? _error;
        internal Task Ready => _ready.Task;
        internal Primary(string identity, Func<CancellationToken, Task>? activation)
        {
            _thread = new(() =>
            {
                try
                {
                    using var instance = new DesktopInstance(identity);
                    if (!instance.IsPrimary) throw new InvalidOperationException("Test identity collision.");
                    if (activation is not null) instance.StartListening(activation);
                    _ready.SetResult(); _stop.Wait();
                }
                catch (Exception error) { _error = error; _ready.TrySetException(error); }
            });
            _thread.IsBackground = true; _thread.Start();
        }
        public void Dispose()
        {
            _stop.Set(); Assert.True(_thread.Join(TimeSpan.FromSeconds(10))); _stop.Dispose();
            Assert.Null(_error);
        }
    }
    [Fact]
    public async Task SecondaryAcknowledgesActivationWithoutAcquiringOwnershipAndShutdownReleasesIt()
    {
        string id = Guid.NewGuid().ToString("N"); int calls = 0;
        using (var first = new Primary(id, _ => { Interlocked.Increment(ref calls); return Task.CompletedTask; }))
        {
            await first.Ready;
            using var second = new DesktopInstance(id); Assert.False(second.IsPrimary);
            Assert.True(await second.RequestActivationAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            Assert.Equal(1, calls);
        }
        // The new lease is acquired and disposed synchronously on its owner thread.
        using var next = new DesktopInstance(id); Assert.True(next.IsPrimary);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MissingOrUnresponsiveListenerTimesOutWithoutSecondWorkspace(bool listen)
    {
        string id = Guid.NewGuid().ToString("N");
        using var first = new Primary(id, listen ? ct => Task.Delay(Timeout.Infinite, ct) : null);
        await first.Ready;
        using var second = new DesktopInstance(id); Assert.False(second.IsPrimary);
        Assert.False(await second.RequestActivationAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken));
        using var third = new DesktopInstance(id); Assert.False(third.IsPrimary);
    }
    [Fact]
    public async Task StartupWindowReadinessIsAwaitedBeforeAcknowledgement()
    {
        string id = Guid.NewGuid().ToString("N");
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var first = new Primary(id, async ct => { received.TrySetResult(); await ready.Task.WaitAsync(ct); });
        await first.Ready;
        using var second = new DesktopInstance(id);
        var activation = second.RequestActivationAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await received.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken); Assert.False(activation.IsCompleted);
        ready.SetResult(); Assert.True(await activation);
    }
}
