// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Captured menu actions expire on dismissal, target changes or document edits.</summary>
internal sealed class GraphMenuSession(GraphEditorSession editor, DesktopWorkflow workflow)
{
    private long _epoch;
    private GraphEditBasis? _basis;
    public string? Begin()
    {
        Cancel();
        editor.Observe(workflow.State);
        var reason = editor.EditRefusal(workflow.State);
        if (reason is null) _basis = editor.Basis;
        return reason;
    }
    public void Cancel() { _epoch++; _basis = null; }
    public Func<Task<string?>> Capture(Func<Task> action)
    {
        long epoch = _epoch; var basis = _basis;
        return async () =>
        {
            var refusal = epoch != _epoch || basis is null ? "Graph.Stale" : editor.EditRefusal(workflow.State, basis);
            if (refusal is not null) return refusal;
            await action();
            // Invalid canonical intent is a playback blocker, not a refused document edit.
            return WorkflowPresentation.IsError(workflow.State) ? workflow.State.Detail : null;
        };
    }
    public Func<Task<string?>> CaptureView(Func<Task> action)
    {
        long epoch = _epoch; var basis = _basis;
        return async () =>
        {
            var refusal = epoch != _epoch || basis is null ? "Graph.Stale" : editor.EditRefusal(workflow.State, basis);
            if (refusal is not null) return refusal;
            await action();
            // Opening a view is not a workflow command; an earlier Save/Open failure is unrelated.
            return null;
        };
    }
    public Func<Task<string?>> CreateNode(GraphNodeCreation choice, GraphPoint point, Action? beforeExecute = null)
    {
        var basis = _basis;
        return Capture(async () =>
        {
            beforeExecute?.Invoke();
            await workflow.AddGraphNodeAsync(basis!, choice.Type, new(point.X, point.Y), choice.Layout, choice.Inputs);
        });
    }
}

internal sealed record GraphNodeCreation(string Type, string Layout, int Inputs = 2)
{
    public static IEnumerable<GraphNodeCreation> Choices =>
        from type in new[] { GraphBuiltIns.Source, GraphBuiltIns.Gain, GraphBuiltIns.Mix, GraphBuiltIns.Output }
        from layout in new[] { GraphBuiltIns.Stereo, GraphBuiltIns.Mono }
        from inputs in type == GraphBuiltIns.Mix ? Enumerable.Range(2, 7) : [2]
        select new GraphNodeCreation(type, layout, inputs);
    public string? Refusal(GraphDefinition graph) => GraphCreationRules.Refusal(graph, Type);
}
