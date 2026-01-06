using FrostHelper.Helpers;

namespace FrostHelper.Tests.AbstractExpressions;

[Collection("FrostHelper")]
public class GetSessionVariableExpressionTests {
    [Fact]
    public void Flag() {
        Assert.True(AbstractExpression.TryParseCached("flag", out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Flag, sessionVarExpr.VariableType);
        
        var nameExpr = Assert.IsType<LiteralExpression<string>>(sessionVarExpr.Name);
        Assert.Equal("flag", nameExpr.Value);
    }
    
    [Fact]
    public void FlagIndirect_ConstStr() {
        Assert.True(AbstractExpression.TryParseCached("""
        f"test"
        """, out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Flag, sessionVarExpr.VariableType);
        
        var nameExpr = Assert.IsType<LiteralExpression<string>>(sessionVarExpr.Name);
        Assert.Equal("test", nameExpr.Value);
    }
    
    [Fact]
    public void FlagIndirect_Interpolated() {
        Assert.True(AbstractExpression.TryParseCached("""
        f"$(3)"
        """.Trim(), out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Flag, sessionVarExpr.VariableType);
        
        var nameExpr = Assert.IsType<InterpolatedStringExpression>(sessionVarExpr.Name);
        var onlyArg = Assert.Single(nameExpr.Arguments);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(onlyArg).Value);
    }
    
    [Fact]
    public void FlagIndirect_Interpolated_MoreComplex() {
        Assert.True(AbstractExpression.TryParseCached("""
        f"flag_$(3)"
        """.Trim(), out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Flag, sessionVarExpr.VariableType);
        
        var nameExprArgs = Assert.IsType<InterpolatedStringExpression>(sessionVarExpr.Name).Arguments;
        Assert.Equal(2, nameExprArgs.Count);
        Assert.Equal("flag_", Assert.IsType<LiteralExpression<string>>(nameExprArgs[0]).Value);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(nameExprArgs[1]).Value);
    }
    
    [Fact]
    public void Counter() {
        Assert.True(AbstractExpression.TryParseCached("#counter", out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Counter, sessionVarExpr.VariableType);
        
        var nameExpr = Assert.IsType<LiteralExpression<string>>(sessionVarExpr.Name);
        Assert.Equal("counter", nameExpr.Value);
    }
    
    [Fact]
    public void Counter_Interpolated() {
        Assert.True(AbstractExpression.TryParseCached("""
        #"$(3)"
        """.Trim(), out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Counter, sessionVarExpr.VariableType);
        
        var nameExpr = Assert.IsType<InterpolatedStringExpression>(sessionVarExpr.Name);
        var onlyArg = Assert.Single(nameExpr.Arguments);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(onlyArg).Value);
    }
    
    [Fact]
    public void Slider() {
        Assert.True(AbstractExpression.TryParseCached("@slider", out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Slider, sessionVarExpr.VariableType);
        
        var nameExpr = Assert.IsType<LiteralExpression<string>>(sessionVarExpr.Name);
        Assert.Equal("slider", nameExpr.Value);
    }
    
    [Fact]
    public void Slider_Interpolated() {
        Assert.True(AbstractExpression.TryParseCached("""
        @"$(3)"
        """.Trim(), out var expr));
        
        var sessionVarExpr = Assert.IsType<GetSessionVariableExpression>(expr);
        Assert.Equal(GetSessionVariableExpression.Types.Slider, sessionVarExpr.VariableType);
        
        var nameExpr = Assert.IsType<InterpolatedStringExpression>(sessionVarExpr.Name);
        var onlyArg = Assert.Single(nameExpr.Arguments);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(onlyArg).Value);
    }
}