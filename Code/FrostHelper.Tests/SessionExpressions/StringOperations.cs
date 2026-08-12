using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class StringOperations {
    [Fact]
    public void StringLen() {
        var session = new Session();
        
        Assert.Equal(4, TestUtils.CreateExpr("\"test\".len").GetInt(session));
    }
    
    [Fact]
    public void StringMatch() {
        var session = new Session();
        
        Assert.True(TestUtils.CreateExpr("\"test\".isMatch(\"t.*t\")").Check(session));
        Assert.False(TestUtils.CreateExpr("\"test\".isMatch(\"b-.*\")").Check(session));

        session.Level = "b-1";
        Assert.True(TestUtils.CreateExpr("$roomName.isMatch(\"b-.*\")").Check(session));
        
        Assert.True(TestUtils.CreateExpr("$roomName.isMatch(\"b-.*\").str(\"x\").isMatch(\"1\")").Check(session));
    }
}