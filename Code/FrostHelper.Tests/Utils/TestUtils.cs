using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;
using System.Runtime.CompilerServices;

namespace FrostHelper.Tests;

public static class TestUtils {
    public static readonly object EngineSceneLock = new(); 
    public static readonly object SettingsInstanceLock = new(); 
    
    public static IEnumerable<(bool, bool)> BoolPermutations => [(false, false), (false, true), (true, false), (true, true)];
    
    public static ConditionHelper.Condition CreateExpr(string txt, ExpressionContext? context = null, bool createHybrid = true) {
        Assert.True(ConditionHelper.TryCreate(txt, context ?? ExpressionContext.Default, out var cond));
        Assert.NotNull(cond);
        
        if (createHybrid)
            cond = new HybridExpression<object>(cond);
        
        return cond;
    }
    
    public static HybridExpression<T> CreateHybridExpr<T>(string txt, ExpressionContext? context = null) {
        Assert.True(ConditionHelper.TryCreate(txt, context ?? ExpressionContext.Default, out var cond));
        Assert.NotNull(cond);
        
        return new HybridExpression<T>(cond);
    }
    
    public static T CreateExpr<T>(string txt, ExpressionContext? context = null) where T : ConditionHelper.Condition {
        var ret = CreateExpr(txt, context, createHybrid: false);
        return Assert.IsType<T>(ret);
    }

    public static Session CreateTestSession() {
        return new Session {
            Area = MockMap.AreaKey,
            Level = MockMap.MockRoomName,
        };
    }

    public static Level CreateLevel() {
        var level = new Level();
        level.Session = CreateTestSession();
        level.HudRenderer = new HudRenderer();
        
        return level;
    }

    public sealed class HybridExpression<T>(ConditionHelper.Condition basedOn) : ConditionHelper.Condition {
        private readonly CompiledCondition<T> _compiled = CompiledCondition<T>.GetFor(basedOn);
        
        public ConditionHelper.Condition SourceCondition => basedOn;
        
        public override object Get(Session session, object? userdata) {
            return GetT(session, userdata)!;
        }
        
        public T GetT(Session session, object? userdata = null) {
            var ret = basedOn.Get<T>(session, userdata);
            var compiledRet = _compiled.Get(session, userdata);

            if (_compiled.CompilationException is not null) {
                Assert.Fail($"Expression failed to compile: {_compiled.CompilationException}.");
            }

            if (!ret!.Equals(compiledRet) && !ret.ToString()!.Equals(compiledRet!.ToString())) {
                Assert.Fail($"Expression didn't return the same value between compiled and interpreted execution!: '{ret}' (interpreted) vs '{compiledRet}' (compiled)");
            }

            return compiledRet;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            basedOn.Emit(ctx, targetType);
        }

        protected internal override Type? ReturnType => basedOn.ReturnType;

        internal override bool UsesCurrentConditionLocalInEmit => basedOn.UsesCurrentConditionLocalInEmit;

        public override bool OnlyChecksFlags() => basedOn.OnlyChecksFlags();
    }
}