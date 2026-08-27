namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class MathTests {
    [Fact]
    public void Spaceless() {
        var session = new Session();
        
        Assert.Equal(7, TestUtils.CreateExpr("4+3").Get<int>(session));
        Assert.Equal(1, TestUtils.CreateExpr("4-3").Get<int>(session));
        Assert.Equal(1f, TestUtils.CreateExpr("4/3").Get<float>(session));
        Assert.Equal(4/3f, TestUtils.CreateExpr("4//3").Get<float>(session));
        // Frost Helper versions <= 1.79.1 used to treat this as a flag name "4%3", because `%` wasn't treated as a word separator!
        Assert.Equal(1, TestUtils.CreateExpr("4%3").Get<int>(session));
    }
    
    [Fact]
    public void Division() {
        var session = new Session();
        
        // Division by 0 returns 0 in all cases instead of crashing.
        Assert.Equal(0, TestUtils.CreateExpr("1 / 0").Get<int>(session));
        Assert.Equal(0, TestUtils.CreateExpr("1 // 0").Get<int>(session));
    }

    [Fact]
    public void Remainder() {
        var session = new Session();
        
        Assert.Equal(3, TestUtils.CreateExpr("3 % 5").Get<int>(session));
        
        // Remainder of 0 returns 0 in all cases instead of crashing.
        Assert.Equal(0, TestUtils.CreateExpr("3 % 0").Get<int>(session));
    }

    [Fact]
    public void Unary() {
        var session = new Session();
        
        Assert.Equal(-6, TestUtils.CreateExpr("3*-2").Get<int>(session));
    }
    
    [Fact]
    public void RightAssociativity() {
        var session = new Session();
        
        // Frost Helper versions <= 1.79.1 used to return wrong values here,
        // as they treated this as 3 * (5 / 3) instead of (3 * 5) / 3.
        Assert.Equal(5, TestUtils.CreateExpr("3 * 5 / 3").Get<int>(session));
        Assert.Equal(1, TestUtils.CreateExpr("3 % 5 / 3").Get<int>(session));
        
        Assert.Equal(6, TestUtils.CreateExpr("5 - 1 + 2").Get<int>(session));
        
        Assert.Equal(6, TestUtils.CreateExpr("3 % 5 + 3").Get<int>(session));
        Assert.Equal(0, TestUtils.CreateExpr("3 % 5 + -3").Get<int>(session));
    }
}