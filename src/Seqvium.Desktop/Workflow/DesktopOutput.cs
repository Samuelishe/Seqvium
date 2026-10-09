// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Audio.Windows;

namespace Seqvium.Desktop.Workflow;

internal sealed record WorkflowOutput(IAudioOutputLifetime Lifetime, RealtimeSampler Sampler,
    string Description, Func<string?> ReadFailure);

internal static class DesktopOutput
{
    // Called on the host control owner, never the UI. Start opens a silent consumer; only coordinator.Start plays.
    internal static WorkflowOutput Open()
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("Audio requires Windows x64 in this delivery.");
        var resolution = WindowsAudioEndpoints.Discover().Resolve(AudioEndpointDirection.Output,
            AudioEndpointIntent.Default());
        if (!resolution.IsAvailable)
            throw new IOException($"Output unavailable: {resolution.Availability} (0x{resolution.ErrorCode:X8}).");
        var output = new WasapiOutput(AudioEndpointIntent.Explicit(resolution.Endpoint!.Id));
        RealtimeSampler? sampler = null;
        try
        {
            if (output.Failure != OutputFailure.None || output.Facts is not { } facts)
                throw new IOException($"Output open failed: {output.Failure} (0x{output.HResult:X8}).");
            sampler = new(facts.Rate, facts.Channels, facts.CapacityFrames);
            output.Start(sampler);
            return new(output, sampler,
                $"{resolution.Endpoint.DisplayName ?? resolution.Endpoint.Id} · {facts.Rate} Hz · {facts.Channels} ch",
                () => !OperatingSystem.IsWindows() || output.Failure == OutputFailure.None ? null :
                    $"{output.Failure} (0x{output.HResult:X8})");
        }
        catch
        {
            output.Close(); // If join throws, no borrowed PCM is released.
            sampler?.TerminateExecution();
            sampler?.Dispose();
            output.Dispose();
            throw;
        }
    }
}
