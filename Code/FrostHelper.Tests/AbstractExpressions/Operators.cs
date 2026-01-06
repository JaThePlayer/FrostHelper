using FrostHelper.Helpers;

namespace FrostHelper.Tests.AbstractExpressions;

[Collection("FrostHelper")]
public class Operators {
    [Fact]
    public void Unary() {
        Assert.True(AbstractExpression.TryParseCached("+3", out var expr));
        var binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Add, binOp.Operator);
        Assert.Equal(0, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("-3", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Sub, binOp.Operator);
        Assert.Equal(0, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("--3", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Sub, binOp.Operator);
        Assert.Equal(0, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        binOp = Assert.IsType<BinOpExpression>(binOp.Right);
        Assert.Equal(0, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("!3", out expr));
        var invertOp = Assert.IsType<InvertExpression>(expr);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(invertOp.Expression).Value);
        
        Assert.True(AbstractExpression.TryParseCached("!!3", out expr));
        invertOp = Assert.IsType<InvertExpression>(expr);
        invertOp = Assert.IsType<InvertExpression>(invertOp.Expression);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(invertOp.Expression).Value);
        
        Assert.True(AbstractExpression.TryParseCached("-(3+1)", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(0, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        binOp = Assert.IsType<BinOpExpression>(binOp.Right);
        Assert.Equal(BinOpExpression.Operators.Add, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("!(3+1)", out expr));
        invertOp = Assert.IsType<InvertExpression>(expr);
        binOp = Assert.IsType<BinOpExpression>(invertOp.Expression);
        Assert.Equal(BinOpExpression.Operators.Add, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
    }

    [Fact]
    public void Binary() {
        Assert.True(AbstractExpression.TryParseCached("3+1", out var expr));
        var binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Add, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3-1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Sub, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3*1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Mul, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3/1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Div, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3//1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.DivFloat, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3&1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.BitwiseAnd, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3&&1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.And, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3|1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.BitwiseOr, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3||1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Or, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3>1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Gt, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3>=1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Ge, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3<1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Lt, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3<=1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Le, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3==1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Eq, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
        
        Assert.True(AbstractExpression.TryParseCached("3!=1", out expr));
        binOp = Assert.IsType<BinOpExpression>(expr);
        Assert.Equal(BinOpExpression.Operators.Ne, binOp.Operator);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(binOp.Left).Value);
        Assert.Equal(1, Assert.IsType<LiteralExpression<int>>(binOp.Right).Value);
    }

    [Fact]
    public void WeirdBehaviorWithMultAndUnaryMinus() {
        // TODO: Found during writing tests, should not do this!
        using (_ = new NotificationExpecter(1)) {
            Assert.False(AbstractExpression.TryParseCached("1*-2", out _));
        }
        
        Assert.True(AbstractExpression.TryParseCached("1*(-2)", out var expr));
    }

    [Fact]
    public void Precedence() {
        Assert.True(AbstractExpression.TryParseCached("3+1*2", out var expr));
        Assert.True(expr is BinOpExpression {
            Operator: BinOpExpression.Operators.Add,
            Left: LiteralExpression<int> { Value: 3 },
            Right: BinOpExpression {
                Operator: BinOpExpression.Operators.Mul,
                Left: LiteralExpression<int> { Value: 1 },
                Right: LiteralExpression<int> { Value: 2 },
            },
        });
        
        Assert.True(AbstractExpression.TryParseCached("(3+1)*2", out expr));
        Assert.True(expr is BinOpExpression {
            Operator: BinOpExpression.Operators.Mul,
            Left: BinOpExpression {
                Operator: BinOpExpression.Operators.Add,
                Left: LiteralExpression<int> { Value: 3 },
                Right: LiteralExpression<int> { Value: 1 },
            },
            Right: LiteralExpression<int> { Value: 2 },
        });

        Assert.True(AbstractExpression.TryParseCached("1 * (-2) >= 3 && !2 | 1+4 == 7", out expr));
        Assert.True(expr is BinOpExpression {
            Operator: BinOpExpression.Operators.And,
            Left: BinOpExpression {
                Operator: BinOpExpression.Operators.Ge,
                Left: BinOpExpression {
                    Operator: BinOpExpression.Operators.Mul,
                    Left: LiteralExpression<int> { Value: 1 },
                    Right: BinOpExpression {
                        Operator: BinOpExpression.Operators.Sub,
                        Left: LiteralExpression<int> { Value: 0 },
                        Right: LiteralExpression<int> { Value: 2 },
                    },
                },
                Right: LiteralExpression<int> { Value: 3 },
            },
            Right: BinOpExpression {
                Operator: BinOpExpression.Operators.Eq,
                Left: BinOpExpression {
                    Operator: BinOpExpression.Operators.BitwiseOr,
                    Left: InvertExpression {
                        Expression: LiteralExpression<int> { Value: 2 }
                    },
                    Right: BinOpExpression {
                        Operator: BinOpExpression.Operators.Add,
                        Left: LiteralExpression<int> { Value: 1 },
                        Right: LiteralExpression<int> { Value: 4 },
                    }
                },
                Right: LiteralExpression<int> { Value: 7 },
            },
        });
    }
}