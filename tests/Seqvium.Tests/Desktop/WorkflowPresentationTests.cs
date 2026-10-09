// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class WorkflowPresentationTests
{
    [Theory]
    [InlineData("Workflow.Opened", "Status.Ready")]
    [InlineData("Workflow.Empty", "Status.Ready")]
    [InlineData("Workflow.Playing", "Status.Playing")]
    [InlineData("Workflow.SaveFailed", "Status.SaveFailed")]
    [InlineData("Workflow.Degraded", "Status.Degraded")]
    [InlineData("Workflow.DeviceFault", "Status.DeviceFault")]
    public void NormalStatusIsCompactAndFailuresRemainPersistent(string actual, string compact)
    {
        var document = ProjectDocument.Create();
        var state = new WorkflowState(document, [], null, null, "", "", actual,
            "Internal provenance belongs in details", null, false, false, false, null);
        Assert.Equal(compact, WorkflowPresentation.CompactStatus(state));
        Assert.DoesNotContain(state.Detail!, new HostLocalizer().Get(compact, HostLanguage.English));
    }

    [Fact]
    public async Task HumanOccurrenceLabelsKeepExactCanonicalIdentityOutOfOrdinaryText()
    {
        var graph = new GraphFixture(twoParts: false);
        await using var workflow = new DesktopWorkflow(graph.Document, mediaDirectory: null);
        var state = await workflow.ObserveAsync();
        var choice = Assert.Single(state.Occurrences);
        Assert.Equal(graph.Attachment.Id, choice.Id); Assert.Equal("Pattern", choice.Label);
        Assert.DoesNotContain(graph.Placement.ToString(), choice.Label);
        Assert.Contains(graph.Placement.ToString(), state.Source);
        Assert.Equal("Source → Gain → Output", state.Chain);
    }

    [Fact]
    public async Task EarlierGuiObservationDoesNotReadLaterMutableHistoryOrDirtyState()
    {
        var graph = new GraphFixture(twoParts: false);
        await using var workflow = new DesktopWorkflow(graph.Document, mediaDirectory: null);
        var earlier = await workflow.ObserveAsync();
        var history = earlier.UndoCount;
        Assert.True(await workflow.SetGainAsync(0.4m));
        var later = await workflow.ObserveAsync();
        Assert.NotEqual(earlier.Snapshot.Revision, later.Snapshot.Revision);
        Assert.Equal(history, earlier.UndoCount); Assert.Equal(history + 1, later.UndoCount);
        Assert.NotEqual(earlier.Gain, later.Gain);
    }

    [Fact]
    public void MusicalReadoutsFollowOwnerSnapshotAndKeepNonDefaultTempoMeter()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create(new(new Tempo(137.125m), new Meter(7, 8)));
        using var shell = new ShellSession(new(directory.File("prefs.json")), new(new(), false), document);
        shell.HasWorkflowOwner = true;
        shell.ObserveDocument(document, document.Current, null, false, false);
        document.Edit("Change settings", edit => edit.SetSettings(new(new Tempo(99m), new Meter(3, 4))));
        Assert.Equal("137.125", shell.Tempo); Assert.Equal("7/8", shell.Meter);
        shell.ObserveDocument(document, document.Current, null, true, false);
        Assert.Equal("99", shell.Tempo); Assert.Equal("3/4", shell.Meter);
        document.Close();
    }
}
