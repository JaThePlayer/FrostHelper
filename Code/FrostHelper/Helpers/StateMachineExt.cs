namespace FrostHelper;

internal static class StateMachineExt {
    /// <summary>
    /// Adds a state to a StateMachine
    /// </summary>
    /// <returns>The index of the new state</returns>
    public static int AddState(this StateMachine machine, Func<Entity, int> onUpdate, Func<Entity, IEnumerator> coroutine = null!, Action<Entity> begin = null!, Action<Entity> end = null!) {
        int nextIndex = Expand(machine);
        // And now we add the new functions
        machine.SetCallbacks(nextIndex, () => onUpdate(machine.Entity), () => coroutine(machine.Entity), () => begin(machine.Entity), () => end(machine.Entity));
        return nextIndex;
    }

    public static int AddState(this StateMachine machine, Func<Player, int> onUpdate, Func<Player, IEnumerator> coroutine = null!, Action<Player> begin = null!, Action<Player> end = null!) {
        int nextIndex = Expand(machine);
        // And now we add the new functions
        machine.SetCallbacks(nextIndex, () => onUpdate((machine.Entity as Player)!), 
            coroutine is null ? null : () => coroutine((machine.Entity as Player)!), 
            () => begin((machine.Entity as Player)!), 
            () => end((machine.Entity as Player)!));
        return nextIndex;
    }

    public static int Expand(this StateMachine machine) {
        Action[] begins = (Action[]) StateMachine_begins.GetValue(machine);
        Func<int>[] updates = (Func<int>[]) StateMachine_updates.GetValue(machine);
        Action[] ends = (Action[]) StateMachine_ends.GetValue(machine);
        Func<IEnumerator>[] coroutines = (Func<IEnumerator>[]) StateMachine_coroutines.GetValue(machine);
        int nextIndex = begins.Length;
        // Now let's expand the arrays
        Array.Resize(ref begins, begins.Length + 1);
        Array.Resize(ref updates, begins.Length + 1);
        Array.Resize(ref ends, begins.Length + 1);
        Array.Resize(ref coroutines, coroutines.Length + 1);
        // Store the resized arrays back into the machine
        StateMachine_begins.SetValue(machine, begins);
        StateMachine_updates.SetValue(machine, updates);
        StateMachine_ends.SetValue(machine, ends);
        StateMachine_coroutines.SetValue(machine, coroutines);

        return nextIndex;
    }

    private static FieldInfo StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
    private static FieldInfo StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
    private static FieldInfo StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
    private static FieldInfo StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);
}
