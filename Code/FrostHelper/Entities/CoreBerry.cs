namespace FrostTempleHelper;

[CustomEntity("FrostHelper/CoreBerry")]
[RegisterStrawberry(true, false)]
public class CoreBerry : Strawberry, IStrawberry {
    private readonly bool _isIce;
    private Sprite _indicator;

    public CoreBerry(EntityData data, Vector2 position, EntityID gid) : base(data, position, gid) {
        ID = gid;
        _isIce = data.Bool("isIce", false);
        Add(new CoreModeListener(OnChangeMode));
    }

    private void OnChangeMode(Session.CoreModes mode) {
        if (!_isIce) {
            // berry hot
            if (mode == Session.CoreModes.Cold) {
                Dissolve();
                sprite.Visible = false;
                _indicator.Visible = true;
            }
        } else {
            // berry cold
            if (mode is Session.CoreModes.Hot or Session.CoreModes.None) {
                Dissolve();
                sprite.Visible = false;
                _indicator.Visible = true;
            }
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        Remove(Get<BloomPoint>());
        var isGhost = SaveData.Instance.CheckStrawberry(ID);
        string spr;
        string ind;
        if (_isIce) {
            ind = "collectables/FrostHelper/CoreBerry/Cold/CoreBerry_Cold_Indicator";
            spr = isGhost ? "coldberryghost" : "coldberry";
        } else {
            ind = "collectables/FrostHelper/CoreBerry/Hot/CoreBerry_Hot_Indicator";
            spr = isGhost ? "hotberryghost" : "hotberry";
        }

        Remove(sprite);
        sprite = FrostHelper.FrostModule.SpriteBank.Create(spr);
        Add(sprite);

        _indicator = new Sprite(GFX.Game, ind);
        _indicator.AddLoop("idle", "", 0.1f);
        _indicator.Play("idle", false, false);
        _indicator.CenterOrigin();
        _indicator.Visible = false;
        Add(_indicator);
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        
        Level level = (scene as Level)!;
        Session.CoreModes mode = level.Session.CoreMode;
        OnChangeMode(mode);
    }

    public void Dissolve(bool visible = true) {
        if (Follower.Leader != null) {
            (Follower.Leader.Entity as Player)!.StrawberryCollectResetTimer = 2.5f;
            Follower.Leader.LoseFollower(Follower);
        }
        Add(new Coroutine(DissolveRoutine(visible), true));
    }

    private IEnumerator DissolveRoutine(bool visible = true) {
        Level level = (Scene as Level)!;
        Session session = level.Session;
        session.DoNotLoad.Remove(ID);
        Audio.Play("event:/game/general/seed_poof", Position);
        Collidable = false;
        sprite.Scale = Vector2.One * 0.5f;
        yield return 0.05f;
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        if (visible) {
            for (int i = 0; i < 6; i++) {
                float dir = Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
            }
        }

        sprite.Scale = Vector2.Zero;
        sprite.Visible = false;
        _indicator.Visible = true;
        yield break;
    }
}