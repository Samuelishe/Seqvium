// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphMenuTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly GraphFixture Graph = new(false);
        internal readonly DesktopWorkflow Workflow;
        internal readonly GraphEditorSession Editor = new();
        internal readonly ShellSession Shell;
        internal readonly GraphMenuSession MenuSession;
        internal Fixture(bool output = true, HostLanguage language = HostLanguage.English)
        {
            if (!output) Graph.Document.Edit("Incomplete graph", e => e.DeleteGraphNode(Graph.Graph.Id, Graph.Output.Id));
            Workflow = new(Graph.Document, () => throw new InvalidOperationException("No physical output in menu tests"));
            Editor.Open(Workflow.State, Graph.Attachment.Id);
            Shell = new(new(Directory.File("prefs.json")), new(new(language, HostTheme.Dark), false), Graph.Document)
                { HasWorkflowOwner = true };
            MenuSession = new(Editor, Workflow);
        }
        internal void Begin() => Assert.Null(MenuSession.Begin());
        public async ValueTask DisposeAsync()
        { Shell.Dispose(); await Workflow.DisposeAsync(); Directory.Dispose(); }
    }

    public static IEnumerable<object[]> CreationChoices()
    {
        foreach (int type in Enumerable.Range(0, 4))
            foreach (int layout in Enumerable.Range(0, 2))
                foreach (int inputs in type == 2 ? Enumerable.Range(2, 7) : [2])
                    yield return [type, layout, inputs];
    }

    [Theory]
    [MemberData(nameof(CreationChoices))]
    public async Task CapturedCreationChoiceCreatesExactNodeInIncompleteGraphAndUndoRestores(int typeIndex, int layoutIndex, int inputs)
    {
        await using var f = new Fixture(output: false);
        Assert.NotEmpty(f.Editor.Report!.Diagnostics);
        var before = f.Workflow.State.Snapshot; int history = f.Workflow.State.UndoCount;
        f.Begin();
        Assert.Null(f.Editor.EditRefusal(f.Workflow.State));
        Assert.Same(before, f.Workflow.State.Snapshot); Assert.Equal(history, f.Workflow.State.UndoCount);
        string requestedType = new[] { GraphBuiltIns.Source, GraphBuiltIns.Gain, GraphBuiltIns.Mix, GraphBuiltIns.Output }[typeIndex];
        var choice = GraphNodeCreation.Choices.Single(c => c.Type == requestedType &&
            c.Layout == (layoutIndex == 0 ? GraphBuiltIns.Stereo : GraphBuiltIns.Mono) && c.Inputs == inputs);
        Assert.Null(choice.Refusal(f.Graph.Graph));
        Assert.Null(await f.MenuSession.CreateNode(choice, new(80, 90))());
        var added = Assert.Single(f.Graph.Graph.Nodes.Where(n => !before.State.Graphs.Single().Nodes.Any(old => old.Id == n.Id)));
        string type = new[] { GraphBuiltIns.Source, GraphBuiltIns.Gain, GraphBuiltIns.Mix, GraphBuiltIns.Output }[typeIndex];
        Assert.Equal(type, added.Type); Assert.Equal(new GraphPosition(80, 90), added.Position);
        Assert.All(added.Ports, p => Assert.Equal(layoutIndex == 0 ? GraphBuiltIns.Stereo : GraphBuiltIns.Mono, p.Layout));
        if (typeIndex == 2) Assert.Equal(inputs, added.Ports.Count(p => p.Direction == GraphPortDirection.Input));
        if (typeIndex == 0) Assert.DoesNotContain(f.Graph.CurrentAttachment.Sources, s => s.NodeId == added.Id);
        if (typeIndex == 3) Assert.Equal(added.Id, f.Graph.CurrentAttachment.OutputNodeId);
        Assert.Equal(history + 1, f.Workflow.State.UndoCount);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Workflow.State.Snapshot);
    }

    [Theory]
    [InlineData(HostLanguage.English)]
    [InlineData(HostLanguage.Russian)]
    public async Task SecondOutputExplainsRefusalWithoutDisablingSourceGainOrMix(HostLanguage language)
    {
        await using var f = new Fixture(language: language); f.Begin();
        foreach (var choice in GraphNodeCreation.Choices)
            Assert.Equal(choice.Type == GraphBuiltIns.Output ? "Graph.OneOutput" : null, choice.Refusal(f.Graph.Graph));
        Assert.NotEqual("Text unavailable", f.Shell["Graph.OneOutput"]);
        Assert.NotEqual("Текст недоступен", f.Shell["Graph.OneOutput"]);
        var before = f.Workflow.State.Snapshot; int history = f.Workflow.State.UndoCount;
        Assert.False(await f.Workflow.AddGraphNodeAsync(f.Editor.Basis!, GraphBuiltIns.Output, new(0, 0)));
        Assert.Equal("Graph.OneOutput", f.Workflow.State.Detail);
        Assert.Same(before, f.Workflow.State.Snapshot); Assert.Equal(history, f.Workflow.State.UndoCount);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("generation")]
    [InlineData("target")]
    [InlineData("replacement")]
    [InlineData("closed")]
    public async Task RetainedLeafCannotActAfterCancellationOrBasisChange(string change)
    {
        await using var f = new Fixture();
        Id<GraphAttachment> other = default;
        if (change == "target")
            Assert.True(await f.Workflow.EditGraphAsync(f.Editor.Basis!, "Other target", (e, _, _) =>
                other = e.CreateItemGraph(f.Graph.OtherPlacement).Id));
        f.Begin();
        var leaf = f.MenuSession.CreateNode(new(GraphBuiltIns.Gain, GraphBuiltIns.Stereo), new(80, 90));
        switch (change)
        {
            case "cancel": f.MenuSession.Cancel(); break;
            case "generation":
                Assert.True(await f.Workflow.SetGraphGainAsync(f.Editor.Basis!, f.Graph.Gain.Id, 0.4m)); break;
            case "target":
                f.Editor.Open(f.Workflow.State, other); break;
            case "replacement":
                string path = f.Directory.File("replacement.seqvium");
                await File.WriteAllBytesAsync(path, ProjectPersistence.Encode(f.Graph.Document), TestContext.Current.CancellationToken);
                Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard)); break;
            case "closed": await f.Workflow.DisposeAsync(); break;
        }
        var before = f.Workflow.State.Snapshot; int history = f.Workflow.State.UndoCount;
        Assert.Equal(change == "closed" ? "Graph.Closed" : "Graph.Stale", await leaf());
        Assert.Same(before, f.Workflow.State.Snapshot); Assert.Equal(history, f.Workflow.State.UndoCount);
    }

    [Fact]
    public async Task EditingAvailabilityUsesTargetAndBusyClosedStateRatherThanPlaybackEligibility()
    {
        await using var f = new Fixture(output: false);
        Assert.Null(f.Editor.EditRefusal(f.Workflow.State));
        Assert.Equal("Workflow.Busy", f.Editor.EditRefusal(f.Workflow.State with { Busy = true }));
        var editor = new GraphEditorSession(); editor.Observe(f.Workflow.State);
        Assert.Equal("Graph.NoTarget", editor.EditRefusal(f.Workflow.State));
        await f.Workflow.DisposeAsync();
        Assert.Equal("Graph.Closed", f.Editor.EditRefusal(f.Workflow.State));
    }

    [Fact]
    public async Task ProcessingLimitRefusesEveryCreationChoiceAndPreservesHistory()
    {
        await using var f = new Fixture();
        Assert.True(await f.Workflow.EditGraphAsync(f.Editor.Basis!, "Node limit", (edit, _, graph) =>
        {
            for (int i = graph.Nodes.Length; i < GraphDiagnostics.MaximumNodes; i++)
                edit.AddGraphNode(graph.Id, GraphBuiltIns.Gain, new(i, 0));
        }));
        f.Begin(); var before = f.Workflow.State.Snapshot; int history = f.Workflow.State.UndoCount;
        foreach (var choice in GraphNodeCreation.Choices) Assert.Equal(GraphReasons.ProcessingLimit, choice.Refusal(f.Graph.Graph));
        var action = f.MenuSession.CreateNode(new(GraphBuiltIns.Gain, GraphBuiltIns.Mono), default);
        Assert.Equal(GraphReasons.ProcessingLimit, await action());
        Assert.Same(before, f.Workflow.State.Snapshot); Assert.Equal(history, f.Workflow.State.UndoCount);
    }

}
