namespace FrostHelper;

[CustomEntity("CCH/WASDMovementTrigger",
              "FrostHelper/WASDMovementTrigger")]
[Tracked]
public class WASDMovementTrigger : Trigger {
    public int HitboxWidth;
    public float Speed;
    public string Texture;

    public WASDMovementTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        HitboxWidth = data.Int("hitboxWidth", 2);
        Speed = data.Float("speed", 80f);
        Texture = data.Attr("texture", "util/pixel");
    }

    public override void OnEnter(Player player) {
        player.StateMachine.State = WASDMovementState.ID;
    }
}

public static class WASDMovementState {
    public static int ID = int.MaxValue;

    public static int HitboxWidth;
    public static float Speed;
    public static Image Image;
    public static Hitbox Hitbox;

    // used when the state ends
    private static Collider previousCollider;
    private static Hitbox previousNormalHitbox;
    private static Hitbox previousNormalHurtbox;
    private static Hitbox previousDuckHitbox;
    private static Hitbox previousHurbox;

    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded) 
            return;
        _hooksLoaded = true;
        On.Celeste.Player.OnCollideV += Player_OnCollideV;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Player.OnCollideV -= Player_OnCollideV;
    }

    /// <summary>
    /// Get rid of particles when colliding with floors while moving downwards
    /// </summary>
    private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data) {
        if (self.StateMachine.State != ID) {
            orig(self, data);
        }
    }

    public static string GetTasToolsDisplayName() => "WASD Movement";

    public static void Begin(Player player) {
        LoadIfNeeded();

        WASDMovementTrigger wasdTrigger = player.CollideFirst<WASDMovementTrigger>();
        HitboxWidth = wasdTrigger.HitboxWidth;
        Speed = wasdTrigger.Speed;

        Image = new Image(GFX.Game[wasdTrigger.Texture]);
        player.Add(Image);

        player.Sprite.Visible = false;
        player.Hair.Visible = false;

        Hitbox = new Hitbox(HitboxWidth, HitboxWidth);

        previousCollider = player.Collider;
        previousNormalHitbox = (Hitbox) Player_normalHitbox.GetValue(player);
        previousDuckHitbox = (Hitbox) Player_duckHitbox.GetValue(player);
        previousNormalHurtbox = (Hitbox) Player_normalHurtbox.GetValue(player);
        previousHurbox = player.hurtbox;

        player.Collider = Hitbox;
        Player_normalHitbox.SetValue(player, Hitbox);
        Player_duckHitbox.SetValue(player, Hitbox);
        Player_normalHurtbox.SetValue(player, Hitbox);
        player.hurtbox = Hitbox;
    }

    public static void End(Player player) {
        Image.RemoveSelf();
        player.Sprite.Visible = true;
        player.Hair.Visible = true;

        // revert colliders to what they used to be
        player.Collider = previousCollider;
        player.Collider = Hitbox;

        Player_normalHitbox.SetValue(player, previousNormalHitbox);
        Player_duckHitbox.SetValue(player, previousDuckHitbox);
        Player_normalHurtbox.SetValue(player, previousNormalHurtbox);
        player.hurtbox = previousHurbox;
    }

    public static int Update(Player player) {
        player.MuffleLanding = true;
        player.Speed = player.CorrectDashPrecision(Input.GetAimVector(0)).SafeNormalize() * Speed;

        return ID;
    }

    // these are readonly, and so we can't bypass access checks that easily.
    private static FieldInfo Player_normalHitbox = typeof(Player).GetField("normalHitbox", BindingFlags.NonPublic | BindingFlags.Instance);
    private static FieldInfo Player_duckHitbox = typeof(Player).GetField("duckHitbox", BindingFlags.NonPublic | BindingFlags.Instance);
    private static FieldInfo Player_normalHurtbox = typeof(Player).GetField("normalHurtbox", BindingFlags.NonPublic | BindingFlags.Instance);
}
