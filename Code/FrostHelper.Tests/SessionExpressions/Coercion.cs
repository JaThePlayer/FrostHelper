namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Coercion {
    [Fact]
    public void Bool() {
        var session = new Session();
        
        // 0 is false, everything else is 1.
        Assert.False(TestUtils.CreateExpr("0").Check(session));
        Assert.True(TestUtils.CreateExpr("1").Check(session));
        Assert.True(TestUtils.CreateExpr("12").Check(session));
        // Same for floats
        Assert.False(TestUtils.CreateExpr("0.").Check(session));
        Assert.True(TestUtils.CreateExpr("1.").Check(session));
        Assert.True(TestUtils.CreateExpr("12.").Check(session));
        
        // Any string is true, even if empty or "0".
        Assert.True(TestUtils.CreateExpr("\"\"").Check(session));
        Assert.True(TestUtils.CreateExpr("\"0\"").Check(session));
    }

    [Fact]
    public void Int() {
        var session = new Session();
        
        Assert.Equal(0, TestUtils.CreateExpr("0").Get<int>(session));
        Assert.Equal(1, TestUtils.CreateExpr("1").Get<int>(session));
        Assert.Equal(-123123, TestUtils.CreateExpr("-123123").Get<int>(session));
        Assert.Equal(-2147483648, TestUtils.CreateExpr("2147483647 + 1").Get<int>(session));
        
        // Float -> Int is truncating
        Assert.Equal(1, TestUtils.CreateExpr("1.99").Get<int>(session));
        Assert.Equal(-3, TestUtils.CreateExpr("-3.99").Get<int>(session));
        // As consequence, since an int literal that's too big becomes a float, it gets truncated when cast to int...
        Assert.Equal(2147483647, TestUtils.CreateExpr("21474836471").Get<int>(session));
    }
    
    [Fact]
    public void Float() {
        var session = new Session();
        
        Assert.Equal(0.4f, TestUtils.CreateExpr("0.4").Get<float>(session));
        Assert.Equal(1.123f, TestUtils.CreateExpr("1.123").Get<float>(session));
        Assert.Equal(-123.123f, TestUtils.CreateExpr("-123.123").Get<float>(session));
        Assert.Equal(21474836471f, TestUtils.CreateExpr("21474836471").Get<float>(session));
        
        // If left and right is an int, the math operation is done on ints, otherwise on floats.
        Assert.Equal(-2147483648f, TestUtils.CreateExpr("2147483647 + 1").Get<float>(session));
        Assert.Equal(2147483648f, TestUtils.CreateExpr("2147483647.0 + 1").Get<float>(session));
        Assert.Equal(2147483648f, TestUtils.CreateExpr("2147483647 + 1.0").Get<float>(session));
        Assert.Equal(21474836472f, TestUtils.CreateExpr("21474836471 + 1").Get<float>(session));
        
        Assert.Equal(0f, TestUtils.CreateExpr("1 / 2").Get<float>(session));
        Assert.Equal(0.5f, TestUtils.CreateExpr("1 / 2.").Get<float>(session));
        Assert.Equal(0.5f, TestUtils.CreateExpr("1 // 2").Get<float>(session));
    }
}