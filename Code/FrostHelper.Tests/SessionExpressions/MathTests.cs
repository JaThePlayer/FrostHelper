namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class MathTests {
    [Fact]
    public void Division() {
        var session = new Session();
        
        // Division by 0 returns 0 in all cases instead of crashing.
        Assert.Equal(0, TestUtils.CreateExpr("1 / 0").Get<int>(session));
        Assert.Equal(0, TestUtils.CreateExpr("1 // 0").Get<int>(session));
    }
}