using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;
using System.Text;
using Xunit.Abstractions;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class IlCompilation {
    private readonly ITestOutputHelper output;

    public IlCompilation(ITestOutputHelper output)
    {
        this.output = output;
    }
    
    void AssertIl(DynamicMethodDefinition method, string expected) {
        var actual = method.Definition.Body.Instructions;
        StringBuilder builder = new StringBuilder();
        foreach (var i in actual) {
            builder.AppendLine(i.ToString());
        }

        var result = builder.ToString().TrimEnd();
        if (expected.ReplaceLineEndings() != result) {
            output.WriteLine(result);
        }
        Assert.Equal(expected.ReplaceLineEndings(), result);
    }

    CompiledSessionExpression<T> AssertIl<T>(string expression, string expected, ExpressionContext? context = null) {
        var flagExpr = TestUtils.CreateExpr(expression, context);
        var compiled = new CompiledSessionExpression<T>(flagExpr);
        compiled.Jit();
        Assert.NotNull(compiled.CompiledMethod);
        AssertIl(compiled.CompiledMethod, expected);

        return compiled;
    }

    [Fact]
    public void Math() {
        AssertIl<int>("3 * 7", """
        IL_0000: ldc.i4 3
        IL_0005: ldc.i4 7
        IL_000a: mul
        IL_000b: ret
        """);
        
        AssertIl<float>("5 // 2", """
        IL_0000: ldc.r4 5
        IL_0005: ldc.r4 2
        IL_000a: div
        IL_000b: ret
        """);
        
        AssertIl<float>("hi // 2", """
        IL_0000: ldarg 
        IL_0004: ldstr "hi"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: ldc.i4.1
        IL_000f: ceq
        IL_0011: conv.r4
        IL_0012: ldc.r4 2
        IL_0017: div
        IL_0018: ret
        """);
       
        AssertIl<float>("2 / hi", """
        IL_0000: ldc.i4 2
        IL_0005: ldarg 
        IL_0009: ldstr "hi"
        IL_000e: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_0013: ldc.i4.1
        IL_0014: ceq
        IL_0016: call T FrostHelper.Helpers.ConditionHelper/OperatorDiv::Perform<System.Int32>(T,T)
        IL_001b: conv.r4
        IL_001c: ret
        """);
        
        AssertIl<float>("2 // hi", """
        IL_0000: ldc.r4 2
        IL_0005: ldarg 
        IL_0009: ldstr "hi"
        IL_000e: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_0013: ldc.i4.1
        IL_0014: ceq
        IL_0016: conv.r4
        IL_0017: call T FrostHelper.Helpers.ConditionHelper/OperatorDiv::Perform<System.Single>(T,T)
        IL_001c: ret
        """);
        
        AssertIl<float>("(5 // 2) + $time", """
        IL_0000: ldc.r4 5
        IL_0005: ldc.r4 2
        IL_000a: div
        IL_000b: call Monocle.Scene Monocle.Engine::get_Scene()
        IL_0010: ldfld System.Single Monocle.Scene::TimeActive
        IL_0015: add
        IL_0016: ret
        """);
        
        AssertIl<float>("$yoyo(2)", """
        IL_0000: ldc.r4 2
        IL_0005: call System.Single FrostHelper.SessionExpressions.FunctionCommands/YoYoFunc::Get(System.Single)
        IL_000a: ret
        """);
        
        AssertIl<float>("$pow2(2)", """
        IL_0000: ldc.i4 2
        IL_0005: call T FrostHelper.SessionExpressions.FunctionCommands/Pow2Func`1<System.Int32>::Get(T)
        IL_000a: conv.r4
        IL_000b: ret
        """);
        
        AssertIl<float>("$pow2(2.)", """
        IL_0000: ldc.r4 2
        IL_0005: call T FrostHelper.SessionExpressions.FunctionCommands/Pow2Func`1<System.Single>::Get(T)
        IL_000a: ret
        """);
        
        AssertIl<float>("$pow(2, 3)", """
        IL_0000: ldc.r4 2
        IL_0005: ldc.r4 3
        IL_000a: call T FrostHelper.SessionExpressions.FunctionCommands/PowFunc`1<System.Single>::Get(T,T)
        IL_000f: ret
        """);
        
        AssertIl<float>("$lerp(0, 1, 0.5)", """
        IL_0000: ldc.r4 0
        IL_0005: ldc.r4 1
        IL_000a: ldc.r4 1
        IL_000f: call System.Single FrostHelper.SessionExpressions.FunctionCommands/LerpFunc::Get(System.Single,System.Single,System.Single)
        IL_0014: ret
        """);
    }
    
    [Fact]
    public void Flags() {
        var flagExpr = AssertIl<int>("flagA + flagB", """
        IL_0000: ldarg 
        IL_0004: ldstr "flagA"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: ldc.i4.1
        IL_000f: ceq
        IL_0011: ldarg 
        IL_0015: ldstr "flagB"
        IL_001a: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_001f: ldc.i4.1
        IL_0020: ceq
        IL_0022: add
        IL_0023: ret
        """);
        var session = TestUtils.CreateTestSession();
        
        session.SetFlag("flagB");
        Assert.Equal(1, flagExpr.Get(session, null));
        
        AssertIl<bool>(@"f""hi$(1)""", """
        IL_0000: ldarg 
        IL_0004: ldloca V_1
        IL_0008: ldc.i4.0
        IL_0009: ldc.i4 2
        IL_000e: call FrostHelper.Helpers.Interpolator FrostHelper.Helpers.Interpolator::get_Shared()
        IL_0013: call System.Void FrostHelper.Helpers.Interpolator/Handler::.ctor(System.Int32,System.Int32,FrostHelper.Helpers.Interpolator)
        IL_0018: ldloca V_1
        IL_001c: ldstr "hi"
        IL_0021: call System.Void FrostHelper.Helpers.Interpolator/Handler::AppendLiteral(System.String)
        IL_0026: ldloca V_1
        IL_002a: ldc.i4 1
        IL_002f: call System.Void FrostHelper.Helpers.Interpolator/Handler::AppendFormatted<System.Int32>(T2)
        IL_0034: ldloca V_1
        IL_0038: call System.String FrostHelper.Helpers.Interpolator/Handler::ResultToString()
        IL_003d: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_0042: ret
        """);
                
        AssertIl<bool>(@"f""hi$(@f)""", """
        IL_0000: ldarg 
        IL_0004: ldloca V_1
        IL_0008: ldc.i4.0
        IL_0009: ldc.i4 2
        IL_000e: call FrostHelper.Helpers.Interpolator FrostHelper.Helpers.Interpolator::get_Shared()
        IL_0013: call System.Void FrostHelper.Helpers.Interpolator/Handler::.ctor(System.Int32,System.Int32,FrostHelper.Helpers.Interpolator)
        IL_0018: ldloca V_1
        IL_001c: ldstr "hi"
        IL_0021: call System.Void FrostHelper.Helpers.Interpolator/Handler::AppendLiteral(System.String)
        IL_0026: ldloca V_1
        IL_002a: ldarg 
        IL_002e: ldstr "f"
        IL_0033: callvirt System.Single Celeste.Session::GetSlider(System.String)
        IL_0038: call System.Void FrostHelper.Helpers.Interpolator/Handler::AppendFormatted<System.Single>(T2)
        IL_003d: ldloca V_1
        IL_0041: call System.String FrostHelper.Helpers.Interpolator/Handler::ResultToString()
        IL_0046: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_004b: ret
        """);
    }

    [Fact]
    public void Counters() {
        AssertIl<int>("#hi + #bye", """
        IL_0000: ldarg.2
        IL_0001: stloc V_0
        IL_0005: ldloc V_0
        IL_0009: stloc V_1
        IL_000d: ldloc V_1
        IL_0011: castclass FrostHelper.Helpers.ConditionHelper/BinaryOperator
        IL_0016: ldfld FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.Helpers.ConditionHelper/BinaryOperator::ConditionA
        IL_001b: stloc V_0
        IL_001f: ldloc V_0
        IL_0023: castclass FrostHelper.Helpers.ConditionHelper/CounterAccessor
        IL_0028: ldarg 
        IL_002c: callvirt System.Int32 FrostHelper.Helpers.ConditionHelper/CounterAccessor::GetCached(Celeste.Session)
        IL_0031: ldloc V_1
        IL_0035: castclass FrostHelper.Helpers.ConditionHelper/BinaryOperator
        IL_003a: ldfld FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.Helpers.ConditionHelper/BinaryOperator::ConditionB
        IL_003f: stloc V_0
        IL_0043: ldloc V_0
        IL_0047: castclass FrostHelper.Helpers.ConditionHelper/CounterAccessor
        IL_004c: ldarg 
        IL_0050: callvirt System.Int32 FrostHelper.Helpers.ConditionHelper/CounterAccessor::GetCached(Celeste.Session)
        IL_0055: ldloc V_1
        IL_0059: stloc V_0
        IL_005d: add
        IL_005e: ret
        """);
        
        AssertIl<int>("#\"count$(1)\"", """
        IL_0000: ldarg 
        IL_0004: ldloca V_1
        IL_0008: ldc.i4.0
        IL_0009: ldc.i4 2
        IL_000e: call FrostHelper.Helpers.Interpolator FrostHelper.Helpers.Interpolator::get_Shared()
        IL_0013: call System.Void FrostHelper.Helpers.Interpolator/Handler::.ctor(System.Int32,System.Int32,FrostHelper.Helpers.Interpolator)
        IL_0018: ldloca V_1
        IL_001c: ldstr "count"
        IL_0021: call System.Void FrostHelper.Helpers.Interpolator/Handler::AppendLiteral(System.String)
        IL_0026: ldloca V_1
        IL_002a: ldc.i4 1
        IL_002f: call System.Void FrostHelper.Helpers.Interpolator/Handler::AppendFormatted<System.Int32>(T2)
        IL_0034: ldloca V_1
        IL_0038: call System.String FrostHelper.Helpers.Interpolator/Handler::ResultToString()
        IL_003d: callvirt System.Int32 Celeste.Session::GetCounter(System.String)
        IL_0042: ret
        """);
    }

    [Fact]
    public void Vector2Tests() {
        AssertIl<Vector2>("$vec(2, 3)", """
        IL_0000: ldc.r4 2
        IL_0005: ldc.r4 3
        IL_000a: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_000f: ret
        """);
        
        AssertIl<float>("($vec(2, 3)).len", """
        IL_0000: ldc.r4 2
        IL_0005: ldc.r4 3
        IL_000a: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_000f: call System.Single FrostHelper.SessionExpressions.FieldAccessCommands/Vector2LenAccessor::GetValue(Microsoft.Xna.Framework.Vector2)
        IL_0014: ret
        """);
        
        AssertIl<Vector2>("$vec(2, 3) / 2", """
        IL_0000: ldc.r4 2
        IL_0005: ldc.r4 3
        IL_000a: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_000f: ldc.r4 2
        IL_0014: call Microsoft.Xna.Framework.Vector2 FrostHelper.Helpers.ConditionHelper/OperatorDiv::Perform(Microsoft.Xna.Framework.Vector2,System.Single)
        IL_0019: ret
        """);
        
        AssertIl<Vector2>("2 / $vec(2, 3)", """
        IL_0000: ldc.r4 2
        IL_0005: ldc.r4 2
        IL_000a: ldc.r4 3
        IL_000f: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_0014: call Microsoft.Xna.Framework.Vector2 FrostHelper.Helpers.ConditionHelper/OperatorDiv::Perform(System.Single,Microsoft.Xna.Framework.Vector2)
        IL_0019: ret
        """);
        
        AssertIl<Vector2>("$vec(0, 1) / $vec(2, 3)", """
        IL_0000: ldc.r4 0
        IL_0005: ldc.r4 1
        IL_000a: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_000f: ldc.r4 2
        IL_0014: ldc.r4 3
        IL_0019: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_001e: call Microsoft.Xna.Framework.Vector2 FrostHelper.Helpers.ConditionHelper/OperatorDiv::Perform(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)
        IL_0023: ret
        """);
    }

    [Fact]
    public void RainbowSpinnerHue() {
        var expr = "0.4 + $yoyo(($pos.len + $time * 50) % 280 / 280) * 0.4";
        
        var uncompiled = TestUtils.CreateExpr(expr, RainbowChannelExpression.ExpressionContext);
        var compiled = AssertIl<float>(expr, """
        IL_0000: ldc.r4 0.4
        IL_0005: ldarg 
        IL_0009: castclass FrostHelper.Helpers.RainbowChannelExpression
        IL_000e: callvirt Microsoft.Xna.Framework.Vector2 FrostHelper.Helpers.RainbowChannelExpression::get_Pos()
        IL_0013: call System.Single FrostHelper.SessionExpressions.FieldAccessCommands/Vector2LenAccessor::GetValue(Microsoft.Xna.Framework.Vector2)
        IL_0018: call Monocle.Scene Monocle.Engine::get_Scene()
        IL_001d: ldfld System.Single Monocle.Scene::TimeActive
        IL_0022: ldc.r4 50
        IL_0027: mul
        IL_0028: add
        IL_0029: ldc.i4 280
        IL_002e: ldc.i4 280
        IL_0033: div
        IL_0034: conv.r4
        IL_0035: rem
        IL_0036: call System.Single FrostHelper.SessionExpressions.FunctionCommands/YoYoFunc::Get(System.Single)
        IL_003b: ldc.r4 0.4
        IL_0040: mul
        IL_0041: add
        IL_0042: ret
        """, RainbowChannelExpression.ExpressionContext);

        Engine.Instance.scene = TestUtils.CreateLevel();
        Engine.Scene.TimeActive = 0f;

        var session = Engine.Scene.ToLevel().Session;
        var userdata = RainbowChannelExpression.Instance.Update(new Vector2(0f, 0f));
        
        Assert.Equal(0.4f, uncompiled.GetFloat(session, userdata));
        Assert.Equal(0.4f, compiled.Get(session, userdata));
        for (float p = 0; p < 1f; p += 1f / 60f) {
            Engine.Scene.TimeActive = p;
            Assert.Equal(uncompiled.GetFloat(session, userdata), compiled.Get(session, userdata));
        }
        for (float p = 0; p < 1f; p += 1f / 60f) {
            userdata = RainbowChannelExpression.Instance.Update(new Vector2(p, 0f));
            Assert.Equal(uncompiled.GetFloat(session, userdata), compiled.Get(session, userdata));
        }
        Assert.Equal(0.400012225f, uncompiled.GetFloat(session, userdata));
    }

    [Fact]
    public void LogicalOperators() {
        AssertIl<bool>("flagA && flagB", """
        IL_0000: ldarg 
        IL_0004: ldstr "flagA"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: brfalse IL_0026
        IL_0013: ldarg 
        IL_0017: ldstr "flagB"
        IL_001c: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_0021: br IL_0027
        IL_0026: ldc.i4.0
        IL_0027: ret
        """);
        
        AssertIl<bool>("flagA || flagB", """
        IL_0000: ldarg 
        IL_0004: ldstr "flagA"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: brtrue IL_0026
        IL_0013: ldarg 
        IL_0017: ldstr "flagB"
        IL_001c: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_0021: br IL_0027
        IL_0026: ldc.i4.1
        IL_0027: ret
        """);
    }
    
    [Fact]
    public void BitwiseOperators() {
        AssertIl<int>("flagA & flagB", """
        IL_0000: ldarg 
        IL_0004: ldstr "flagA"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: ldc.i4.1
        IL_000f: ceq
        IL_0011: ldarg 
        IL_0015: ldstr "flagB"
        IL_001a: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_001f: ldc.i4.1
        IL_0020: ceq
        IL_0022: and
        IL_0023: ret
        """);
        
        AssertIl<int>("flagA | flagB", """
        IL_0000: ldarg 
        IL_0004: ldstr "flagA"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: ldc.i4.1
        IL_000f: ceq
        IL_0011: ldarg 
        IL_0015: ldstr "flagB"
        IL_001a: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_001f: ldc.i4.1
        IL_0020: ceq
        IL_0022: or
        IL_0023: ret
        """);
    }

    [Fact]
    public void ComparisonOperators() {
        AssertIl<bool>("flagA > flagB", """
        IL_0000: ldarg 
        IL_0004: ldstr "flagA"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: ldc.i4.1
        IL_000f: ceq
        IL_0011: ldarg 
        IL_0015: ldstr "flagB"
        IL_001a: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_001f: ldc.i4.1
        IL_0020: ceq
        IL_0022: cgt
        IL_0024: ret
        """);
    }
    
    private void Test(Session s) {
        var test = s.GetCounter("x") <= s.GetCounter("y");
        
        Consume(test);
    }

    private void Consume(bool b) {
        
    }
}