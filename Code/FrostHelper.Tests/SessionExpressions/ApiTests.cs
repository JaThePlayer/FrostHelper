using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;
using Xunit.Abstractions;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class ApiTests(ITestOutputHelper output) {
    [Fact]
    public void RegisterSimpleSessionExpressionCommandV2() {
        var context = (IExpressionContext)API.API.CreateSessionExpressionContextV2();
        
        API.API.RegisterSimpleSessionExpressionCommandV2("test", "three", context, [],
            static (Session session) => 3);
        API.API.RegisterSimpleSessionExpressionCommandV2("test", "staticThree", context, [], 
            Three);
        API.API.RegisterSimpleSessionExpressionCommandV2("test", "staticThreeNoArg", context, [],
            ThreeNoArg);
        
        var session = TestUtils.CreateTestSession();
        Assert.Equal(3, TestUtils.CreateExpr("$three", context).Get<int>(session));
        Assert.Equal(3, TestUtils.CreateExpr("$staticThree", context).Get<int>(session));
        Assert.Equal(3, TestUtils.CreateExpr("$staticThreeNoArg", context).Get<int>(session));
        
        TestUtils.AssertIl<int>(output, "$three", """
        IL_0000: ldc.i4 0
        IL_0005: ldc.i4 ***
        IL_000a: call System.Object MonoMod.Utils.DynamicReferenceManager::GetValue(System.Int32,System.Int32)
        IL_000f: ldarg 
        IL_0013: call System.Int32 FrostHelper.Tests.SessionExpressions.ApiTests/<>c::<RegisterSimpleSessionExpressionCommandV2>b__2_0(Celeste.Session)
        IL_0018: ret
        """, context);
        
        TestUtils.AssertIl<int>(output, "$staticThree", """
        IL_0000: ldarg 
        IL_0004: ldarg 
        IL_0008: call System.Int32 FrostHelper.Tests.SessionExpressions.ApiTests::Three(Celeste.Session,System.Object)
        IL_000d: ret
        """, context);
        
        TestUtils.AssertIl<int>(output, "$staticThreeNoArg", """
        IL_0000: call System.Int32 FrostHelper.Tests.SessionExpressions.ApiTests::ThreeNoArg()
        IL_0005: ret
        """, context);

        Assert.Throws<Exception>(() => API.API.RegisterSimpleSessionExpressionCommandV2("test", "invalidFirstArg",
            context, [], (object wrongType) => 1));
        Assert.Throws<Exception>(() => API.API.RegisterSimpleSessionExpressionCommandV2("test", "invalidThirdArg",
            context, [], (Session _, object? _, object invalidThird) => 1));
        
        
        API.API.RegisterSimpleSessionExpressionCommandV2("test", "needsUserdata", context, [], NeedsUserdata);

        var needsUserdataExpr = TestUtils.CreateHybridExpr<int>("$needsUserdata", context);
        Assert.Throws<API.API.InvalidUserdataTypeException>(() => needsUserdataExpr.GetT(session));
        Assert.Throws<API.API.InvalidUserdataTypeException>(() => needsUserdataExpr.GetTCompiled(session));
        
        TestUtils.AssertIl<int>(output, "$needsUserdata", """
        IL_0000: ldarg 
        IL_0004: ldarg 
        IL_0008: call TUserdata FrostHelper.API.API/ModApiSimpleCommandV2::ThrowInvalidUserdataExceptionIfNeeded<FrostHelper.Tests.SessionExpressions.ApiTests/TestUserdata>(System.Object)
        IL_000d: call System.Int32 FrostHelper.Tests.SessionExpressions.ApiTests::NeedsUserdata(Celeste.Session,FrostHelper.Tests.SessionExpressions.ApiTests/TestUserdata)
        IL_0012: ret
        """, context);
        
        Assert.Throws<API.API.InvalidUserdataTypeException>(() => needsUserdataExpr.GetT(session, userdata: 3));
        Assert.Throws<API.API.InvalidUserdataTypeException>(() => needsUserdataExpr.GetTCompiled(session, userdata: 3));
        
        Assert.Equal(42, needsUserdataExpr.GetT(session, new TestUserdata(42)));
    }

    private static int NeedsUserdata(Session _, TestUserdata userdata) {
        return userdata.Value;
    }

    record TestUserdata(int Value);
    
    private static int Three(Session session, object? userdata) {
        return 3;
    }
    
    private static int ThreeNoArg() {
        return 3;
    }


    [Fact]
    public void RegisterFunctionSessionExpressionCommandV2() {
        var session = TestUtils.CreateTestSession();
        var context = (IExpressionContext)API.API.CreateSessionExpressionContextV2();
        
        Assert.Throws<Exception>(() => API.API.RegisterFunctionSessionExpressionCommandV2("test", "notEnoughArgs",
            context, [], static () => 1));
        Assert.Throws<Exception>(() => API.API.RegisterFunctionSessionExpressionCommandV2("test", "notEnoughArgs",
            context, [], static (Scene scene) => 1));
        Assert.Throws<Exception>(() => API.API.RegisterFunctionSessionExpressionCommandV2("test", "wrongFirstArg",
            context, [], static (object _, object? userdata) => 1));
        
        API.API.RegisterFunctionSessionExpressionCommandV2("test", "one", context, [], 
            static (Session session, object? userdata) => 1);
        
        Assert.Equal(1, TestUtils.CreateExpr("$one()", context).Get<int>(session));
        
        TestUtils.AssertIl<int>(output, "$one()", """
        IL_0000: ldc.i4 2
        IL_0005: ldc.i4 ***
        IL_000a: call System.Object MonoMod.Utils.DynamicReferenceManager::GetValue(System.Int32,System.Int32)
        IL_000f: ldarg 
        IL_0013: ldarg 
        IL_0017: call System.Int32 FrostHelper.Tests.SessionExpressions.ApiTests/<>c::<RegisterFunctionSessionExpressionCommandV2>b__7_3(Celeste.Session,System.Object)
        IL_001c: ret
        """, context);
        
        API.API.RegisterFunctionSessionExpressionCommandV2("test", "sub", context, [], 
            static (Session session, object? userdata, int left, int right) => left - right);
        
        Assert.Equal(2, TestUtils.CreateExpr("$sub(3, 1)", context).Get<int>(session));
        TestUtils.AssertIl<int>(output, "$sub(3, 1)", """
        IL_0000: ldc.i4 4
        IL_0005: ldc.i4 ***
        IL_000a: call System.Object MonoMod.Utils.DynamicReferenceManager::GetValue(System.Int32,System.Int32)
        IL_000f: ldarg 
        IL_0013: ldarg 
        IL_0017: ldc.i4 3
        IL_001c: ldc.i4 1
        IL_0021: call System.Int32 FrostHelper.Tests.SessionExpressions.ApiTests/<>c::<RegisterFunctionSessionExpressionCommandV2>b__7_4(Celeste.Session,System.Object,System.Int32,System.Int32)
        IL_0026: ret
        """, context);
        
        Assert.True(context.TryGetFunctionCommand("sub", out var subCommand));
        Assert.Equal(2, subCommand.Descriptor.Arguments.Count);
        Assert.Equal("left", subCommand.Descriptor.Arguments[0].Name);
        Assert.Equal("right", subCommand.Descriptor.Arguments[1].Name);
        
        
        // Arguments get coerced
        API.API.RegisterFunctionSessionExpressionCommandV2("test", "sumColor", context, [], SumColor);
        Assert.Equal(new Color(255, 255, 0, 255), TestUtils.CreateExpr($"$sumColor(\"red\", {0x00ff00ff})", context).Get<Color>(session));
        TestUtils.AssertIl<Color>(output, $"$sumColor(\"red\", {0x00ff00ff})", """
        IL_0000: ldarg 
        IL_0004: ldarg 
        IL_0008: ldstr "red"
        IL_000d: call Microsoft.Xna.Framework.Color FrostHelper.ColorHelper::GetColor(System.String)
        IL_0012: ldc.i4 16711935
        IL_0017: call Microsoft.Xna.Framework.Color FrostHelper.ColorHelper::HexToColorInt(System.Int32)
        IL_001c: call Microsoft.Xna.Framework.Color FrostHelper.Tests.SessionExpressions.ApiTests::SumColor(Celeste.Session,System.Object,Microsoft.Xna.Framework.Color,Microsoft.Xna.Framework.Color)
        IL_0021: ret
        """, context);
        
        // Make sure getting conditions instances works in IL.
        context.RegisterSimpleCommand("uncompileable", new UncompileableCondition(Color.Red));
        Assert.Equal(new Color(255, 255, 0, 255), TestUtils.CreateExpr($"$sumColor($uncompileable, {0x00ff00ff})", context).Get<Color>(session));
        TestUtils.AssertIl<Color>(output, $"$sumColor($uncompileable, {0x00ff00ff})", """
        IL_0000: ldarg.2
        IL_0001: stloc V_0
        IL_0005: ldarg 
        IL_0009: ldarg 
        IL_000d: ldloc V_0
        IL_0011: stloc V_1
        IL_0015: ldloc V_1
        IL_0019: ldc.i4 0
        IL_001e: call FrostHelper.Helpers.ConditionHelper/Condition FrostHelper.API.API/ModFunctionConditionV2::GetArg(System.Int32)
        IL_0023: stloc V_0
        IL_0027: ldloc V_0
        IL_002b: ldarg 
        IL_002f: ldarg 
        IL_0033: callvirt T FrostHelper.Helpers.ConditionHelper/Condition::Get<Microsoft.Xna.Framework.Color>(Celeste.Session,System.Object)
        IL_0038: ldc.i4 16711935
        IL_003d: call Microsoft.Xna.Framework.Color FrostHelper.ColorHelper::HexToColorInt(System.Int32)
        IL_0042: ldloc V_1
        IL_0046: stloc V_0
        IL_004a: call Microsoft.Xna.Framework.Color FrostHelper.Tests.SessionExpressions.ApiTests::SumColor(Celeste.Session,System.Object,Microsoft.Xna.Framework.Color,Microsoft.Xna.Framework.Color)
        IL_004f: ret
        """, context);

        Assert.True(context.TryGetFunctionCommand("sumColor", out var sumColorCommand));
        Assert.Equal(2, sumColorCommand.Descriptor.Arguments.Count);
        Assert.Equal("left", sumColorCommand.Descriptor.Arguments[0].Name);
        Assert.Equal("right", sumColorCommand.Descriptor.Arguments[1].Name);
    }

    private static Color SumColor(Session session, object? userdata, Color left, Color right) {
        return new Color(left.R + right.R, left.G + right.G, left.B + right.B);
    }
    
    class UncompileableCondition(object value) : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            return value;
        }
    }

    [Fact]
    public void CreateTypedSessionExpressionOrNull() {
        var threePlusFour = Assert.IsType<Func<Session, object?, int>>(API.API.CreateTypedSessionExpressionOrNull("3 + #four", null, typeof(int)));

        var session = TestUtils.CreateTestSession();
        session.SetCounter("four", 4);
        Assert.Equal(7, threePlusFour(session, null));
        Assert.Equal("FrostHelper.SessionExpressions.CompiledCondition`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]::Get", threePlusFour.Method.GetID(simple: true));
        
        // Return value is coerced automatically.
        var color = Assert.IsType<Func<Session, object?, Color>>(API.API.CreateTypedSessionExpressionOrNull("\"red\"", null, typeof(Color)));
        Assert.Equal(Color.Red, color(session, null));
    }
}