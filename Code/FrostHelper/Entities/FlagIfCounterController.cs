using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/FlagIfCounterController")]
internal sealed class FlagIfCounterController : Entity {
    private readonly SessionCounterComparer _comparer;
    private readonly string _flag;
    
    public FlagIfCounterController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Active = true;

        _flag = data.Attr("flagToSet");
        
        _comparer = new SessionCounterComparer(
            data.Attr("counter"),
            data.Attr("target"),
            data.Enum("operation", SessionCounterComparer.CounterOperation.Equal));

        Tag |= Tags.PauseUpdate;
    }

    public override void Update() {
        if (Scene is Level l) {
            l.Session.SetFlag(_flag, _comparer.Check(l));
        }
    }
}