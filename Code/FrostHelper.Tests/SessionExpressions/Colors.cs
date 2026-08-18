namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Colors {
    [Fact]
    public void Rgb() {
        var session = new Session();
        
        Assert.Equal(new Color(255, 41, 16), TestUtils.CreateHybridExpr<Color>("$rgb(255, 41, 16)").GetT(session));
    }

    [Fact]
    public void Hsv() {
        var session = new Session();
        
        Assert.Equal(Calc.HsvToColor(0.3f, 0.6f, 0.7f), TestUtils.CreateHybridExpr<Color>("$hsv(0.3, 0.6, 0.7)").GetT(session));
    }
}
