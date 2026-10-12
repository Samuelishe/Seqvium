// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace Seqvium.Desktop.Lifecycle;

/// <summary>Process ownership acquired before GUI/document/audio initialization. The mutex lease
/// belongs to the calling entry-point thread; IPC has its own cancellable, joined lifetime.</summary>
internal sealed class DesktopInstance : IDisposable
{
    private const byte ActivateCommand = 1, Acknowledged = 2;
    private readonly Mutex _mutex;
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private readonly CancellationTokenSource _stop = new();
    private Task _listener = Task.CompletedTask;
    private bool _disposed;
    private bool _listening;
    internal string PipeName { get; }
    public bool IsPrimary { get; }
    public string? ListenerFailure { get; private set; }
    public static readonly TimeSpan ActivationTimeout = TimeSpan.FromSeconds(5);

    public DesktopInstance(string identity)
    {
        // User configuration is shared across logon/shell sessions, so ownership is user-wide.
        // CurrentUserOnly plus a user-specific name never serializes different users.
        string userScope = Environment.UserName + "|" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity + "|" + userScope)))[..32];
        PipeName = "seqvium.desktop." + suffix;
        _mutex = new Mutex(false, PipeName, new NamedWaitHandleOptions
        { CurrentUserOnly = true, CurrentSessionOnly = false });
        try { IsPrimary = _mutex.WaitOne(0); }
        catch (AbandonedMutexException) { IsPrimary = true; }
    }

    public void StartListening(Func<CancellationToken, Task> activate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsPrimary || _listening) throw new InvalidOperationException("Only the primary instance can listen once.");
        _listening = true;
        _listener = ListenAsync(activate);
    }

    private async Task ListenAsync(Func<CancellationToken, Task> activate)
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                using var request = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                request.CancelAfter(ActivationTimeout);
                try
                {
                    var command = new byte[1];
                    await server.ReadExactlyAsync(command, request.Token).ConfigureAwait(false);
                    if (command[0] != ActivateCommand) continue; // Protocol v1 admits activation only.
                    await activate(request.Token).WaitAsync(request.Token).ConfigureAwait(false);
                    await server.WriteAsync(new byte[] { Acknowledged }, request.Token).ConfigureAwait(false);
                    await server.FlushAsync(request.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (request.IsCancellationRequested) { }
                catch (IOException) { } // A disconnected/invalid client cannot end the primary's listener.
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { ListenerFailure = "Desktop.Instance.ListenerUnavailable"; Console.Error.WriteLine(ListenerFailure); }
    }

    public async Task<bool> RequestActivationAsync(TimeSpan timeout, CancellationToken cancellation = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(timeout);
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(deadline.Token).ConfigureAwait(false);
            await client.WriteAsync(new byte[] { ActivateCommand }, deadline.Token).ConfigureAwait(false);
            await client.FlushAsync(deadline.Token).ConfigureAwait(false);
            var response = new byte[1];
            await client.ReadExactlyAsync(response, deadline.Token).ConfigureAwait(false);
            return response[0] == Acknowledged;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or OperationCanceledException or TimeoutException)
        { return false; }
    }

    public async Task StopAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        await _listener.ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (IsPrimary && Environment.CurrentManagedThreadId != _ownerThread)
            throw new InvalidOperationException("The entry-point thread must release desktop ownership.");
        // Synchronous joining is confined to the process entry-point/lease teardown, never the GUI.
        StopAsync().GetAwaiter().GetResult();
        if (IsPrimary) _mutex.ReleaseMutex();
        _mutex.Dispose(); _stop.Dispose(); _disposed = true;
    }
}
