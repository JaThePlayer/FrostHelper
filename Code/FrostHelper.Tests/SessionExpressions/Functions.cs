namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Functions {
    [Fact]
    public void Range() {
        var session = TestUtils.CreateTestSession();
        
        Assert.Equal([0, 1], TestUtils.CreateExpr("$range(0, 2)").Get<IEnumerable<int>>(session));
        Assert.Equal([1, 2], TestUtils.CreateExpr("$range(1, 2)").Get<IEnumerable<int>>(session));
        
        Assert.Equal(6, TestUtils.CreateExpr("$range(1, 3).sum($i => $i)").Get<int>(session));
        
        Assert.True(TestUtils.CreateExpr("$range(1, 3).any($i => $i == 3)").Check(session));
        Assert.False(TestUtils.CreateExpr("$range(1, 3).any($i => $i == 4)").Check(session));
        
        Assert.True(TestUtils.CreateExpr("$range(1, 3).all($i => $i < 4)").Check(session));
        Assert.False(TestUtils.CreateExpr("$range(1, 3).all($i => $i > 1)").Check(session));
    }
}