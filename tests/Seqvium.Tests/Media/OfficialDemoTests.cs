// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class OfficialDemoTests
{
    [Theory]
    [InlineData("Neon Orchard.seqvium")]
    [InlineData("checkpoints/publication-01.json")]
    public async Task IndependentDemoRetainsAllMediaPatternsMixerAndSongExport(string input)
    {
        string example = Path.Combine(AppContext.BaseDirectory, "Examples", "Neon Orchard");
        using var directory = new TemporaryDirectory();
        string relocated = directory.File("Independent.seqvium");
        File.Copy(Path.Combine(example, input), relocated);
        string mediaRoot = relocated + ".media";
        Directory.CreateDirectory(Path.Combine(mediaRoot, "wav"));
        using var inventory = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(example, "MEDIA.json")));
        var entries = inventory.RootElement.GetProperty("files").EnumerateArray().ToArray();
        foreach (var entry in entries)
        {
            string locator = entry.GetProperty("path").GetString()!;
            byte[] bytes = File.ReadAllBytes(Path.Combine(example, "Neon Orchard.seqvium.media", locator));
            Assert.Equal(entry.GetProperty("sha256").GetString(),
                Convert.ToHexStringLower(SHA256.HashData(bytes)));
            Assert.Equal(entry.GetProperty("bytes").GetInt64(), bytes.LongLength);
            Assert.Contains(entry.GetProperty("license").GetString(), new[] { "Apache-2.0", "CC0-1.0" });
            File.WriteAllBytes(Path.Combine(mediaRoot, locator), bytes);
        }

        var loaded = ProjectPersistence.Open(relocated);
        Assert.False(loaded.IsDegraded);
        Assert.Empty(loaded.Diagnostics);
        var document = loaded.Document;
        try
        {
            var state = document.Current.State;
            Assert.NotNull(state.Mixer);
            Assert.NotEmpty(state.ArrangementClips);
            var locators = state.Resources.Select(r => r.ManagedLocator).Distinct().Order().ToArray();
            foreach (var locator in locators)
                Assert.Contains(entries, e => e.GetProperty("path").GetString() == locator);
            foreach (var resource in state.Resources)
            {
                Assert.StartsWith(mediaRoot + Path.DirectorySeparatorChar, ProjectMedia.Resolve(document, resource));
                using var decoded = ProjectMedia.Decode(document, resource.Id);
                Assert.True(decoded.Frames > 0);
            }

            // Every Rack occurrence must be executable, including patterns not currently audible in Song.
            foreach (var attachment in state.GraphAttachments)
            {
                using var plan = GraphPreparation.Prepare(document, attachment.Id, 48000, 2, 1024,
                    voiceCapacity: GraphPreparation.MaximumVoices,
                    cancellationToken: TestContext.Current.CancellationToken);
                using var execution = plan.CreateExecution(smoothBoundaries: true);
                var packet = new float[2048];
                double energy = 0;
                while (execution.Position < plan.EndFrame)
                {
                    int count = (int)Math.Min(1024, plan.EndFrame - execution.Position);
                    execution.Process(packet.AsSpan(0, count * 2));
                    for (int i = 0; i < count * 2; i++)
                    {
                        float value = packet[i];
                        Assert.True(float.IsFinite(value));
                        energy += value * value;
                    }
                }

                Assert.True(energy > 0, "Silent demo Pattern: " + attachment.Id);
            }

            string saved = directory.File("Saved.seqvium");
            Assert.False(ProjectPersistence.SaveWithReport(document, saved).IsDegraded);
            var reopened = ProjectPersistence.Open(saved);
            try
            {
                Assert.False(reopened.IsDegraded);
                Assert.True(JsonNode.DeepEquals(
                    JsonNode.Parse(ProjectPersistence.Encode(document))!["state"],
                    JsonNode.Parse(ProjectPersistence.Encode(reopened.Document))!["state"]));
                var result = await PatternWaveExport.ExportArrangementAsync(reopened.Document,
                    directory.File("Song.wav"), TestContext.Current.CancellationToken);
                Assert.Equal(0, result.ClippedSamples);
                using var pcm = WavDecoder.Decode(File.ReadAllBytes(directory.File("Song.wav")));
                Assert.Equal(48000, pcm.SampleRate);
                Assert.Equal(2, pcm.Channels);
                Assert.Equal(result.Frames, pcm.Frames);
                double peak = 0;
                for (int frame = 0; frame < pcm.Frames; frame++)
                for (int channel = 0; channel < pcm.Channels; channel++)
                {
                    float value = pcm.Sample(frame, channel);
                    Assert.True(float.IsFinite(value));
                    Assert.True(Math.Abs(value) < .9999);
                    peak = Math.Max(peak, Math.Abs(value));
                }

                Assert.True(peak > .1);
                if (input.StartsWith("checkpoints/", StringComparison.Ordinal))
                {
                    Assert.Equal(15, state.Patterns.Length);
                    Assert.Equal(191, state.Patterns.Sum(p => p.Parts.Sum(part => part.Notes.Length)));
                    Assert.Equal(78, state.ArrangementClips.Length);
                    Assert.Equal(9, state.ArrangementTracks.Length);
                    Assert.Equal(13, state.Mixer!.Channels.Length);
                    Assert.Equal(3291429, result.Frames);
                }
            }
            finally
            {
                reopened.Document.Close();
            }
        }
        finally
        {
            document.Close();
        }
    }
}
