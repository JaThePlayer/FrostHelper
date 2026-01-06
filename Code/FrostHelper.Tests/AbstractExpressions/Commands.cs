using FrostHelper.Helpers;

namespace FrostHelper.Tests.AbstractExpressions;

[Collection("FrostHelper")]
public class Commands {
    [Fact]
    public void SimpleCommands() {
        Assert.True(AbstractExpression.TryParseCached("$hi", out var expr));
        var simpleCommand = Assert.IsType<SimpleCommandExpression>(expr);
        Assert.Equal("hi", simpleCommand.Name);
    }
    
    [Fact]
    public void FunctionCommands() {
        Assert.True(AbstractExpression.TryParseCached("$hi(3)", out var expr));
        var func = Assert.IsType<FunctionCommandExpression>(expr);
        Assert.Equal("hi", func.Name);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(Assert.Single(func.Arguments)).Value);
        
        Assert.True(AbstractExpression.TryParseCached("$hi(3, 4 ,5)", out expr));
        func = Assert.IsType<FunctionCommandExpression>(expr);
        Assert.Equal("hi", func.Name);
        Assert.Equal(3, func.Arguments.Count);
        Assert.Equal(3, Assert.IsType<LiteralExpression<int>>(func.Arguments[0]).Value);
        Assert.Equal(4, Assert.IsType<LiteralExpression<int>>(func.Arguments[1]).Value);
        Assert.Equal(5, Assert.IsType<LiteralExpression<int>>(func.Arguments[2]).Value);
    }

    [Fact]
    public void NestedCalls() {
        Assert.True(AbstractExpression.TryParseCached("$hi($hi2(1), 3)", out var expr));
        Assert.True(expr is FunctionCommandExpression {
            Name: "hi",
            Arguments: [
                FunctionCommandExpression {
                    Name: "hi2",
                    Arguments: [
                        LiteralExpression<int> { Value: 1 }
                    ],
                },
                LiteralExpression<int> { Value: 3 }
            ]
        });
    }
}