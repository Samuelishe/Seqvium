// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;

namespace Seqvium.Core;

public enum GraphPortDirection { Input, Output }
public enum GraphPortCardinality { Single, FanOut }

public sealed record GraphPosition(double X, double Y) : CanonicalData;
public sealed record GraphPort(Id<GraphPort> Id, string Role, GraphPortDirection Direction,
    string SignalClass, string Use, string Layout, string RatePolicy, GraphPortCardinality Cardinality,
    bool Required) : CanonicalData;
public sealed record GraphNode(Id<GraphNode> Id, string Type, int StateVersion,
    ImmutableDictionary<string, JsonElement> Parameters, ImmutableArray<GraphPort> Ports,
    GraphPosition Position, ExtensionState? Extension) : CanonicalData
{
    public ImmutableDictionary<string, JsonElement> Parameters
    {
        get;
        init => field = value.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.Clone());
    } = Parameters.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.Clone());
}
public sealed record GraphConnection(Id<GraphConnection> Id, Id<GraphNode> FromNodeId,
    Id<GraphPort> FromPortId, Id<GraphNode> ToNodeId, Id<GraphPort> ToPortId) : CanonicalData;
public sealed record GraphDefinition(Id<GraphDefinition> Id, ImmutableArray<GraphNode> Nodes,
    ImmutableArray<GraphConnection> Connections) : CanonicalData;
public sealed record GraphSourceBinding(Id<GraphSourceBinding> Id, Id<GraphNode> NodeId,
    Id<PatternPlacement> PlacementId, Id<MusicalPart> PartId, string Boundary) : CanonicalData;
public sealed record GraphAttachment(Id<GraphAttachment> Id, Id<GraphDefinition> GraphId,
    Id<ProcessingContext> ContextId, Id<GraphNode>? OutputNodeId,
    ImmutableArray<GraphSourceBinding> Sources) : CanonicalData;

public sealed record GraphNodeDescriptor(string Type, int StateVersion, int MinimumInputs, int MaximumInputs,
    int Outputs);

/// <summary>Versioned first-party declarations, not a plugin ABI or executable processor registry.</summary>
public static class GraphBuiltIns
{
    public const string Source = "core.source";
    public const string Gain = "core.gain";
    public const string Mix = "core.mix";
    public const string Output = "core.output";
    public const string Audio = "audio";
    public const string Musical = "musical-event";
    public const string Control = "control-modulation";
    public const string Audible = "audible";
    public const string Detector = "detector";
    public const string Mono = "mono";
    public const string Stereo = "stereo-lr";
    public const string HostRate = "host";
    public const string PreItem = "part.pre-item";
    public const string GainAmplitude = "linearAmplitude";
    public const string GainUnit = "linear-amplitude-ratio";
    public const decimal GainDefault = 1m;
    // This is the attenuation capability of state version 1, not an architectural gain ceiling.
    public const decimal GainMinimum = 0m;
    public const decimal GainMaximum = 1m;

    public static ImmutableArray<GraphNodeDescriptor> Descriptors { get; } =
    [new(Source, 1, 0, 0, 1), new(Gain, 1, 1, 1, 1), new(Mix, 1, 2, 8, 1), new(Output, 1, 1, 1, 0)];

    public static GraphNode Create(string type, GraphPosition position, string layout = Stereo, int mixInputs = 2)
    {
        var descriptor = Descriptors.SingleOrDefault(item => item.Type == type)
            ?? throw new ProjectValidationException("Unknown built-in node type.");
        ProjectValidation.Require(layout is Mono or Stereo, "Supported node layout is mono or stereo-lr.");
        int inputs = type == Mix ? mixInputs : descriptor.MinimumInputs;
        ProjectValidation.Require(inputs >= descriptor.MinimumInputs && inputs <= descriptor.MaximumInputs,
            "Unsupported built-in input count.");
        var ports = ImmutableArray.CreateBuilder<GraphPort>();
        for (int index = 0; index < inputs; index++)
            ports.Add(new(Id<GraphPort>.New(), type == Mix ? "slot" : "in", GraphPortDirection.Input,
                Audio, Audible, layout, HostRate, GraphPortCardinality.Single, true));
        if (descriptor.Outputs == 1)
            ports.Add(new(Id<GraphPort>.New(), "out", GraphPortDirection.Output, Audio, Audible, layout,
                HostRate, GraphPortCardinality.FanOut, false));
        var parameters = ImmutableDictionary<string, JsonElement>.Empty;
        if (type == Gain) parameters = parameters.Add(GainAmplitude, JsonSerializer.SerializeToElement(GainDefault));
        return new(Id<GraphNode>.New(), type, descriptor.StateVersion, parameters, ports.ToImmutable(), position, null);
    }

    public static bool Compatible(GraphPort from, GraphPort to) =>
        from.Direction == GraphPortDirection.Output && to.Direction == GraphPortDirection.Input &&
        SupportedAudio(from) && SupportedAudio(to) && from.Layout == to.Layout;

    internal static bool SupportedAudio(GraphPort port) => port.SignalClass == Audio && port.Use == Audible &&
        port.Layout is Mono or Stereo && port.RatePolicy == HostRate;
}
