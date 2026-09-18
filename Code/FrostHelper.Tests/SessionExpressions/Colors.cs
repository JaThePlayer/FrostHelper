namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Colors {
    [Fact]
    public void Accessors() {
        var session = new Session();
        
        Assert.Equal(258, TestUtils.CreateHybridExpr<int>("$rgb(255, 41, 16).r + 3").GetT(session));
        Assert.Equal(41, TestUtils.CreateHybridExpr<int>("$rgb(255, 41, 16).g").GetT(session));
        Assert.Equal(16, TestUtils.CreateHybridExpr<int>("$rgb(255, 41, 16).b").GetT(session));
    }
    
    [Fact]
    public void Rgb() {
        var session = new Session();
        
        Assert.Equal(new Color(255, 41, 16), TestUtils.CreateHybridExpr<Color>("$rgb(255, 41, 16)").GetT(session));
        Assert.Equal(new Color(255, 41, 16, 40), TestUtils.CreateHybridExpr<Color>("$rgba(255, 41, 16, 40)").GetT(session));
    }

    [Fact]
    public void Hsv() {
        var session = new Session();
        
        Assert.Equal(Calc.HsvToColor(0.3f, 0.6f, 0.7f), TestUtils.CreateHybridExpr<Color>("$hsv(0.3, 0.6, 0.7)").GetT(session));
        Assert.Equal(Calc.HsvToColor(0.3f, 0.6f, 0.7f) with { A = 40 }, TestUtils.CreateHybridExpr<Color>("$hsva(0.3, 0.6, 0.7, 40)").GetT(session));
    }

    [Fact]
    public void OperatorMul() {
        var session = new Session();
        
        Assert.Equal(new Color(255, 41, 16) * 0.3f, TestUtils.CreateHybridExpr<Color>("$rgb(255, 41, 16) * .3").GetT(session));
        Assert.Equal(new Color(255, 41, 16) * 0.3f, TestUtils.CreateHybridExpr<Color>(".3 * $rgb(255, 41, 16)").GetT(session));
    }

    [Fact]
    public void Lerpc() {
        var session = new Session();
        
        Assert.Equal(Color.Lerp(Color.White, Color.Red, 0.5f), TestUtils.CreateHybridExpr<Color>("""$lerpc("white", "red", 0.5)""").GetT(session));
    }
}
