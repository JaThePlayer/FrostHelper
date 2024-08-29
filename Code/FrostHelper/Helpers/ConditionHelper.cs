using FrostHelper.ModIntegration;
using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

public static class ConditionHelper {
    private static readonly Condition _EmptyCondition = new Condition("");
    public class Condition : ISavestatePersisted {
        public bool Inverted;
        public string Flag;
        public bool Empty;

        public Condition(string str) {
            if (string.IsNullOrWhiteSpace(str)) {
                Empty = true;
                return;
            }

            int startIndex = 0;
            if (str[startIndex] == '!') {
                Inverted = true;
                startIndex++;
            }

            Flag = str[startIndex..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check() => Empty || Check(FrostModule.GetCurrentLevel().Session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(Session session) => Empty || (session.GetFlag(Flag) != Inverted);
    }

    public static Condition GetCondition(this EntityData data, string name, string def = "") {
        Condition condition = null!;
        if (data.Values.TryGetValue(name, out var cond)) {
            switch (cond) {
                case Condition fullCondition:
                    condition = fullCondition;
                    break;
                case string str:
                    condition = new(str);
                    data.Values[name] = condition; // cache the parsed condition
                    break;
            }
        } else {
            condition = _EmptyCondition;
        }

        return condition;
    }
}
