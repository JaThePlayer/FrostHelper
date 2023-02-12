namespace FrostHelper.EXPERIMENTAL;

internal class RandomStuff {
    #warning DONT SHIP THIS AAAAA
    [OnLoad]
    public static void Load() {
        //IL.Celeste.Solid.Awake += Solid_Awake;
    }

    private static void Solid_Awake(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After,
            instr => instr.MatchLdloc(2),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchCallvirt<StaticMover>("IsRiding")
        );

        var label = cursor.Next.Operand;
        cursor.Index -= 3;

        cursor.Emit(OpCodes.Ldloc, 2);
        cursor.Emit(OpCodes.Ldfld, typeof(StaticMover).GetField(nameof(StaticMover.Platform)));
        cursor.Emit(OpCodes.Brtrue_S, label);
    }
}
