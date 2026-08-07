using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;

namespace FrostHelper.Tests;

public static class TestUtils {
    public static IEnumerable<(bool, bool)> BoolPermutations => [(false, false), (false, true), (true, false), (true, true)];
    
    public static ConditionHelper.Condition CreateExpr(string txt, ExpressionContext? context = null, bool createHybrid = true) {
        Assert.True(ConditionHelper.TryCreate(txt, context ?? ExpressionContext.Default, out var cond));
        Assert.NotNull(cond);
        
        if (createHybrid)
            cond = new HybridExpression(cond);
        
        return cond;
    }
    
    public static T CreateExpr<T>(string txt, ExpressionContext? context = null) where T : ConditionHelper.Condition {
        var ret = CreateExpr(txt, context, createHybrid: false);
        return Assert.IsType<T>(ret);
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

    internal sealed class HybridExpression(ConditionHelper.Condition basedOn) : ConditionHelper.Condition {
        private readonly CompiledSessionExpression<object> _compiled = new CompiledSessionExpression<object>(basedOn);
        
        public ConditionHelper.Condition SourceCondition => basedOn;
        
        public override object Get(Session session, object? userdata) {
            var ret = basedOn.Get(session, userdata);
            var compiledRet = _compiled.Get(session, userdata);

            if (!ret.Equals(compiledRet)) {
                Assert.Fail($"Expression didn't return the same value between compiled and interpreted execution!: '{ret}' (interpreted) vs '{compiledRet}' (compiled)");
            }

            return ret;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            basedOn.Emit(ctx, targetType);
        }

        protected internal override Type? ReturnType => basedOn.ReturnType;

        internal override bool UsesCurrentConditionLocalInEmit => basedOn.UsesCurrentConditionLocalInEmit;

        public override bool OnlyChecksFlags() => basedOn.OnlyChecksFlags();
    }
}