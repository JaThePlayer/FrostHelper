using FrostHelper.Helpers;

namespace FrostHelper.Tests.AbstractExpressions;

[Collection("FrostHelper")]
public class Lambdas {
    [Fact]
    public void ZeroArgLambda() {
        Assert.True(AbstractExpression.TryParseCached("$() => $s", out var expr));
        var lambda = Assert.IsType<LambdaExpression>(expr);
        Assert.Equal([ ], lambda.ArgumentNames);
        Assert.Equal("s", Assert.IsType<SimpleCommandExpression>(lambda.Code).Name);
    }
    
    [Fact]
    public void OneArgLambda() {
        Assert.True(AbstractExpression.TryParseCached("$s => $s", out var expr));
        var lambda = Assert.IsType<LambdaExpression>(expr);
        Assert.Equal([ "s" ], lambda.ArgumentNames);
        Assert.Equal("s", Assert.IsType<SimpleCommandExpression>(lambda.Code).Name);
        
        Assert.True(AbstractExpression.TryParseCached("$(s) => $s", out expr));
        lambda = Assert.IsType<LambdaExpression>(expr);
        Assert.Equal([ "s" ], lambda.ArgumentNames);
        Assert.Equal("s", Assert.IsType<SimpleCommandExpression>(lambda.Code).Name);
        
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("$s =>", out _));
        }
        
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("$(1) => 1", out _));
        }
        
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("$(x+y) => 1", out _));
        }
        
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("=> 1", out _));
        }
    }
    
    [Fact]
    public void TwoArgLambda() {
        Assert.True(AbstractExpression.TryParseCached("$(a, b) => $a + $b", out var expr));
        var lambda = Assert.IsType<LambdaExpression>(expr);
        Assert.Equal([ "a", "b" ], lambda.ArgumentNames);
        Assert.Equal(BinOpExpression.Operators.Add, Assert.IsType<BinOpExpression>(lambda.Code).Operator);
    }
    
    [Fact]
    public void LambaAsArgument() {
        Assert.True(AbstractExpression.TryParseCached("$strawberries.count($s => $s.roomName == \"a\")", out var expr));
        
        Assert.True(expr is FunctionCommandExpression {
            Name: "strawberries.count",
            Arguments: [
                LambdaExpression {
                    ArgumentNames: [ "s" ],
                    Code: BinOpExpression {
                        Operator: BinOpExpression.Operators.Eq,
                        Left: SimpleCommandExpression {
                            Name: "s.roomName"
                        },
                        Right: LiteralExpression<string> {
                            Value: "a"
                        }
                    }
                }
            ]
        });
    }
}