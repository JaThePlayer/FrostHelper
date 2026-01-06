using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;

namespace FrostHelper.Tests;

public static class TestUtils {
    public static IEnumerable<(bool, bool)> BoolPermutations => [(false, false), (false, true), (true, false), (true, true)];
    
    public static ConditionHelper.Condition CreateExpr(string txt) {
        Assert.True(ConditionHelper.TryCreate(txt, ExpressionContext.Default, out var cond));
        Assert.NotNull(cond);
        
        return cond;
    }
    
    public static T CreateExpr<T>(string txt) where T : ConditionHelper.Condition {
        return Assert.IsType<T>(CreateExpr(txt));
    }

    public static Session CreateTestSession() {
        return new Session {
            Area = MockMap.AreaKey,
        };
    }

    public static Level CreateLevel() {
        var level = new Level();
        level.Session = CreateTestSession();
        level.HudRenderer = new HudRenderer();
        
        return level;
    }
}