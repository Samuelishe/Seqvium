// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal Task<bool> ExportWaveAsync(BeatEditBasis basis, string path, CancellationToken cancellation = default)
        => Command("Workflow.ExportFailed", async () =>
        {
            var (_, _, attachment, _) = ValidateBeatBasis(basis, requireGrid: false);
            _status = "Workflow.Exporting"; Publish();
            var result = await PatternWaveExport.ExportAsync(_document, attachment.Id, path, cancellation);
            _status = result.ClippedSamples > 0 ? "Workflow.ExportClipped" : "Workflow.Exported";
            return true;
        });
}
