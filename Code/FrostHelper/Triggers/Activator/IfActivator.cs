namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Only activates others if a flag is enabled
/// </summary>
[CustomEntity("FrostHelper/IfActivator")]
internal class IfActivator : BaseActivator {
    //public string Flag;
    FullCondition condition;

    public IfActivator(EntityData data, Vector2 offset) : base(data, offset) {
        if (data.Values.TryGetValue("condition", out var cond)) {
            switch (cond) {
                case FullCondition fullCondition:
                    condition = fullCondition;
                    break;
                case string str:
                    condition = new(str);
                    data.Values["condition"] = condition; // cache the parsed condition
                    break;
            }
        }

        Collidable = false;
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (condition.Met())
            ActivateAll(player);
    }

    private class FullCondition {
        private Condition FirstCondition;

        public FullCondition(string str) {
            var i = 0;
            var invert = false;

            while (i < str.Length) {
                if (str[i] == '!') {
                    invert = !invert;
                    i++;
                } else {
                    var spaceIdx = str.IndexOf(' ', i);
                    if (spaceIdx == -1)
                        spaceIdx = str.Length - 1;

                    FirstCondition = new Flag(str.Substring(i, spaceIdx - i)) {
                        Reverted = invert,
                    };

                    i = spaceIdx + 1;
                }
            }
        }

        public bool Met() => FirstCondition.IsMet();

        private abstract class Condition {
            public bool Reverted;

            public bool IsMet() => Check() != Reverted;

            internal abstract bool Check();
        }

        private class Flag : Condition {
            string flag;

            public Flag(string flag) {
                this.flag = flag;
            }

            internal override bool Check() => FrostModule.GetCurrentLevel().Session.GetFlag(flag);
        }
    }
}