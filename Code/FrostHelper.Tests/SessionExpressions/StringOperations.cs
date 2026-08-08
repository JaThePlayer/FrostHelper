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
        
        Assert.True(TestUtils.CreateExpr("\"test\".match(\"t.*t\")").Check(session));
        Assert.False(TestUtils.CreateExpr("\"test\".match(\"b-.*\")").Check(session));

        session.Level = "b-1";
        Assert.True(TestUtils.CreateExpr("$roomName.match(\"b-.*\")").Check(session));
        
        Assert.True(TestUtils.CreateExpr("$roomName.match(\"b-.*\").str(\"x\").match(\"1\")").Check(session));
    }
}