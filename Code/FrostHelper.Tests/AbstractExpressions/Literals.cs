using FrostHelper.Helpers;

namespace FrostHelper.Tests.AbstractExpressions;

[Collection("FrostHelper")]
public class Literals {
    [Fact]
    public void SimpleLiterals() {
        Assert.True(AbstractExpression.TryParseCached("1", out var expr));
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(expr).Value);
        
        Assert.True(AbstractExpression.TryParseCached("1.", out expr));
        Assert.Equal(1, Assert.IsType<LiteralExpression<float>>(expr).Value);
        
        Assert.True(AbstractExpression.TryParseCached("\"hi\"", out expr));
        Assert.Equal("hi", Assert.IsType<LiteralExpression<string>>(expr).Value);
    }

    [Fact]
    public void InterpolatedStrings() {
        Assert.True(AbstractExpression.TryParseCached("\"hi $(3)\"", out var expr));
        var interp = Assert.IsType<InterpolatedStringExpression>(expr);
        Assert.Equal(2, interp.Arguments.Count);
        Assert.Equal("hi ", Assert.IsType<LiteralExpression<string>>(interp.Arguments[0]).Value);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(interp.Arguments[1]).Value);
    }
    
    [Fact]
    public void InterpolatedStrings_FailConditions() {
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("\"hi $(3\"", out var expr));
        }
        
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("\"hi $\"", out var expr));
        }
        
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("\"hi $$(3)\"", out var expr));
        }
    }
}