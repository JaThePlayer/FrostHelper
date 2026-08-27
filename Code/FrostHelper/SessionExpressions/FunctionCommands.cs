using FrostHelper.API;
using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using static FrostHelper.Helpers.ConditionHelper;
using Vector2 = Microsoft.Xna.Framework.Vector2;

using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

public delegate bool FunctionCommandFactory(
    IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? result, 
    [NotNullWhen(false)] out string? errorMessage);

public record FunctionCommand(FunctionCommandFactory Factory, CommandDescriptor Descriptor);

internal static class FunctionCommands {
    static FunctionCommands() {
        Register("min", [ ArgumentDescriptor.VarargFor(TypeDescriptor.For(typeof(float))) ], TypeDescriptor.For(typeof(float)),
            [ ApiRenderPart.Default("Returns the smallest value from all provided arguments.") ], MinCondition.TryCreate);
        
        Register("max", [ ArgumentDescriptor.VarargFor(TypeDescriptor.For(typeof(float))) ], TypeDescriptor.For(typeof(float)),
            [ ApiRenderPart.Default("Returns the largest value from all provided arguments.") ], MaxCondition.TryCreate);

        RegisterPure<int, int, int, int, ClampFunc<int>>("clamp", [ ApiRenderPart.Default("Clamps the value of x so that its between min and max.") ]);
        RegisterPure<float, float, AbsFunc<float>>("abs", [ ApiRenderPart.Default("Returns the absolute value of x.") ]);
        
        RegisterPure<float, float, SinFunc>("sin", [ ApiRenderPart.Default("Calculates trigonometrical functions, x is assumed to be in radians. (Tip: $pi)") ]);
        RegisterPure<float, float, CosFunc>("cos", [ ApiRenderPart.Default("Calculates trigonometrical functions, x is assumed to be in radians. (Tip: $pi)") ]);
        RegisterPure<float, float, TanFunc>("tan", [ ApiRenderPart.Default("Calculates trigonometrical functions, x is assumed to be in radians. (Tip: $pi)") ]);
        
        RegisterPure<float, float, TruncateFunc>("truncate", [ ApiRenderPart.Default("Truncates the value.") ]);
        RegisterPure<float, float, RoundFunc>("round", [ ApiRenderPart.Default("Rounds the value.") ]);
        
        RegisterPure<float, float, float, PowFunc<float>>("pow", [ ApiRenderPart.Default("x raised to the power of y.") ]);
        RegisterPure<float, float, Pow2Func<float>>("pow2", [ ApiRenderPart.Default("x raised to the power of 2.") ]);
        
        RegisterPure<float, float, SqrtFunc>("sqrt", [ ApiRenderPart.Default("x raised to the power of 2.") ]);
        RegisterPure<float, float, CbrtFunc>("cbrt", [ ApiRenderPart.Default("Square root of x.") ]);
        RegisterPure<float, float, ExpFunc>("exp", [ ApiRenderPart.Default("Cube root of x.") ]);
        RegisterPure<float, float, Exp2Func>("exp2", [ ApiRenderPart.Default("$e raised to the power of x") ]);
        
        RegisterPure<float, float, float, LogFunc<float>>("log", [ ApiRenderPart.Default("The base-y logarithm of x.") ]);
        RegisterPure<float, float, LognFunc>("logn", [ ApiRenderPart.Default("The natural logarithm of x.") ]);
        RegisterPure<float, float, Log2Func>("log2", [ ApiRenderPart.Default("The base-2 logarithm of x.") ]);
        RegisterPure<float, float, Log10Func>("log10", [ ApiRenderPart.Default("The base-10 logarithm of x.") ]);
        
        RegisterPure<float, float, float, float, LerpFunc>("lerp", [ ApiRenderPart.Default("Performs a linear interpolation between two values based on the given weight. Params: x — The first value, which is intended to be the lower bound. y — The second value, which is intended to be the upper bound. amount — A value between 0 and 1, that indicates the weight of the interpolation.") ]);
        
        RegisterPure<float, float, YoYoFunc>("yoyo", [ ApiRenderPart.Default("x <= 0.5 ? x * 2 : 1.0 - (value - 0.5) * 2.0).") ]);
        
        RegisterPure<int, int, IEnumerable<int>, RangeFunc>("range", [
            ApiRenderPart.Default("Creates a "),
            ApiRenderPart.Type(TypeDescriptor.For(typeof(IEnumerable<int>))),
            ApiRenderPart.Default(" containing numbers between min (inclusive) and max (exclusive).")
        ]);
        
        RegisterSession<string, IEnumerable<string>, FlagsFunc>("flags", [
            ApiRenderPart.Default("Creates a "),
            ApiRenderPart.Type(TypeDescriptor.For(typeof(IEnumerable<string>))),
            ApiRenderPart.Default(" containing all currently set flags matching the given regex.")
        ]);
        
        RegisterPure<int, int, int, Color, RgbFunc>("rgb", [
            ApiRenderPart.Default("Creates a "),
            ApiRenderPart.Type(TypeDescriptor.For(typeof(Color))),
            ApiRenderPart.Default(" using r, g, b values, assumed to be in range 0-255.")
        ]);
        
        RegisterPure<float, float, float, Color, HsvFunc>("hsv", [
            ApiRenderPart.Default("Creates a "),
            ApiRenderPart.Type(TypeDescriptor.For(typeof(Color))),
            ApiRenderPart.Default(" using h, s, v values, assumed to be in range 0-1.")
        ]);
        
        RegisterPure<string, string, DialogFunc>("dialog", [
            ApiRenderPart.Default("Gets the dialog text in the current language for the given dialogId."),
        ]);
        
        Register("vec", [
                new ArgumentDescriptor("x", TypeDescriptor.For(typeof(float))),
                new ArgumentDescriptor("y", TypeDescriptor.For(typeof(float)))
            ],
            TypeDescriptor.For(typeof(Vector2)),
            [
                ApiRenderPart.Default("Creates a "),
                ApiRenderPart.Type(TypeDescriptor.For(typeof(Vector2))),
                ApiRenderPart.Default(" with the given x, y values.")
            ], VecCondition.TryCreate);
    }
    
    private static readonly Dictionary<string, FunctionCommand> Registry = new();

    private static void Register(string name, IReadOnlyList<ArgumentDescriptor> arguments, TypeDescriptor returnType, IReadOnlyList<ApiRenderPart> description, FunctionCommandFactory factory) {
        Registry[name] = new FunctionCommand(factory, new CommandDescriptor {
            Name = name,
            Description = description,
            Arguments = arguments,
            ReturnType = returnType,
        });
    }

    private static void RegisterPure<TArg1, TRet, TOp>(string name, IReadOnlyList<ApiRenderPart> description)
        where TOp : struct, IPureFunc<TArg1, TRet> {
        Register(name, [
                new ArgumentDescriptor(TOp.ArgName, TypeDescriptor.For(typeof(TArg1)))
            ],
            TypeDescriptor.For(typeof(TRet)),
            description,
            PureMathCondition.TryCreate<TArg1, TRet, TOp>);
    }
    
    private static void RegisterSession<TArg1, TRet, TOp>(string name, IReadOnlyList<ApiRenderPart> description)
        where TOp : struct, ISessionFunc<TArg1, TRet> {
        Register(name, [
                new ArgumentDescriptor(TOp.ArgName, TypeDescriptor.For(typeof(TArg1)))
            ],
            TypeDescriptor.For(typeof(TRet)),
            description,
            PureMathCondition.TryCreateSession<TArg1, TRet, TOp>);
    }
    
    private static void RegisterPure<TArg1, TArg2, TRet, TOp>(string name, IReadOnlyList<ApiRenderPart> description)
        where TOp : struct, IPureFunc<TArg1, TArg2, TRet> {
        Register(name, [
                new ArgumentDescriptor(TOp.Arg1Name, TypeDescriptor.For(typeof(TArg1))),
                new ArgumentDescriptor(TOp.Arg2Name, TypeDescriptor.For(typeof(TArg2))),
            ],
            TypeDescriptor.For(typeof(TRet)),
            description,
            PureMathCondition.TryCreate<TArg1, TArg2, TRet, TOp>);
    }
    
    private static void RegisterPure<TArg1, TArg2, TArg3, TRet, TOp>(string name, IReadOnlyList<ApiRenderPart> description)
        where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TRet> {
        Register(name, [ 
                new ArgumentDescriptor(TOp.Arg1Name, TypeDescriptor.For(typeof(TArg1))),
                new ArgumentDescriptor(TOp.Arg2Name, TypeDescriptor.For(typeof(TArg2))),
                new ArgumentDescriptor(TOp.Arg3Name, TypeDescriptor.For(typeof(TArg3)))
            ],
            TypeDescriptor.For(typeof(TRet)),
            description,
            PureMathCondition.TryCreate<TArg1, TArg2, TArg3, TRet, TOp>);
    }
    

    internal struct RangeFunc : IPureFunc<int, int, IEnumerable<int>> {
        public static IEnumerable<int> Get(int min, int count) {
            return Enumerable.Range(min, count);
        }

        public static string Arg1Name => "min";
        
        public static string Arg2Name => "max";
    }

    public static void Register(string modName, string cmdName, Func<Session, object?, IReadOnlyList<object>, object> func) {
        var key = $"{modName}.{cmdName}";
        if (Registry.TryGetValue(key, out var existing)) {
            Logger.Warn("FrostHelper.ConditionHelper", $"Replacing function command '${key}'");
        }

        Registry[key] = new FunctionCommand(CreateFactoryForCustomCommand(func), new CommandDescriptor {
            Name = key,
            DeclaringMod = modName
        });
    }

    internal static FunctionCommandFactory CreateFactoryForCustomCommand(Func<Session, object?, IReadOnlyList<object>, object> func) {
        return (args, out result, out message) => {
            result = new ModFunctionCondition(args, func);
            message = null;
            return true;
        };
    }
    
    public static bool TryCreate(string name, IReadOnlyList<Condition> args, IExpressionContext ctx, [NotNullWhen(true)] out Condition? condition) {
        if (!ctx.TryGetFunctionCommand(name, out var functionCommand) && !Registry.TryGetValue(name, out functionCommand)) {
            if (name.Contains('.')) {
                var remaining = name;
                List<string>? fields = null;
                condition = null;
                while (true) {
                    // Try simple commands from the context
                    if (ctx.TryGetSimpleCommand(remaining, out var cond)) {
                        condition = cond;
                        break;
                    }
                    
                    // Try simple commands
                    if (SimpleCommands.Registry.TryGetValue(remaining, out var simpleCommand)) {
                        condition = simpleCommand.Condition;
                        break;
                    }
                    
                    var lastDotIdx = remaining.LastIndexOf('.');
                    if (lastDotIdx == -1)
                        break;
                    fields ??= [];
                    fields.Add(remaining[(lastDotIdx+1)..]);
                    remaining = remaining[..lastDotIdx];
                }

                if (condition is not null) {
                    while (fields?.Count > 1) {
                        condition = FieldAccessCommands.Create(fields[^1], condition, ctx);
                        fields.RemoveAt(fields.Count - 1);
                    }

                    if (fields?.Count > 0) {
                        condition = InstanceFunctionCommands.Create(fields[0], condition, args, ctx);
                        return true;
                    }
                    
                }
            }
            
            NotificationHelper.Notify($"Unknown Session Expression function: '{name}'");
            condition = null;
            return false;
        }

        if (!functionCommand.Factory(args, out condition, out var errorMessage)) {
            NotificationHelper.Notify($"Failed to create Session Expression function: '{name}':\n{errorMessage}");
            condition = null;
            return false;
        }

        condition.Descriptor = functionCommand.Descriptor;

        return true;
    }

    private sealed class LazyFunctionArgumentList(IReadOnlyList<Condition> args) : IReadOnlyList<object> {
        public Session Session { get; set; }
        public object? UserData { get; set; }
        
        private readonly object?[] _cache = new object[args.Count];

        public void Reset(Session session, object? userdata) {
            Array.Clear(_cache);
            Session = session;
            UserData = userdata;
        }
        
        public IEnumerator<object> GetEnumerator()
            => args.Select(x => x.Get(Session, UserData)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count => args.Count;

        public object this[int index] 
            => args[index].Get(Session, UserData);
    }

    private sealed class ModFunctionCondition(IReadOnlyList<Condition> args, Func<Session, object?, IReadOnlyList<object>, object> func) 
        : FunctionCondition(args) {
        private readonly object[] _array = new object[args.Count];
        //private readonly LazyFunctionArgumentList _args = new(args);
        
        public override object Get(Session session, object? userdata) {
            for (int i = 0; i < args.Count; i++) {
                _array[i] = args[i].Get(session, userdata);
            }
            
            return func(session, userdata, _array);
            // TODO: test!
            //_args.Reset(session, userdata);
            //return func(session, userdata, _args);
        }
    }

    private interface IPureFunc<in TArg1, out TRet> {
        public static abstract TRet Get(TArg1 arg1);

        public static abstract string ArgName { get; }
    }
    
    private interface ISessionFunc<in TArg1, out TRet> {
        public static abstract TRet Get(Session session, TArg1 arg1);
        
        public static abstract string ArgName { get; }
    }
    
    private interface IPureFunc<in TArg1, in TArg2, out TRet> {
        public static abstract TRet Get(TArg1 arg1, TArg2 arg2);

        public static abstract string Arg1Name { get; }

        public static abstract string Arg2Name { get; }
    }
    
    private interface IPureFunc<in TArg1, in TArg2, in TArg3, out TRet> {
        public static abstract TRet Get(TArg1 arg1, TArg2 arg2, TArg3 arg3);
        
        public static abstract string Arg1Name { get; }

        public static abstract string Arg2Name { get; }
        
        public static abstract string Arg3Name { get; }
    }

    private interface IPureMathFunc<T> : IPureFunc<T, T> where T : struct, INumber<T>;

    private interface IPureTwoArgMathFunc<T> : IPureFunc<T, T, T> where T : struct, INumber<T>;
    
    private interface IPureThreeArgMathFunc<T> : IPureFunc<T, T, T, T> where T : struct, INumber<T>;
    
    private struct SinFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Sin(x);
        
        public static string ArgName => "x";
    }
    
    private struct CosFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Cos(x);
        
        public static string ArgName => "x";
    }
    
    private struct TanFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Tan(x);
        
        public static string ArgName => "x";
    }
    
    private struct TruncateFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Truncate(x);
        
        public static string ArgName => "x";
    }
    
    private struct SqrtFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Sqrt(x);
        
        public static string ArgName => "x";
    }
    
    private struct CbrtFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Cbrt(x);
        
        public static string ArgName => "x";
    }
    
    private struct ExpFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Exp(x);
        
        public static string ArgName => "x";
    }
    
    private struct Exp2Func : IPureMathFunc<float> {
        public static float Get(float x) => float.Exp2(x);
        
        public static string ArgName => "x";
    }
    
    private struct LognFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Log(x);
        
        public static string ArgName => "x";
    }
    
    private struct Log10Func : IPureMathFunc<float> {
        public static float Get(float x) => float.Log10(x);
        
        public static string ArgName => "x";
    }
    
    private struct Log2Func : IPureMathFunc<float> {
        public static float Get(float x) => float.Log2(x);
        
        public static string ArgName => "x";
    }
    
    private struct RoundFunc : IPureMathFunc<float> {
        public static float Get(float x) => float.Round(x);
        
        public static string ArgName => "x";
    }
    
    private struct AbsFunc<T> : IPureMathFunc<T> where T : struct, INumber<T> {
        public static T Get(T x) => T.Abs(x);
        
        public static string ArgName => "x";
    }

    private struct PowFunc<T> : IPureTwoArgMathFunc<T> where T : struct, INumber<T>, IPowerFunctions<T> {
        public static T Get(T x, T y) => T.Pow(x, y);

        public static string Arg1Name => "x";

        public static string Arg2Name => "y";
    }
    
    private struct Pow2Func<T> : IPureMathFunc<T> where T : struct, INumber<T> {
        public static T Get(T x) => x * x;
        
        public static string ArgName => "x";
    }
    
    private struct LogFunc<T> : IPureTwoArgMathFunc<T> where T : struct, INumber<T>, ILogarithmicFunctions<T> {
        public static T Get(T x, T y) => T.Log(x, y);

        public static string Arg1Name => "x";

        public static string Arg2Name => "y";
    }
    
    private struct LerpFunc : IPureThreeArgMathFunc<float> {
        public static float Get(float x, float y, float z) => float.Lerp(x, y, z);
        
        public static string Arg1Name => "x";

        public static string Arg2Name => "y";

        public static string Arg3Name => "amount";
    }
    
    private struct YoYoFunc : IPureMathFunc<float> {
        public static float Get(float x) => Calc.YoYo(x);
        
        public static string ArgName => "x";
    }

    private struct FlagsFunc : ISessionFunc<string, IEnumerable<string>> {
        public static IEnumerable<string> Get(Session session, string regex) {
            return session.Flags.Where(f => Regex.IsMatch(f, regex, RegexOptions.Compiled));
        }

        public static string ArgName => "regex";
    }
    
    private struct RgbFunc : IPureFunc<int, int, int, Color> {
        public static Color Get(int r, int g, int b) {
            return new Color(r, g, b);
        }

        public static string Arg1Name => "r";
        
        public static string Arg2Name => "g";
        
        public static string Arg3Name => "b";
    }
    
    private struct HsvFunc : IPureFunc<float, float, float, Color> {
        public static Color Get(float h, float s, float v) {
            return Calc.HsvToColor(h, s, v);
        }
        
        public static string Arg1Name => "h";
        
        public static string Arg2Name => "s";
        
        public static string Arg3Name => "v";
    }
    
    private struct DialogFunc : IPureFunc<string, string> {
        public static string Get(string dialogId) => Dialog.Clean(dialogId);
        
        public static string ArgName => "dialogId";
    }

    private sealed class PureMathCondition<TArg, TRet, TOp>(Condition x) : FunctionCondition(x)
        where TOp : struct, IPureFunc<TArg, TRet> {

        private readonly Condition _innerCondition = x;

        private static readonly FieldInfo InnerConditionFieldInfo
            = typeof(PureMathCondition<TArg, TRet, TOp>).GetField(nameof(_innerCondition),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        public override object Get(Session session, object? userdata) {
            return TOp.Get(_innerCondition.Get<TArg>(session, userdata))!;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _innerCondition, InnerConditionFieldInfo);
            
            _innerCondition.Emit(ctx, typeof(TArg));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            
            il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Get))!);
            il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => _innerCondition.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(TRet);
    }
    
    private sealed class Session1ArgCondition<TArg, TRet, TOp>(Condition x) : FunctionCondition(x)
        where TOp : struct, ISessionFunc<TArg, TRet> {

        private readonly Condition _innerCondition = x;

        private static readonly FieldInfo InnerConditionFieldInfo
            = typeof(Session1ArgCondition<TArg, TRet, TOp>).GetField(nameof(_innerCondition),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        public override object Get(Session session, object? userdata) {
            return TOp.Get(session, _innerCondition.Get<TArg>(session, userdata))!;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            
            ctx.EmitLoadSession();
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _innerCondition, InnerConditionFieldInfo);
            _innerCondition.Emit(ctx, typeof(TArg));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            
            il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Get))!);
            il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => _innerCondition.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(TRet);
    }
    
    private sealed class PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>(Condition x, Condition y) : FunctionCondition(x)
        where TOp : struct, IPureFunc<TArg1, TArg2, TRet> {
        
        private readonly Condition _x = x;
        private readonly Condition _y = y;

        private static readonly FieldInfo XFieldInfo
            = typeof(PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>).GetField(nameof(_x),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        private static readonly FieldInfo YFieldInfo
            = typeof(PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>).GetField(nameof(_y),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        public override object Get(Session session, object? userdata) {
            return TOp.Get(_x.Get<TArg1>(session, userdata), _y.Get<TArg2>(session, userdata))!;
        }
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _x, XFieldInfo);
            _x.Emit(ctx, typeof(TArg1));
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _y, YFieldInfo);
            _y.Emit(ctx, typeof(TArg2));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            
            il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Get))!);
            il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
        }
        
        internal override bool UsesCurrentConditionLocalInEmit => _x.UsesCurrentConditionLocalInEmit || _y.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(TRet);
    }
    
    private sealed class PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>(Condition x, Condition y, Condition z) : FunctionCondition(x)
        where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TRet> {
        private readonly Condition _x = x;
        private readonly Condition _y = y;
        private readonly Condition _z = z;

        private static readonly FieldInfo XFieldInfo
            = typeof(PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>).GetField(nameof(_x),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        private static readonly FieldInfo YFieldInfo
            = typeof(PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>).GetField(nameof(_y),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        private static readonly FieldInfo ZFieldInfo
            = typeof(PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>).GetField(nameof(_z),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        public override object Get(Session session, object? userdata) {
            return TOp.Get(
                _x.Get<TArg1>(session, userdata), 
                _y.Get<TArg2>(session, userdata), 
                _z.Get<TArg3>(session, userdata))!;
        }
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _x, XFieldInfo);
            _x.Emit(ctx, typeof(TArg1));
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _y, YFieldInfo);
            _y.Emit(ctx, typeof(TArg2));
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _z, ZFieldInfo);
            _z.Emit(ctx, typeof(TArg3));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            
            il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Get))!);
            il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
        }
        
        internal override bool UsesCurrentConditionLocalInEmit => _x.UsesCurrentConditionLocalInEmit 
                                                               || _y.UsesCurrentConditionLocalInEmit
                                                               || _z.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(TRet);
    }

    private static class PureMathCondition {
        public static bool TryCreateIntOrFloat<TInt, TFloat>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TInt : struct, IPureMathFunc<int>
            where TFloat : struct, IPureMathFunc<float>
        {
            if (args is not [var only]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            if (only.ReturnType == typeof(int)) {
                return FunctionCondition.Ok(new PureMathCondition<int, int, TInt>(only), out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathCondition<float, float, TFloat>(only), out condition, out errorMessage);
        }
        
        public static bool TryCreateFloat<TFloat>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TFloat : struct, IPureMathFunc<float>
        {
            if (args is not [var only]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathCondition<float, float, TFloat>(only), out condition, out errorMessage);
        }
        
        public static bool TryCreateTwoArgFloat<TFloat>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TFloat : struct, IPureTwoArgMathFunc<float>
        {
            if (args is not [var left, var right]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathTwoArgCondition<float, float, float, TFloat>(left, right), out condition, out errorMessage);
        }
        
        public static bool TryCreateTwoArg<TArg1, TArg2, TRet, TFunc>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TFunc : struct, IPureFunc<TArg1, TArg2, TRet>
        {
            if (args is not [var left, var right]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathTwoArgCondition<TArg1, TArg2, TRet, TFunc>(left, right), out condition, out errorMessage);
        }
        
        public static bool TryCreate<TArg1, TRet, TFunc>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TFunc : struct, IPureFunc<TArg1, TRet>
        {
            if (args is not [var only]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathCondition<TArg1, TRet, TFunc>(only), out condition, out errorMessage);
        }
        
        public static bool TryCreateSession<TArg1, TRet, TFunc>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TFunc : struct, ISessionFunc<TArg1, TRet>
        {
            if (args is not [var only]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new Session1ArgCondition<TArg1, TRet, TFunc>(only), out condition, out errorMessage);
        }
        
        public static bool TryCreatThreeArgFloat<TFloat>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TFloat : struct, IPureThreeArgMathFunc<float>
        {
            if (args is not [var a, var b, var c]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 3, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathThreeArgCondition<float, float, float, float, TFloat>(a, b, c), out condition, out errorMessage);
        }
        
        public static bool TryCreate<TArg1, TArg2, TRet, TOp>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TOp : struct, IPureFunc<TArg1, TArg2, TRet>
        {
            if (args is not [var a, var b]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 3, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>(a, b), out condition, out errorMessage);
        }
        
        public static bool TryCreate<TArg1, TArg2, TArg3, TRet, TOp>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TRet>
        {
            if (args is not [var a, var b, var c]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 3, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>(a, b, c), out condition, out errorMessage);
        }
    }

    private sealed class MinCondition(IEnumerable<Condition> x, Type type) : FunctionCondition(x) {
        private T GetImpl<T>(Session session, object? userdata) where T : struct, INumber<T>, IMinMaxValue<T> {
            T min = T.MaxValue;
            foreach (var c in Conditions) {
                min = T.Min(c.GetNumber<T>(session, userdata), min);
            }

            return min;
        }

        protected internal override Type ReturnType => type;

        public override object Get(Session session, object? userdata) {
            if (ReturnType == typeof(int))
                return GetImpl<int>(session, userdata);
            return GetImpl<float>(session, userdata);
        }
        
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [_, ..]) {
                return TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }

            if (args.All(x => x.ReturnType == typeof(int))) {
                return Ok(new MinCondition(args, typeof(int)), out condition, out errorMessage);
            }

            return Ok(new MinCondition(args, typeof(float)), out condition, out errorMessage);
        }
    }
    
    private sealed class MaxCondition(IEnumerable<Condition> x, Type type) : FunctionCondition(x) {
        private T GetImpl<T>(Session session, object? userdata) where T : struct, INumber<T>, IMinMaxValue<T> {
            T max = T.MinValue;
            foreach (var c in Conditions) {
                max = T.Max(c.GetNumber<T>(session, userdata), max);
            }

            return max;
        }

        protected internal override Type ReturnType => type;

        public override object Get(Session session, object? userdata) {
            if (ReturnType == typeof(int))
                return GetImpl<int>(session, userdata);
            return GetImpl<float>(session, userdata);
        }
        
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [_, ..]) {
                return TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }

            if (args.All(x => x.ReturnType == typeof(int))) {
                return Ok(new MaxCondition(args, typeof(int)), out condition, out errorMessage);
            }

            return Ok(new MaxCondition(args, typeof(float)), out condition, out errorMessage);
        }
    }
    
    private sealed class VecCondition(Condition x, Condition y) : FunctionCondition(x, y) {
        private readonly Condition _x = x;
        private readonly Condition _y = y;
        
        private static readonly FieldInfo XFieldInfo
            = typeof(VecCondition).GetField(nameof(_x),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        private static readonly FieldInfo YFieldInfo
            = typeof(VecCondition).GetField(nameof(_y),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        protected internal override Type ReturnType => typeof(Vector2);

        public override object Get(Session session, object? userdata) {
            
            return new Vector2(_x.GetFloat(session, userdata), _y.GetFloat(session, userdata));
        }
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _x, XFieldInfo);
            _x.Emit(ctx, typeof(float));
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _y, YFieldInfo);
            _y.Emit(ctx, typeof(float));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            
            il.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor([typeof(float), typeof(float)])!);
            il.EmitConvertToInSessionExpression(typeof(Vector2), targetType);
        }
        
        internal override bool UsesCurrentConditionLocalInEmit => _x.UsesCurrentConditionLocalInEmit || _y.UsesCurrentConditionLocalInEmit;
        
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var x, var y]) {
                return ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
            }

            return Ok(new VecCondition(x, y), out condition, out errorMessage);
        }
    }
    
    private struct ClampFunc<T> : IPureFunc<T, T, T, T> where T : INumber<T> {
        public static T Get(T xVal, T minVal, T maxVal) {
            if (minVal > maxVal)
                return T.Clamp(xVal, maxVal, minVal);
            return T.Clamp(xVal, minVal, maxVal);
        }

        public static string Arg1Name => "x";
        
        public static string Arg2Name => "minVal";
        
        public static string Arg3Name => "maxVal";
    }

    private sealed class DialogCondition(Condition key) : FunctionCondition(key) {
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var x]) {
                return TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }
            
            return Ok(new DialogCondition(x), out condition, out errorMessage);
        }

        public override object Get(Session session, object? userdata) {
            var name = key.GetString(session, userdata);

            return Dialog.Clean(name);
        }

        protected internal override Type ReturnType => typeof(string);
    }
    
    internal abstract class FunctionCondition : Condition {
        protected readonly Condition[] Conditions;

        public FunctionCondition(params IEnumerable<Condition> conditions) {
            Conditions = conditions.ToArray();
        }

        protected override IEnumerable<object> GetArgsForDebugPrint() => Conditions;

        protected internal static bool ArgumentAmtMismatch(int received, int expected, 
            out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            
            condition = null;
            if (received > expected) {
                
                errorMessage = $"Too many arguments: {received}, expected: {expected}";
                return false;
            }

            if (received < expected) {
                errorMessage = $"Too few arguments: {received}, expected: {expected}";
                return false;
            }

            errorMessage = null;
            return true;
        }
        
        protected internal static bool TooFewArgs(int received, int expected, 
            out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            condition = null;
            if (received < expected) {
                errorMessage = $"Too few arguments: {received}, expected: {expected}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        protected internal static bool Ok(Condition condition, [NotNullWhen(true)] out Condition? retCond, [NotNullWhen(false)] out string? errorMessage) {
            retCond = condition;
            errorMessage = null;
            return true;
        }
    }
}
