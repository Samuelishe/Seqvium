// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Single-owner canonical document. Callers serialize mutations; preview and execution live elsewhere.</summary>
public sealed class ProjectDocument
{
    private sealed record HistoryEntry(ProjectSnapshot Before, ProjectSnapshot After, string Description);

    private readonly List<HistoryEntry> _undo = [];
    private readonly Stack<HistoryEntry> _redo = new();
    private readonly int _historyLimit;
    private readonly Guid _initialRevision;
    private bool _editing;
    internal string? UnnamedMediaRoot { get; set; }

    internal List<string> MediaRoots { get; } = [];

    // Owner-thread lifetime notification, never called from execution or preparation workers.
    internal event Action? Closed;

    public Guid LifecycleId { get; } = Guid.NewGuid();
    public long Generation { get; private set; }
    public bool IsClosed { get; private set; }
    public ProjectSnapshot Current { get; private set; }
    public ProjectSnapshot? Saved { get; private set; }
    public string? SavedPath { get; private set; }
    public System.Collections.Immutable.ImmutableArray<string> SavedMediaDiagnostics { get; internal set; } = [];
    public bool IsDirty => (Saved?.Revision ?? _initialRevision) != Current.Revision;
    internal ProjectCompatibility Compatibility { get; set; } = new(0, 0);
    public int UndoCount => _undo.Count;
    public int RedoCount => _redo.Count;
    public string? NextUndoDescription => _undo.LastOrDefault()?.Description;

    private ProjectDocument(ProjectSnapshot current, ProjectSnapshot? saved, int historyLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(historyLimit);
        ProjectValidation.Validate(current.State);
        ProjectValidation.Require(current.Revision != Guid.Empty, "Revision cannot be empty.");
        Current = current;
        _initialRevision = current.Revision;
        Saved = saved;
        _historyLimit = historyLimit;
    }

    public static ProjectDocument Create(ProjectSettings? defaults = null, int historyLimit = 256)
    {
        var settings = defaults ?? new ProjectSettings(new Tempo(120m), new Meter(4, 4));
        var state = new ProjectState(Id<ProjectState>.New(), null, settings, [], [], [], [], [], [], []);
        // A pristine unnamed document is unmodified, although it has no file association yet.
        var initial = new ProjectSnapshot(state, Guid.NewGuid());
        return new(initial, null, historyLimit);
    }

    internal static ProjectDocument Reopen(ProjectSnapshot snapshot) => new(snapshot, snapshot, 256);

    public bool Edit(string description, Action<ProjectEdit> operation)
    {
        CheckAvailable();
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(operation);
        var edit = new ProjectEdit(Current.State);
        _editing = true;
        try
        {
            operation(edit);
            var candidate = edit.State;
            ProjectValidation.Require(candidate.Id == Current.State.Id, "An edit cannot replace document identity.");
            ProjectValidation.Validate(candidate);
            if (HasSameContent(candidate, Current.State)) return false;
            var nextGeneration = checked(Generation + 1);
            var next = new ProjectSnapshot(candidate, Guid.NewGuid());
            _undo.Add(new(Current, next, description));
            if (_undo.Count > _historyLimit) _undo.RemoveAt(0);
            _redo.Clear();
            Current = next;
            Generation = nextGeneration;
            return true;
        }
        finally
        {
            edit.Seal();
            _editing = false;
        }
    }

    public bool Undo()
    {
        CheckAvailable();
        if (_undo.Count == 0) return false;
        var nextGeneration = checked(Generation + 1);
        var entry = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Push(entry);
        Current = entry.Before;
        Generation = nextGeneration;
        return true;
    }

    public bool Redo()
    {
        CheckAvailable();
        if (_redo.Count == 0) return false;
        var nextGeneration = checked(Generation + 1);
        var entry = _redo.Pop();
        _undo.Add(entry);
        Current = entry.After;
        Generation = nextGeneration;
        return true;
    }

    /// <summary>Descriptor owners for later media retention. Enumeration is on the document's owning thread.</summary>
    public IEnumerable<ProjectSnapshot> RetainedSnapshots =>
        new[] { Current }.Concat(Saved is { } saved ? [saved] : [])
            .Concat(_undo.SelectMany(entry => new[] { entry.Before, entry.After }))
            .Concat(_redo.SelectMany(entry => new[] { entry.Before, entry.After }))
            .DistinctBy(snapshot => snapshot.Revision);

    public void Close()
    {
        CheckAvailable();
        Generation = checked(Generation + 1);
        IsClosed = true;
        _undo.Clear();
        _redo.Clear();
        Closed?.Invoke();
    }

    internal void MarkSaved(ProjectSnapshot snapshot, string path)
    {
        CheckAvailable();
        ProjectValidation.Require(snapshot.State.Id == Current.State.Id, "Saved snapshot belongs to another document.");
        Saved = snapshot;
        SavedPath = path;
    }

    internal void CheckAvailable()
    {
        if (IsClosed) throw new InvalidOperationException("Document is closed.");
        if (_editing) throw new InvalidOperationException("Reentrant document operations are not supported.");
    }

    // ImmutableArray/Dictionary use reference equality. A composed edit can return to identical
    // canonical values through different collection instances; that must not invent accepted history.
    private static bool HasSameContent(ProjectState left, ProjectState right)
    {
        if (left == right) return true;
        return left with
               {
                   Patterns = right.Patterns, Sounds = right.Sounds, Placements = right.Placements,
                   Groups = right.Groups, Resources = right.Resources, Contexts = right.Contexts, Routes = right.Routes
               } == right
               && SameSequence(left.Patterns, right.Patterns, (a, b) =>
                   a with { Parts = b.Parts } == b && SameSequence(a.Parts, b.Parts, (partA, partB) =>
                       partA with { Notes = partB.Notes } == partB && partA.Notes.SequenceEqual(partB.Notes)))
               && SameSequence(left.Sounds, right.Sounds, (a, b) =>
                   a with { Parameters = b.Parameters, ResourceIds = b.ResourceIds } == b &&
                   a.ResourceIds.SequenceEqual(b.ResourceIds) && a.Parameters.Count == b.Parameters.Count &&
                   a.Parameters.All(pair => b.Parameters.TryGetValue(pair.Key, out var value) && value == pair.Value))
               && SameSequence(left.Placements, right.Placements, (a, b) =>
                   a with { PartRelationships = b.PartRelationships } == b &&
                   a.PartRelationships.SequenceEqual(b.PartRelationships))
               && SameSequence(left.Groups, right.Groups, (a, b) =>
                   a with { SoundIds = b.SoundIds } == b && a.SoundIds.SequenceEqual(b.SoundIds))
               && left.Resources.SequenceEqual(right.Resources) && left.Contexts.SequenceEqual(right.Contexts)
               && left.Routes.SequenceEqual(right.Routes);
    }

    private static bool SameSequence<T>(System.Collections.Immutable.ImmutableArray<T> left,
        System.Collections.Immutable.ImmutableArray<T> right, Func<T, T, bool> equal)
    {
        if (left.Length != right.Length) return false;
        for (int index = 0; index < left.Length; index++)
            if (!equal(left[index], right[index]))
                return false;
        return true;
    }
}
