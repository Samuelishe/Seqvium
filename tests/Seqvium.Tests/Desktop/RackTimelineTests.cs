// SPDX-License-Identifier: Apache-2.0

using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class RackTimelineTests
{
    [Theory]
    [InlineData(1, 462)]
    [InlineData(2, 942)]
    [InlineData(4, 1902)]
    [InlineData(8, 3822)]
    [InlineData(32, 15342)]
    public void WholeBarsKeepPadBeatAndBarSpacesWithoutTrailingGap(int bars, double width)
    {
        var timeline = new RackTimelineGeometry(bars * 16, 24, 4, 10, 8);
        Assert.Equal(width, timeline.Width);
        Assert.Equal(0, timeline.X(0));
        Assert.Equal(width, timeline.X(bars * 3840000L));
        Assert.Equal(118, timeline.X(960000));
        Assert.Equal(236, timeline.X(1920000));
        Assert.Equal(354, timeline.X(2880000));
        if (bars > 1) Assert.Equal(480, timeline.X(3840000));
        Assert.Equal(width - 24, timeline.X(bars * 3840000L - 240000));
    }

    [Fact]
    public void CrossBarAndFractionalNotesIncludeTheSameRhythmicGapsAsPads()
    {
        var timeline = new RackTimelineGeometry(32, 24, 4, 10, 8);
        Assert.Equal(410, timeline.X(3360000));
        Assert.Equal(536, timeline.X(4320000));
        Assert.Equal(126, timeline.X(4320000) - timeline.X(3360000));
        Assert.Equal(14, timeline.X(120000));
        Assert.Equal(0, timeline.X(-1));
        Assert.Equal(timeline.Width, timeline.X(long.MaxValue));
    }
}
