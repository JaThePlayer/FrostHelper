using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/Bubbler")]
public class Bubbler : Entity {
    private Vector2[] nodes;
    private Sprite? sprite;
    private Sprite? previewSprite;
    private SineWave? sine;

    public Bubbler(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(14f, 14f, 0f, 0f);
        Collider.CenterOrigin();
        Add(new PlayerCollider(OnPlayer));

        nodes = data.NodesOffset(offset);

        if (data.Bool("visible", false)) {
            Calc.PushRandom(VisualRandom.Instance);
            sine = new SineWave(0.6f);
            sine.Randomize();
            Calc.PopRandom();

            var color = ColorHelper.GetColor(data.Attr("color", "White"));
            Add(sprite = new Sprite(GFX.Game, "objects/FrostHelper/bubble"));
            sprite.AddLoop("idle", "", 0.1f);
            sprite.CenterOrigin();
            sprite.Play("idle", false, false);
            sprite.SetColor(color);

            if (data.Bool("showReturnIndicator", true)) {
                previewSprite = new Sprite(GFX.Game, "objects/FrostHelper/bubble");
                previewSprite.AddLoop("idle", "", 0.1f);
                previewSprite.CenterOrigin();
                previewSprite.Play("idle", false, false);
                previewSprite.Position = nodes.Last() - new Vector2(0f, 10f);
                previewSprite.SetColor(new Color(color.R, color.G, color.B, 128f) * 0.3f);
            }
        }
    }

    public override void Update() {
        if (sine is { }) {
            sine.Update();
            var wobble = sine.Value;
            if (sprite is { })
                sprite.Y = wobble;
        }

        base.Update();
    }

    public override void Render() {
        base.Render();

        if (previewSprite is { } && CameraCullHelper.IsVisible(FrostModule.GetCurrentLevel().Camera.Position, previewSprite)) {
            DreamySpriteHelper.DrawDreamySprite(previewSprite, speed: 2f, maxOffset: 1.3f);
        }
    }

    private void OnPlayer(Player player) {
        Collidable = false;
        if (nodes != null && nodes.Length >= 2) {
            sprite?.RemoveSelf();
            Add(new Coroutine(NodeRoutine(player), true));
        }
    }

    private IEnumerator NodeRoutine(Player player) {
        if (!player.Dead) {
            Audio.Play("event:/game/general/cassette_bubblereturn", SceneAs<Level>().Camera.Position + new Vector2(160f, 90f));
            player.Dashes = Math.Max(player.Dashes, player.MaxDashes);
            player.StartCassetteFly(nodes[1], nodes[0]);
            // Cursed on linux? D:
            //On.Celeste.Player.CassetteFlyEnd += Player_CassetteFlyEnd;

            On.Celeste.Player.NormalBegin += Player_NormalBegin;
        }
        yield break;
    }

    private void Player_NormalBegin(On.Celeste.Player.orig_NormalBegin orig, Player self) {
        orig(self);

        previewSprite?.RemoveSelf();
        previewSprite = null;

        On.Celeste.Player.NormalBegin -= Player_NormalBegin;
    }
}