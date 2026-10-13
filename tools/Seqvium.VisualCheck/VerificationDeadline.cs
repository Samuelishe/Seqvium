// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.VisualCheck;

// Timer runs independently of the GUI dispatcher, including startup/close/IPC teardown.
internal sealed class VerificationDeadline : IDisposable
{
    private readonly Timer _timer;
    internal VerificationDeadline(TimeSpan deadline, string output, Action expired)
        : this(deadline, () => output, expired) { }

    internal VerificationDeadline(TimeSpan deadline, Func<string> output, Action expired)
    {
        _timer = new(_ =>
        {
            try { File.AppendAllText(Path.Combine(output(), "failure.txt"), $"Watchdog timeout after {deadline.TotalSeconds}s; test host PID {Environment.ProcessId}.\n"); }
            catch (Exception error) { Console.Error.WriteLine("Cannot persist timeout: " + error.Message); }
            finally { expired(); }
        }, null, deadline, Timeout.InfiniteTimeSpan);
    }
    public void Dispose() => _timer.Dispose();
}
