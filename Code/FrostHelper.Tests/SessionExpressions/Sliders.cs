namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Sliders {
    [Fact]
    public void Indirect() {
        var e = TestUtils.CreateExpr(@"@""array$(@x)""");
        
        var session = new Session();
        session.SetSlider("array0", -2);
        session.SetSlider("array1.25", 6);
        
        Assert.Equal(-2, e.GetInt(session));
        session.SetSlider("x", 1.25f);
        Assert.Equal(6, e.GetInt(session));
        session.SetSlider("x", 2);
        Assert.Equal(0, e.GetInt(session));
    }
}
