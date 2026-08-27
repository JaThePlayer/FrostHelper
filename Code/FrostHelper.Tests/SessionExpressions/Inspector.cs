using FrostHelper.API;
using FrostHelper.SessionExpressions;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Inspector {
    [Fact]
    public void Commands() {
        var inspector = new InspectorSession(ExpressionContext.Default);
        inspector.CurrentExpression = "$player";
        
        Assert.Equal([
            ( "$player", "command", [
                ("player", "command"),
                (" -> ", "default"),
                ("Player", "type"),
                ("\n", "whitespace"),
                ("Current player Entity instance.", "default")
            ])
        ], inspector.GetRenderParts().AsEnumerable());
        
        inspector.CurrentExpression = "$player.x";
        Assert.Equal([
            ( "$player", "command", [
                ("player", "command"),
                (" -> ", "default"),
                ("Player", "type"),
                ("\n", "whitespace"),
                ("Current player Entity instance.", "default")
            ]),
            ( ".", "operator", null),
            ("x", "field", [
                ("Entity", "type"),
                (".", "operator"),
                ("x", "command"),
                (" -> ", "default"),
                ("float", "type"),
                ("\n", "whitespace"),
                ("The position of the entity on the x-axis.", "default")
            ])
        ], inspector.GetRenderParts().AsEnumerable());
    }
}