using FrostHelper.Helpers;

namespace FrostHelper.Tests.Helpers;

[Collection("FrostHelper")]
public class EntityFilterTests {
    [Fact]
    public void Parsing() {
        var filter = EntityFilter.CreateFrom("spinner,3", isBlacklist: false);
        Assert.Equal([ typeof(CrystalStaticSpinner) ], filter.Types);
        Assert.Equal([ 3 ], filter.Ids);
        
        // Whitespace is trimmed, regression reported in #20.
        filter = EntityFilter.CreateFrom("  7 ,    spinner, dreamBlock , 3   ", isBlacklist: false);
        Assert.Equal([ typeof(CrystalStaticSpinner), typeof(DreamBlock) ], filter.Types);
        Assert.Equal([ 7, 3 ], filter.Ids);
    }
}