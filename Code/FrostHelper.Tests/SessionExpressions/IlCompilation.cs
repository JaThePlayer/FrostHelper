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

    CompiledCondition<T> AssertIl<T>(string expression, string expected, ExpressionContext? context = null) {
        var flagExpr = TestUtils.CreateExpr(expression, context);
        var compiled = CompiledCondition<T>.GetFor(flagExpr);
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
        IL_0016: call T FrostHelper.SessionExpressions.OperatorDiv::Perform<System.Int32>(T,T)
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
        IL_0017: call T FrostHelper.SessionExpressions.OperatorDiv::Perform<System.Single>(T,T)
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
        IL_000a: ldc.r4 0.5
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
        
        AssertIl<bool>("!hi", """
        IL_0000: ldarg 
        IL_0004: ldstr "hi"
        IL_0009: callvirt System.Boolean Celeste.Session::GetFlag(System.String)
        IL_000e: ldnull
        IL_000f: ceq
        IL_0011: ret
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
        IL_0023: castclass FrostHelper.SessionExpressions.CounterAccessorCondition
        IL_0028: ldarg 
        IL_002c: callvirt System.Int32 FrostHelper.SessionExpressions.CounterAccessorCondition::GetCached(Celeste.Session)
        IL_0031: ldloc V_1
        IL_0035: castclass FrostHelper.Helpers.ConditionHelper/BinaryOperator
        IL_003a: ldfld FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.Helpers.ConditionHelper/BinaryOperator::ConditionB
        IL_003f: stloc V_0
        IL_0043: ldloc V_0
        IL_0047: castclass FrostHelper.SessionExpressions.CounterAccessorCondition
        IL_004c: ldarg 
        IL_0050: callvirt System.Int32 FrostHelper.SessionExpressions.CounterAccessorCondition::GetCached(Celeste.Session)
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
        IL_0014: call Microsoft.Xna.Framework.Vector2 FrostHelper.SessionExpressions.OperatorDiv::Perform(Microsoft.Xna.Framework.Vector2,System.Single)
        IL_0019: ret
        """);
        
        AssertIl<Vector2>("2 / $vec(2, 3)", """
        IL_0000: ldc.r4 2
        IL_0005: ldc.r4 2
        IL_000a: ldc.r4 3
        IL_000f: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_0014: call Microsoft.Xna.Framework.Vector2 FrostHelper.SessionExpressions.OperatorDiv::Perform(System.Single,Microsoft.Xna.Framework.Vector2)
        IL_0019: ret
        """);
        
        AssertIl<Vector2>("$vec(0, 1) / $vec(2, 3)", """
        IL_0000: ldc.r4 0
        IL_0005: ldc.r4 1
        IL_000a: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_000f: ldc.r4 2
        IL_0014: ldc.r4 3
        IL_0019: newobj System.Void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)
        IL_001e: call Microsoft.Xna.Framework.Vector2 FrostHelper.SessionExpressions.OperatorDiv::Perform(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)
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
        IL_0029: ldc.r4 280
        IL_002e: rem
        IL_002f: ldc.r4 280
        IL_0034: div
        IL_0035: call System.Single FrostHelper.SessionExpressions.FunctionCommands/YoYoFunc::Get(System.Single)
        IL_003a: ldc.r4 0.4
        IL_003f: mul
        IL_0040: add
        IL_0041: ret
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
        Assert.Equal(0.545714259f, uncompiled.GetFloat(session, userdata));
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

    [Fact]
    public void Invert() {
        AssertIl<bool>("!(1 + 2)", """
        IL_0000: ldarg.2
        IL_0001: stloc V_0
        IL_0005: ldc.i4 1
        IL_000a: ldc.i4 2
        IL_000f: add
        IL_0010: ldc.i4.0
        IL_0011: cgt.un
        IL_0013: ldnull
        IL_0014: ceq
        IL_0016: ret
        """);
    }

    [Fact]
    public void SimpleCommands() {
        AssertIl<int>("$deaths", """
        IL_0000: ldarg 
        IL_0004: ldfld System.Int32 Celeste.Session::Deaths
        IL_0009: ret
        """);
        
        AssertIl<int>("$deathsHere", """
        IL_0000: ldarg 
        IL_0004: ldfld System.Int32 Celeste.Session::DeathsInCurrentLevel
        IL_0009: ret
        """);
        
        AssertIl<string>("$roomName", """
        IL_0000: ldarg 
        IL_0004: ldfld System.String Celeste.Session::Level
        IL_0009: ret
        """);
        
        AssertIl<bool>("$photosensitive", """
        IL_0000: ldsfld Celeste.Settings Celeste.Settings::Instance
        IL_0005: ldfld System.Boolean Celeste.Settings::DisableFlashes
        IL_000a: ret
        """);
        
        AssertIl<bool>("$allowGlitch", """
        IL_0000: call Celeste.Mod.Core.CoreModuleSettings Celeste.Mod.Core.CoreModule::get_Settings()
        IL_0005: call System.Boolean Celeste.Mod.Core.CoreModuleSettings::get_AllowGlitch()
        IL_000a: ret
        """);
    }

    [Fact]
    public void EnumerableOperations() {
        AssertIl<int>("$strawberries.count", """
        IL_0000: ldarg 
        IL_0004: ldfld System.Collections.Generic.HashSet`1<Celeste.EntityID> Celeste.Session::Strawberries
        IL_0009: call System.Int32 System.Collections.Generic.HashSet`1<Celeste.EntityID>::get_Count()
        IL_000e: ret
        """);
        
        AssertIl<int>("$strawberries.sum($s => $s.roomName == \"test\")", """
        IL_0000: ldarg.2
        IL_0001: stloc V_0
        IL_0005: ldarg 
        IL_0009: ldfld System.Collections.Generic.HashSet`1<Celeste.EntityID> Celeste.Session::Strawberries
        IL_000e: ldloc V_0
        IL_0012: stloc V_1
        IL_0016: ldloc V_1
        IL_001a: castclass FrostHelper.SessionExpressions.InstanceFunctionCommands/OneArgSessionInstanceFunc`4<System.Collections.IEnumerable,FrostHelper.SessionExpressions.LambdaCondition,System.Single,FrostHelper.SessionExpressions.InstanceFunctionCommands/EnumerableSum>
        IL_001f: ldfld FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.SessionExpressions.InstanceFunctionCommands/OneArgSessionInstanceFunc`4<System.Collections.IEnumerable,FrostHelper.SessionExpressions.LambdaCondition,System.Single,FrostHelper.SessionExpressions.InstanceFunctionCommands/EnumerableSum>::_arg
        IL_0024: stloc V_0
        IL_0028: ldloc V_0
        IL_002c: castclass FrostHelper.SessionExpressions.LambdaDefinitionCondition
        IL_0031: call FrostHelper.SessionExpressions.LambdaCondition FrostHelper.SessionExpressions.LambdaDefinitionCondition::get_Instance()
        IL_0036: ldloc V_1
        IL_003a: stloc V_0
        IL_003e: ldc.r4 0
        IL_0043: stloc V_2
        IL_0047: stloc V_3
        IL_004b: callvirt System.Collections.Generic.HashSet`1/Enumerator<T> System.Collections.Generic.HashSet`1<Celeste.EntityID>::GetEnumerator()
        IL_0050: stloc V_4
        IL_0054: ldloca V_4
        IL_0058: callvirt System.Boolean System.Collections.Generic.HashSet`1/Enumerator<Celeste.EntityID>::MoveNext()
        IL_005d: brfalse IL_0129
        IL_0062: ldloc V_3
        IL_0066: ldc.i4.0
        IL_0067: ldloca V_4
        IL_006b: callvirt T System.Collections.Generic.HashSet`1/Enumerator<Celeste.EntityID>::get_Current()
        IL_0070: box Celeste.EntityID
        IL_0075: callvirt System.Void FrostHelper.SessionExpressions.LambdaCondition::SetArgument(System.Int32,System.Object)
        IL_007a: ldloc V_0
        IL_007e: stloc V_5
        IL_0082: ldloc V_3
        IL_0086: stloc V_0
        IL_008a: ldloc V_0
        IL_008e: stloc V_6
        IL_0092: ldloc V_0
        IL_0096: castclass FrostHelper.SessionExpressions.LambdaCondition
        IL_009b: ldfld FrostHelper.SessionExpressions.LambdaDefinitionCondition FrostHelper.SessionExpressions.LambdaCondition::_definition
        IL_00a0: call FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.SessionExpressions.LambdaDefinitionCondition::get_Code()
        IL_00a5: stloc V_0
        IL_00a9: ldloc V_0
        IL_00ad: stloc V_7
        IL_00b1: ldloc V_7
        IL_00b5: castclass FrostHelper.Helpers.ConditionHelper/BinaryOperator
        IL_00ba: ldfld FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.Helpers.ConditionHelper/BinaryOperator::ConditionA
        IL_00bf: stloc V_0
        IL_00c3: ldloc V_0
        IL_00c7: stloc V_8
        IL_00cb: ldloc V_8
        IL_00cf: castclass FrostHelper.SessionExpressions.GeneralFieldAccessor
        IL_00d4: ldfld FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.SessionExpressions.GeneralFieldAccessor::_target
        IL_00d9: stloc V_0
        IL_00dd: ldloc V_0
        IL_00e1: castclass FrostHelper.SessionExpressions.LambdaArgumentCondition
        IL_00e6: call System.Object FrostHelper.SessionExpressions.LambdaArgumentCondition::GetArgument()
        IL_00eb: unbox.any Celeste.EntityID
        IL_00f0: ldloc V_8
        IL_00f4: stloc V_0
        IL_00f8: ldfld System.String Celeste.EntityID::Level
        IL_00fd: ldstr "test"
        IL_0102: ldloc V_7
        IL_0106: stloc V_0
        IL_010a: call System.Boolean FrostHelper.SessionExpressions.OperatorEq::Compare(System.String,System.String)
        IL_010f: ldc.i4.1
        IL_0110: ceq
        IL_0112: conv.r4
        IL_0113: ldloc V_6
        IL_0117: stloc V_0
        IL_011b: ldloc V_2
        IL_011f: add
        IL_0120: stloc V_2
        IL_0124: br IL_0054
        IL_0129: ldloc V_2
        IL_012d: conv.i4
        IL_012e: ret
        """);
    }
    
    private void Test(Session s) {
        var test = s.GetSlider("x") != 0f;
        
        Consume(test);
    }

    private void Consume(bool b) {
        
    }
}