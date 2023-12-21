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
        Action[] begins = machine.begins;
        Func<int>[] updates = machine.updates;
        Action[] ends = machine.ends;
        Func<IEnumerator>[] coroutines = machine.coroutines;
        
        int nextIndex = begins.Length;
        // Now let's expand the arrays
        Array.Resize(ref begins, begins.Length + 1);
        Array.Resize(ref updates, begins.Length + 1);
        Array.Resize(ref ends, begins.Length + 1);
        Array.Resize(ref coroutines, coroutines.Length + 1);
        
        // Store the resized arrays back into the machine
        machine.begins = begins;
        machine.updates = updates;
        machine.ends = ends;
        machine.coroutines = coroutines;

        return nextIndex;
    }
}
