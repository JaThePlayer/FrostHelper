namespace FrostHelper;

[CustomEntity("FrostHelper/AnxietyTrigger")]
public class AnxietyTrigger : Trigger {
    private SineWave anxietySine;
    float mult;
    float anxietyJitter;
    public AnxietyTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        mult = data.Float("multiplyer", 1f);
        Add(anxietySine = new SineWave(0.3f, 0f));
    }

    public override void OnStay(Player player) {
        base.OnStay(player);
        if (SceneAs<Level>().OnInterval(0.1f)) {
            anxietyJitter = Calc.Random.Range(-0.1f, 0.1f);
        }
        Distort.Anxiety = Math.Max(0.2f, anxietyJitter + anxietySine.Value * 0.6f) * mult;
    }
}
