namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Counters {
    [Fact]
    public void Indirect() {
        var e = TestUtils.CreateExpr(@"#""array$(#x)""");
        
        var session = new Session();
        session.SetCounter("array0", -2);
        session.SetCounter("array1", 6);
        
        Assert.Equal(-2, e.GetInt(session));
        session.SetCounter("x", 1);
        Assert.Equal(6, e.GetInt(session));
        session.SetCounter("x", 2);
        Assert.Equal(0, e.GetInt(session));
    }
}