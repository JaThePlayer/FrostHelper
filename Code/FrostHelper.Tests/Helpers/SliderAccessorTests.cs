using FrostHelper.Helpers;

namespace FrostHelper.Tests.Helpers;

[Collection("FrostHelper")]
public class SliderAccessorTests {
    [Fact]
    public void Usage() {
        const string sliderName = "cool";
        
        var session = new Session();
        var sessionB = new Session();
        var access = new SliderAccessor(sliderName);
        
        Assert.Equal(0, access.Get(session));
        Assert.Equal(0, access.Get(sessionB));
        
        session.SetSlider(sliderName, 1f);
        sessionB.SetSlider(sliderName, 2f);
        Assert.Equal(1f, access.Get(session));
        Assert.Equal(2f, access.Get(sessionB));

        access.Set(session, 3f);
        Assert.Equal(3f, access.Get(session));
        Assert.Equal(3f, session.GetSlider(sliderName));
    }
}