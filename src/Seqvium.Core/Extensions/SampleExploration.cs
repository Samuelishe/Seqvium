// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

public sealed record SampleControl(string Id, decimal Minimum, decimal Maximum, decimal Default, string Unit);
public sealed record SampleFamily(string Id, ImmutableArray<SampleControl> Controls);
public enum SampleExplorationKind { Random, Similar }
public sealed record SampleRecipe(string Family, int Seed, ImmutableDictionary<string, decimal> Values,
    Guid? Reference, ImmutableDictionary<string, decimal> Locks);

/// <summary>Numeric recipes are family-local, version-local generator inputs, never arbitrary-audio similarity.</summary>
public sealed record SampleExploration(string Family, SampleExplorationKind Kind, int Seed,
    SampleRecipe? Reference, Guid? ReferenceId, ImmutableDictionary<string, decimal> Locks)
{
    internal static void ValidateFamilies(ImmutableArray<SampleFamily> families)
    {
        if (families.IsDefault || families.Length > 8 || families.Select(f => f.Id).Distinct().Count() != families.Length ||
            families.Any(f => !ExtensionDiscovery.Label(f.Id) || f.Controls.IsDefaultOrEmpty || f.Controls.Length > 16 ||
                f.Controls.Select(c => c.Id).Distinct().Count() != f.Controls.Length || f.Controls.Any(c =>
                    !ExtensionDiscovery.Label(c.Id) || c.Minimum >= c.Maximum || c.Default < c.Minimum || c.Default > c.Maximum ||
                    c.Unit.Length > 16))) throw new InvalidDataException("Extension.InvalidParameters");
    }

    public void Validate(ImmutableArray<SampleFamily> families)
    {
        var family = families.SingleOrDefault(f => f.Id == Family) ?? throw new ArgumentException("Lab.FamilyUnavailable");
        if (!Enum.IsDefined(Kind) || Locks.Count > family.Controls.Length || Locks.Any(p =>
                !family.Controls.Any(c => c.Id == p.Key && p.Value >= c.Minimum && p.Value <= c.Maximum)))
            throw new ArgumentException("Extension.InvalidParameters");
        if (Kind == SampleExplorationKind.Similar && (Reference is null || ReferenceId is null || Reference.Family != Family ||
            Reference.Values.Count != family.Controls.Length || family.Controls.Any(c =>
                !Reference.Values.TryGetValue(c.Id, out var v) || v < c.Minimum || v > c.Maximum)))
            throw new ArgumentException("Lab.ReferenceRequired");
    }
}

public interface IExploringSampleGenerator : ISampleGenerator
{
    ImmutableArray<SampleFamily> Families { get; }
}

/// <summary>Host-owned disposable audio, separate from the provider, document history and accepted resources.</summary>
public sealed class SampleCandidateHistory : IDisposable
{
    public const int Capacity = 12;
    private readonly List<SampleCandidate> _items = [];
    public ImmutableArray<SampleCandidate> Items => [.. _items];
    public Guid? Selected { get; private set; }
    public Guid? Reference { get; private set; }
    public string? Family { get; private set; }
    public ImmutableDictionary<string, decimal> Locks { get; private set; } = ImmutableDictionary<string, decimal>.Empty;
    public SampleCandidate? Selection => _items.SingleOrDefault(c => c.Id == Selected);
    public SampleCandidate? ReferenceCandidate => _items.SingleOrDefault(c => c.Id == Reference);
    public void SetFamily(string family)
    {
        if (Family == family) return;
        Family = family; Reference = null; Locks = Locks.Clear();
    }
    public void Select(Guid id)
    {
        if (!_items.Any(c => c.Id == id && c.IsAvailable)) throw new InvalidOperationException("Extension.StaleResult");
        Selected = id;
    }
    public void SetReference(Guid id)
    {
        var candidate = _items.Single(c => c.Id == id && c.IsAvailable);
        if (candidate.Recipe is not { } recipe || recipe.Family != Family) throw new InvalidOperationException("Lab.ReferenceRequired");
        Reference = id;
    }
    public void SetLock(string control, decimal? value)
    {
        Locks = value is { } held ? Locks.SetItem(control, held) : Locks.Remove(control);
    }
    public void Add(SampleCandidate candidate, bool select)
    {
        if (!candidate.IsAvailable || _items.Any(c => c.Id == candidate.Id)) throw new InvalidOperationException("Extension.StaleResult");
        _items.Add(candidate);
        if (select) Selected = candidate.Id;
        while (_items.Count > Capacity)
        {
            var victim = _items.First(c => c.Id != Selected && c.Id != Reference);
            _items.Remove(victim); victim.Dispose();
        }
    }
    public void Dispose()
    {
        foreach (var candidate in _items) candidate.Dispose();
        _items.Clear(); Selected = Reference = null; Locks = Locks.Clear();
    }
}
