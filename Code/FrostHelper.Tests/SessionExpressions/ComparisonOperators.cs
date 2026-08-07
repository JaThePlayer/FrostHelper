namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class ComparisonOperators {
    [Fact]
    public void Equality() {
        var session = new Session();
        
        Assert.True(TestUtils.CreateExpr("0 == 0.0").Check(session));
        Assert.True(TestUtils.CreateExpr("hi == 0.0").Check(session));
        session.SetFlag("hi");
        Assert.False(TestUtils.CreateExpr("hi == 0.0").Check(session));
        Assert.True(TestUtils.CreateExpr("\"a\" == \"a\"").Check(session));
        Assert.False(TestUtils.CreateExpr("\"$(@number)\" == \"0.125\"").Check(session));
        session.SetSlider("number", 0.125f);
        Assert.True(TestUtils.CreateExpr("\"$(@number)\" == \"0.125\"").Check(session));
        
        // Equality should not have precedence over addition
        session.SetFlag("bye");
        Assert.True(TestUtils.CreateExpr("bye + hi == 2").Check(session));
        session.SetFlag("false");
        Assert.True(TestUtils.CreateExpr("bye + hi == 2").Check(session));
    }
    
    [Fact]
    public void Comparisons() {
        var session = new Session();
        
        Assert.False(TestUtils.CreateExpr("0 > 1").Check(session));
        // Ints get coerced to floats if needed
        Assert.False(TestUtils.CreateExpr("1 >= 1.1").Check(session));
        Assert.True(TestUtils.CreateExpr("1 < 1.1").Check(session));
        
        Assert.True(TestUtils.CreateExpr("\"a\" < \"b\"").Check(session));
        Assert.False(TestUtils.CreateExpr("\"a\" > \"b\"").Check(session));
        Assert.True(TestUtils.CreateExpr("\"a\" != \"b\"").Check(session));
        Assert.False(TestUtils.CreateExpr("\"a\" != \"a\"").Check(session));
    }
}