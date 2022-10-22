using Mono.Cecil;

namespace FrostHelper;

public static class EasierILHook {
    public static void ReplaceStrings(ILCursor cursor, Dictionary<string, string> toReplace) {
        int lastIndex = cursor.Index;
        cursor.Index = 0;
        while (cursor.TryGotoNext(MoveType.After, instr => MatchStringInDict(instr, toReplace.Keys.ToList()))) {
            string old = (string) cursor.Prev.Operand;
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldstr, toReplace[old]);
        }
        cursor.Index = lastIndex;
    }

    public static void ReplaceInts(ILCursor cursor, Dictionary<int, int> toReplace) {
        int lastIndex = cursor.Index;
        cursor.Index = 0;
        int replacement = -1;
        while (cursor.TryGotoNext(MoveType.After, instr => MatchIntInDict(instr, toReplace, out replacement))) {
            /*
                int old = -1;
                if (cursor.Prev.Operand.GetType() == typeof(int))
                    old = (int)cursor.Prev.Operand;
                else
                    old = (int)(sbyte)cursor.Prev.Operand;*/
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4, replacement);
        }
        cursor.Index = lastIndex;
    }

    public static void ReplaceInts(ILCursor cursor, Dictionary<int, Func<int>> toReplace) {
        int lastIndex = cursor.Index;
        cursor.Index = 0;
        Func<int> replacement = null!;
        while (cursor.TryGotoNext(MoveType.After, instr => MatchIntInDict(instr, toReplace, out replacement))) {
            cursor.Emit(OpCodes.Pop);
            cursor.EmitDelegate(replacement);
        }
        cursor.Index = lastIndex;
    }

    public static void ReplaceFloats(ILCursor cursor, Dictionary<float, float> toReplace) {
        int lastIndex = cursor.Index;
        cursor.Index = 0;
        while (cursor.TryGotoNext(MoveType.After, instr => MatchFloatInDict(instr, toReplace.Keys.ToList()))) {
            float old = (float) cursor.Prev.Operand;
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_R4, toReplace[old]);
        }
        cursor.Index = lastIndex;
    }

    public static void ReplaceFloats(ILCursor cursor, Dictionary<float, Func<float>> toReplace) {
        int lastIndex = cursor.Index;
        cursor.Index = 0;
        while (cursor.TryGotoNext(MoveType.After, instr => MatchFloatInDict(instr, toReplace.Keys.ToList()))) {
            float old = (float) cursor.Prev.Operand;
            cursor.Emit(OpCodes.Pop);
            cursor.EmitDelegate(toReplace[old]);
        }
        cursor.Index = lastIndex;
    }

    public static void ReplaceStrings(ILCursor cursor, Dictionary<string, Func<string>> toReplace) {
        int lastIndex = cursor.Index;
        cursor.Index = 0;
        while (cursor.TryGotoNext(MoveType.After, instr => MatchStringInDict(instr, toReplace.Keys.ToList()))) {
            string old = (string) cursor.Prev.Operand;
            cursor.Emit(OpCodes.Pop);
            cursor.EmitDelegate(toReplace[old]);
        }
        cursor.Index = lastIndex;
    }

    static bool MatchStringInDict(Instruction instr, List<string> keys) {
        foreach (string val in keys) {
            if (instr.MatchLdstr(val))
                return true;
        }
        return false;
    }

    static bool MatchIntInDict(Instruction instr, Dictionary<int, int> keys, out int replacementInt) {
        foreach (int val in keys.Keys) {
            if (instr.MatchLdcI4(val)) {
                replacementInt = keys[val];
                return true;
            }
        }
        replacementInt = -1;
        return false;
    }

    static bool MatchIntInDict(Instruction instr, Dictionary<int, Func<int>> keys, out Func<int> replacementInt) {
        foreach (int val in keys.Keys) {
            if (instr.MatchLdcI4(val)) {
                replacementInt = keys[val];
                return true;
            }
        }
        replacementInt = () => -1;
        return false;
    }

    static bool MatchFloatInDict(Instruction instr, List<float> keys) {
        foreach (float val in keys) {
            if (instr.MatchLdcR4(val))
                return true;
        }
        return false;
    }

    public static void Ret(this ILCursor cursor) => cursor.Emit(OpCodes.Ret);

    public static void Ret(this ILProcessor p) => p.Emit(OpCodes.Ret);

    public static void Ldarg0(this ILProcessor p) => p.Emit(OpCodes.Ldarg_0);

    public static void LoadStaticField(this ILProcessor p, FieldInfo fieldInfo) => p.Emit(OpCodes.Ldsfld, fieldInfo);

    public static void LoadStaticField(this ILCursor p, FieldInfo fieldInfo) => p.Emit(OpCodes.Ldsfld, fieldInfo);

    public static void LoadField(this ILProcessor p, FieldInfo fieldInfo) => p.Emit(OpCodes.Ldfld, fieldInfo);

    public static void LoadField(this ILCursor p, FieldInfo fieldInfo) => p.Emit(OpCodes.Ldfld, fieldInfo);

    public static void LoadField<T>(this ILCursor p, string fieldName) => p.Emit(OpCodes.Ldfld, typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic));

    public static void EmitCall(this ILProcessor p, MethodInfo method) => p.Emit(OpCodes.Call, method);
    public static void EmitCall(this ILCursor p, MethodInfo method) => p.Emit(OpCodes.Call, method);
    public static void EmitCall<T>(this ILCursor p, string methodName) => p.Emit(OpCodes.Call, typeof(T).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));

    public static void EmitCall<T>(this ILCursor p, T action) where T : Delegate {
        p.Emit(OpCodes.Call, action.Method);
    }

    public static void LoadArg(this ILProcessor p, int argNum) {
        var opc = GetLoadArgOpcode(argNum);
        if (opc.Code == Code.Ldarg) {
            p.Emit(opc, argNum);
        } else {
            p.Emit(opc);
        }
    }

    public static ILCursor LoadInt(this ILCursor cursor, int amt) {
        var opc = GetLoadIntOpcode(amt);
        if (opc.Code == Code.Ldc_I4) {
            cursor.Emit(opc, amt);
        } else {
            cursor.Emit(opc);
        }

        return cursor;
    }

    public static ILProcessor LoadInt(this ILProcessor cursor, int amt) {
        var opc = GetLoadIntOpcode(amt);
        if (opc.Code == Code.Ldc_I4) {
            cursor.Emit(opc, amt);
        } else {
            cursor.Emit(opc);
        }

        return cursor;
    }

    public static OpCode GetLoadIntOpcode(int amt) => amt switch {
        0 => OpCodes.Ldc_I4_0,
        1 => OpCodes.Ldc_I4_1,
        2 => OpCodes.Ldc_I4_2,
        3 => OpCodes.Ldc_I4_3,
        4 => OpCodes.Ldc_I4_4,
        5 => OpCodes.Ldc_I4_5,
        6 => OpCodes.Ldc_I4_6,
        7 => OpCodes.Ldc_I4_7,
        8 => OpCodes.Ldc_I4_8,
        _ => OpCodes.Ldc_I4,
    };

    public static OpCode GetLoadArgOpcode(int amt) => amt switch {
        0 => OpCodes.Ldarg_0,
        1 => OpCodes.Ldarg_1,
        2 => OpCodes.Ldarg_2,
        3 => OpCodes.Ldarg_3,
        _ => OpCodes.Ldarg,
    };

    public static ILHook Hook<T>(string methodName, ILContext.Manipulator manipulator) {
        return new ILHook(typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), manipulator);
    }

    public static T CreateDynamicMethod<T>(string methodName, Action<ILProcessor> generator) where T : Delegate {
        var TType = typeof(T);
        var genTypes = TType.GenericTypeArguments;
        var isFunc = TType.Name.Contains("Func");

        var method = new DynamicMethodDefinition(methodName,
            isFunc ? genTypes.Last() : null,
            isFunc ? genTypes.Take(genTypes.Length - 1).ToArray() : genTypes
            );

        generator(method.GetILProcessor());

        return method.Generate().CreateDelegate<T>();
    }

    /// <summary>
    /// 
    /// The following method is written by max480. Thanks max!
    /// 
    /// Utility method to patch "coroutine" kinds of methods with IL.
    /// Those methods' code reside in a compiler-generated method, and IL.Celeste.* do not allow manipulating them directly.
    /// </summary>
    /// <param name="manipulator">Method taking care of the patching</param>
    /// <returns>The IL hook if the actual code was found, null otherwise</returns>
    public static ILHook HookCoroutine(string typeName, string methodName, ILContext.Manipulator manipulator) {
        // get the Celeste.exe module definition Everest loaded for us
        ModuleDefinition celeste = Everest.Relinker.SharedRelinkModuleMap["Celeste.Mod.mm"];

        // get the type
        TypeDefinition type = celeste.GetType(typeName);
        if (type == null)
            return null!;

        // the "coroutine" method is actually a nested type tracking the coroutine's state
        // (to make it restart from where it stopped when MoveNext() is called).
        // what we see in ILSpy and what we want to hook is actually the MoveNext() method in this nested type.
        foreach (TypeDefinition nest in type.NestedTypes) {
            if (nest.Name.StartsWith("<" + methodName + ">d__")) {
                // check that this nested type contains a MoveNext() method
                MethodDefinition method = nest.FindMethod("System.Boolean MoveNext()");
                if (method == null)
                    return null!;

                // we found it! let's convert it into basic System.Reflection stuff and hook it.
                //Logger.Log("ExtendedVariantMode/ExtendedVariantsModule", $"Building IL hook for method {method.FullName} in order to mod {typeName}.{methodName}()");
                Type reflectionType = typeof(Player).Assembly.GetType(typeName);
                Type reflectionNestedType = reflectionType.GetNestedType(nest.Name, BindingFlags.NonPublic);
                MethodBase moveNextMethod = reflectionNestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
                return new ILHook(moveNextMethod, manipulator);
            }
        }

        return null!;
    }

    /// <summary>
    /// Keeps moving the <paramref name="cursor"/> until a Callvirt opcode is found that calls <paramref name="declaringType"/>.<paramref name="methodName"/> 
    /// </summary>
    /// <returns>True if the specified function call got found</returns>
    public static bool SeekVirtFunctionCall(this ILCursor cursor, Type declaringType, string methodName, MoveType moveType = MoveType.After) {
        while (cursor.TryGotoNext(moveType, instr => instr.MatchCallvirt(declaringType, methodName))) {
            return true;
        }

        return false;
    }

    public static bool SeekVirtFunctionCall<T>(this ILCursor cursor, string methodName, MoveType moveType = MoveType.After)
        => SeekVirtFunctionCall(cursor, typeof(T), methodName, moveType);

    public static bool SeekCall(this ILCursor cursor, Type declaringType, string methodName, MoveType moveType = MoveType.After) {
        while (cursor.TryGotoNext(moveType, instr => instr.MatchCall(declaringType, methodName))) {
            return true;
        }

        return false;
    }

    public static bool SeekCall<T>(this ILCursor cursor, string methodName, MoveType moveType = MoveType.After)
        => SeekCall(cursor, typeof(T), methodName, moveType);

    public static bool SeekLoadFloat(this ILCursor cursor, float value) {
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(value))) {
            return true;
        }

        return false;
    }

    public static bool SeekLoadString(this ILCursor cursor, string value) {
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr(value))) {
            return true;
        }

        return false;
    }
}
