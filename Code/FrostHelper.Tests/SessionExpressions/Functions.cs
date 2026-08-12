namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Functions {
    [Fact]
    public void Range() {
        var session = TestUtils.CreateTestSession();
        
        Assert.Equal([0, 1], TestUtils.CreateExpr("$range(0, 2)").Get<IEnumerable<int>>(session));
        Assert.Equal([1, 2], TestUtils.CreateExpr("$range(1, 2)").Get<IEnumerable<int>>(session));
        
        Assert.Equal(6, TestUtils.CreateExpr("$range(1, 3).sum($i => $i)").Get<int>(session));
    }
}