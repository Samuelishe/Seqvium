// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class AudioEndpointTests
{
    private static readonly AudioEndpoint Output = new("out-a", "Duplicate name", AudioEndpointDirection.Output,
        AudioEndpointState.Active);

    private static readonly AudioEndpoint Other = new("out-b", "Duplicate name", AudioEndpointDirection.Output,
        AudioEndpointState.Active);

    private static readonly AudioEndpoint Input = new("in-a", "Microphone", AudioEndpointDirection.Input,
        AudioEndpointState.Active);

    private static AudioEndpointSnapshot Snapshot(params AudioEndpoint[] endpoints) => new(endpoints,
    [
        new(AudioEndpointDirection.Output, AudioEndpointRole.Console, Output.Id),
        new(AudioEndpointDirection.Output, AudioEndpointRole.Multimedia, Other.Id),
        new(AudioEndpointDirection.Input, AudioEndpointRole.Console, Input.Id)
    ]);

    [Fact]
    public void SelectionsAreIndependentAndNeverEditCanonicalStateOrUndo()
    {
        var document = ProjectDocument.Create();
        document.Edit("Retained work", edit => edit.RenameDocument("Musical intent"));
        var initial = document.Current;
        byte[] before = ProjectPersistence.Encode(document);
        long generation = document.Generation;
        using var owner = new AudioDeviceSession();
        owner.SelectInput(AudioEndpointIntent.Explicit(Input.Id));
        Assert.True(owner.OutputIntent.FollowsDefault);
        owner.SelectOutput(AudioEndpointIntent.Explicit(Other.Id));
        Assert.Equal(Input.Id, owner.InputIntent.EndpointId);
        owner.SelectInput(AudioEndpointIntent.Default(AudioEndpointRole.Communications));
        Assert.Equal(Other.Id, owner.OutputIntent.EndpointId);
        owner.SelectOutput(AudioEndpointIntent.Explicit("missing"));
        Assert.Equal(AudioEndpointAvailability.Missing,
            owner.ResolveOutput(() => Snapshot(Output, Input)).Availability);
        Assert.Same(initial, document.Current);
        Assert.Equal(before, ProjectPersistence.Encode(document));
        Assert.Equal(generation, document.Generation);
        Assert.Equal(1, document.UndoCount);
        Assert.Equal(0, document.RedoCount);
        Assert.Null(document.Saved);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void DuplicateNamesArePresentationAndDefaultRoleIsDistinctFromFixedIdentity()
    {
        var snapshot = Snapshot(Output, Other, Input);
        var following = AudioEndpointIntent.Default(AudioEndpointRole.Multimedia);
        var fixedIntent = AudioEndpointIntent.Explicit(Other.Id);
        Assert.NotEqual(following, fixedIntent);
        Assert.Equal(Other, snapshot.Resolve(AudioEndpointDirection.Output, following).Endpoint);
        Assert.Equal(Output,
            snapshot.Resolve(AudioEndpointDirection.Output, AudioEndpointIntent.Explicit(Output.Id)).Endpoint);
        var changed = new AudioEndpointSnapshot([Output, Other],
            [new(AudioEndpointDirection.Output, AudioEndpointRole.Multimedia, Output.Id)]);
        Assert.Equal(Output, changed.Resolve(AudioEndpointDirection.Output, following).Endpoint);
        Assert.Equal(Other, changed.Resolve(AudioEndpointDirection.Output, fixedIntent).Endpoint);
        Assert.Null(following.EndpointId);
    }

    [Theory]
    [InlineData(AudioEndpointState.Disabled)]
    [InlineData(AudioEndpointState.NotPresent)]
    [InlineData(AudioEndpointState.Unplugged)]
    [InlineData(AudioEndpointState.Unknown)]
    [InlineData(AudioEndpointState.Active | AudioEndpointState.Disabled)]
    public void ExplicitUnavailableNeverFallsBackToActiveDefault(AudioEndpointState state)
    {
        var unavailable = Other with { State = state };
        var snapshot = Snapshot(Output, unavailable, Input);
        var intent = AudioEndpointIntent.Explicit(Other.Id);
        var result = snapshot.Resolve(AudioEndpointDirection.Output, intent);
        Assert.Equal(AudioEndpointAvailability.Unavailable, result.Availability);
        Assert.Equal(unavailable, result.Endpoint);
        Assert.Same(intent, result.Intent);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public void MissingAndWrongDirectionAreExplicitRefusals()
    {
        var snapshot = Snapshot(Output, Input);
        var result = snapshot.Resolve(AudioEndpointDirection.Output, AudioEndpointIntent.Explicit("missing"));
        Assert.Equal(AudioEndpointAvailability.Missing, result.Availability);
        Assert.Null(result.Endpoint);
        Assert.Equal(AudioEndpointAvailability.WrongDirection,
            snapshot.Resolve(AudioEndpointDirection.Output, AudioEndpointIntent.Explicit(Input.Id)).Availability);
        Assert.Equal(AudioEndpointAvailability.WrongDirection,
            snapshot.Resolve(AudioEndpointDirection.Input, AudioEndpointIntent.Explicit(Output.Id)).Availability);
    }

    [Theory]
    [InlineData("no-role")]
    [InlineData("no-id")]
    [InlineData("gone")]
    [InlineData("disabled")]
    public void DefaultDisappearanceIsObservableWithoutChangingIntent(string kind)
    {
        using var owner = new AudioDeviceSession();
        var original = owner.OutputIntent;
        var defaults = kind == "no-role"
            ? Array.Empty<AudioEndpointDefault>()
            :
            [
                new AudioEndpointDefault(AudioEndpointDirection.Output, AudioEndpointRole.Console,
                    kind == "no-id" ? null : Output.Id, kind == "no-id" ? 17 : 0)
            ];
        var snapshot = new AudioEndpointSnapshot(
            kind == "disabled" ? [Output with { State = AudioEndpointState.Disabled }, Other] : [Other], defaults);
        var result = owner.ResolveOutput(() => snapshot);
        Assert.Equal(AudioEndpointAvailability.DefaultUnavailable, result.Availability);
        Assert.Same(original, owner.OutputIntent);
        Assert.True(result.Intent.FollowsDefault);
    }

    [Fact]
    public void DefaultQueryFailureIsNotReportedAsSuccessfulDiscovery()
    {
        var snapshot = new AudioEndpointSnapshot([Output],
            [new(AudioEndpointDirection.Output, AudioEndpointRole.Console, null, 42, QueryFailed: true)]);
        var result = snapshot.Resolve(AudioEndpointDirection.Output, AudioEndpointIntent.Default());
        Assert.Equal(AudioEndpointAvailability.DiscoveryFailed, result.Availability);
        Assert.Equal(42, result.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("id\0suffix")]
    public void InvalidIdentityCannotEnterSelection(string id) =>
        Assert.Throws<ArgumentException>(() => AudioEndpointIntent.Explicit(id));

    [Fact]
    public void InvalidRolesAndDuplicateSnapshotIdentitiesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => AudioEndpointIntent.Explicit(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => AudioEndpointIntent.Default((AudioEndpointRole)99));
        Assert.Throws<ArgumentException>(() => Snapshot(Output, Output));
        Assert.Throws<ArgumentException>(() => new AudioEndpointSnapshot([Output],
        [
            new(AudioEndpointDirection.Output, AudioEndpointRole.Console, Output.Id),
            new(AudioEndpointDirection.Output, AudioEndpointRole.Console, Other.Id)
        ]));
        using var owner = new AudioDeviceSession();
        Assert.Throws<ArgumentNullException>(() => owner.SelectOutput(null!));
        Assert.True(owner.OutputIntent.FollowsDefault);
    }

    [Fact]
    public void SnapshotIsFrozenAndUseResolvesFreshAvailabilityOutsideCallback()
    {
        var endpoints = new List<AudioEndpoint> { Output, Input };
        var snapshot = new AudioEndpointSnapshot(endpoints,
            [new(AudioEndpointDirection.Output, AudioEndpointRole.Console, Output.Id)]);
        endpoints.Clear();
        Assert.Equal(2, snapshot.Endpoints.Length);
        using var owner = new AudioDeviceSession();
        int discoveries = 0;

        AudioEndpointSnapshot Discover()
        {
            discoveries++;
            return snapshot;
        }

        Assert.True(owner.ResolveOutput(Discover).IsAvailable);
        var sampler = new RealtimeSampler(48000, 2, 16);
        owner.AttachOutput(new JoinBoundary(sampler), sampler);
        float[] buffer = new float[32];
        for (int i = 0; i < 100; i++) Assert.True(sampler.Process(buffer));
        Assert.Equal(1, discoveries);
        snapshot = Snapshot(Input);
        Assert.Equal(AudioEndpointAvailability.DefaultUnavailable, owner.ResolveOutput(Discover).Availability);
        Assert.Equal(2, discoveries);
        Assert.True(owner.ResolveInput(Snapshot(Output, Input)).IsAvailable);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeJoinsBeforeReleasingPreparedStateAndPreservesRealStopAcknowledgment(bool processStop)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var media = await WavFixtures.Import(document, directory, Enumerable.Repeat(0.1f, 1000).ToArray());
        Id<Pattern> pattern = default;
        document.Edit("Prepared music", edit =>
        {
            pattern = edit.AddPattern("Pattern", new(960000));
            var part = edit.AddPart(pattern, "Part", media.SoundId);
            edit.AddNote(pattern, part, new(0), new(960000), 60, 1);
        });
        byte[] before = ProjectPersistence.Encode(document);
        using var owner = new AudioDeviceSession();
        var sampler = new RealtimeSampler(48000, 2, 16);
        var boundary = new JoinBoundary(sampler) { ProcessStop = processStop };
        owner.AttachOutput(boundary, sampler);
        using (var preparation = sampler.BeginPreparation(document, pattern))
        {
            await preparation.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, preparation.Publish(TestContext.Current.CancellationToken));
        }

        sampler.Start();
        sampler.Process(new float[2]);
        Assert.Equal(1, sampler.ActiveVoices);
        long stop = sampler.Stop();
        if (processStop) sampler.Process(new float[2]);
        long acknowledgment = sampler.StopAcknowledgment;
        owner.SelectInput(AudioEndpointIntent.Explicit(Input.Id));
        Assert.Equal(0, boundary.Closes);
        owner.SelectOutput(AudioEndpointIntent.Explicit(Other.Id));
        Assert.True(boundary.Joined);
        Assert.True(boundary.Disposed);
        Assert.Equal(0, sampler.LivePreparedStates);
        Assert.Equal(1, sampler.TerminationBoundaries);
        Assert.True(processStop ? sampler.StopAcknowledgment >= stop : sampler.StopAcknowledgment == acknowledgment);
        Assert.False(sampler.Process(new float[2]));
        Assert.Null(owner.OutputSampler);
        Assert.Equal(before, ProjectPersistence.Encode(document));
        var replacement = new RealtimeSampler(44100, 1, 32);
        var fresh = new JoinBoundary(replacement);
        owner.AttachOutput(fresh, replacement);
        Assert.False(replacement.IsTerminated);
        Assert.Equal(Input.Id, owner.InputIntent.EndpointId);
        owner.CloseOutput();
        Assert.True(fresh.Joined);
        Assert.True(fresh.Disposed);
    }

    [Fact]
    public void FailedJoinRetainsIntentAndResourcesForRetry()
    {
        using var owner = new AudioDeviceSession();
        var sampler = new RealtimeSampler(48000, 2, 16);
        var boundary = new JoinBoundary(sampler) { FailJoin = true };
        owner.AttachOutput(boundary, sampler);
        var initial = owner.OutputIntent;
        Assert.Throws<TimeoutException>(() => owner.SelectOutput(AudioEndpointIntent.Explicit(Other.Id)));
        Assert.Same(initial, owner.OutputIntent);
        Assert.Same(sampler, owner.OutputSampler);
        Assert.False(boundary.Disposed);
        Assert.False(sampler.IsTerminated);
        Assert.True(sampler.Process(new float[2]));
        boundary.FailJoin = false;
        owner.SelectOutput(AudioEndpointIntent.Explicit(Other.Id));
        Assert.True(boundary.Disposed);
    }

    [Fact]
    public void InputFailureAndSameOutputIntentDoNotCloseWorkingOutput()
    {
        using var owner = new AudioDeviceSession();
        var sampler = new RealtimeSampler(48000, 2, 16);
        var boundary = new JoinBoundary(sampler);
        owner.AttachOutput(boundary, sampler);
        owner.SelectOutput(AudioEndpointIntent.Default());
        owner.SelectInput(AudioEndpointIntent.Explicit(Input.Id));
        var result = owner.ResolveInput(Snapshot(Output, Input with { State = AudioEndpointState.Disabled }));
        Assert.Equal(AudioEndpointAvailability.Unavailable, result.Availability);
        Assert.True(owner.ResolveOutput(() => Snapshot(Output)).IsAvailable);
        Assert.Same(sampler, owner.OutputSampler);
        Assert.Equal(0, boundary.Closes);
        Assert.False(boundary.Disposed);
        Assert.True(sampler.Process(new float[2]));
        owner.SelectInput(AudioEndpointIntent.Default());
        Assert.Equal(AudioEndpointAvailability.DefaultUnavailable, owner.ResolveInput(Snapshot(Output)).Availability);
        Assert.Equal(0, boundary.Closes);
    }

    [Fact]
    public void UnconfirmedJoinCannotReleaseSamplerEvenIfCloseReturnsNormally()
    {
        using var owner = new AudioDeviceSession();
        var sampler = new RealtimeSampler(48000, 2, 16);
        var boundary = new JoinBoundary(sampler) { ReturnUnjoined = true };
        owner.AttachOutput(boundary, sampler);
        Assert.Throws<InvalidOperationException>(() => owner.CloseOutput());
        Assert.False(boundary.Disposed);
        Assert.Same(sampler, owner.OutputSampler);
        Assert.False(sampler.IsTerminated);
        boundary.ReturnUnjoined = false;
        owner.CloseOutput();
        owner.Dispose();
        Assert.Throws<ObjectDisposedException>(() => owner.SelectInput(AudioEndpointIntent.Default()));
    }

    // Simulates only the borrowing/join boundary, not WASAPI, formats, clocks, devices or a processing backend.
    private sealed class JoinBoundary(RealtimeSampler sampler) : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        internal bool Disposed, FailJoin, ProcessStop, ReturnUnjoined;
        internal int Closes;

        public void Close()
        {
            Closes++;
            Assert.False(Disposed);
            if (FailJoin) throw new TimeoutException("Deliberately unjoined consumer.");
            if (ReturnUnjoined) return;
            if (ProcessStop) Assert.True(sampler.Process(new float[2]));
            sampler.TerminateExecution();
            Joined = true;
        }

        public void Dispose()
        {
            Assert.True(Joined);
            Assert.True(sampler.IsTerminated);
            Disposed = true;
        }
    }
}
