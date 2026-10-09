// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;

namespace Seqvium.Core;

internal static class GraphContent
{
    public static bool Same(ImmutableArray<GraphDefinition> left, ImmutableArray<GraphDefinition> right) =>
        Sequence(left, right, (a, b) => Scalar(a with { Nodes = b.Nodes, Connections = b.Connections }, b) &&
            Sequence(a.Nodes, b.Nodes, Node) && Sequence(a.Connections, b.Connections, Scalar));

    public static bool Same(ImmutableArray<GraphAttachment> left, ImmutableArray<GraphAttachment> right) =>
        Sequence(left, right, (a, b) => Scalar(a with { Sources = b.Sources }, b) && Sequence(a.Sources, b.Sources, Scalar));

    private static bool Node(GraphNode a, GraphNode b) =>
        Scalar(a with { Parameters = ImmutableDictionary<string, JsonElement>.Empty, Ports = b.Ports, Position = b.Position, Extension = b.Extension },
            b with { Parameters = ImmutableDictionary<string, JsonElement>.Empty }) &&
        Map(a.Parameters, b.Parameters) && Sequence(a.Ports, b.Ports, Scalar) && Scalar(a.Position, b.Position) &&
        Extension(a.Extension, b.Extension);

    private static bool Extension(ExtensionState? a, ExtensionState? b) => a is null || b is null ? a == b :
        a.ExtensionId == b.ExtensionId && a.StateVersion == b.StateVersion &&
        Map(a.AdditionalData, b.AdditionalData) && JsonElement.DeepEquals(a.Payload, b.Payload);

    private static bool Scalar<T>(T a, T b) where T : CanonicalData =>
        EqualityComparer<CanonicalData>.Default.Equals(a with { AdditionalData = ImmutableDictionary<string, JsonElement>.Empty },
            b with { AdditionalData = ImmutableDictionary<string, JsonElement>.Empty }) &&
        Map(a.AdditionalData, b.AdditionalData);

    private static bool Map(ImmutableDictionary<string, JsonElement> a, ImmutableDictionary<string, JsonElement> b) =>
        a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var value) && JsonElement.DeepEquals(pair.Value, value));

    private static bool Sequence<T>(ImmutableArray<T> a, ImmutableArray<T> b, Func<T, T, bool> equal) =>
        a.Length == b.Length && a.Where((item, index) => !equal(item, b[index])).Any() == false;
}
