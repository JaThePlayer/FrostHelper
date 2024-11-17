using FrostHelper.Helpers;

namespace FrostHelper;

/// <summary>
/// A door, but it's not an Actor and doesn't check for collisions with solids
/// </summary>
[Tracked]
[CustomEntity("FrostHelper/StaticDoor")]
public class StaticDoor : Entity {
    public Sprite Sprite;
    public string OpenSfx;
    public string CloseSfx;
    public LightOcclude Occlude;
    public bool Disabled;
    public bool SolidIfDisabled;

    public StaticDoor(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Depth = 8998;
        string type = data.Attr("type", "wood");
        if (type == "wood") {
            Add(Sprite = GFX.SpriteBank.Create("door"));
            OpenSfx = "event:/game/03_resort/door_wood_open";
            CloseSfx = "event:/game/03_resort/door_wood_close";
        } else {
            Add(Sprite = GFX.SpriteBank.Create(type + "door"));
            OpenSfx = "event:/game/03_resort/door_metal_open";
            CloseSfx = "event:/game/03_resort/door_metal_close";
        }

        OpenSfx = data.AttrNullable("openSfx", OpenSfx);
        CloseSfx = data.AttrNullable("closeSfx", CloseSfx);
        SolidIfDisabled = data.Bool("solidIfDisabled", false);

        Sprite.Play("idle", false, false);
        Collider = data.Collider("hitbox") ?? new Hitbox(12f, 22f, -6f, -23f);
        Add(Occlude = new LightOcclude(new(-1, -24, 2, 24), data.Float("lightOccludeAlpha", 1f)));
        Add(new PlayerCollider(HitPlayer, null, null));
    }

    private void HitPlayer(Player player) {
        if (!Disabled) {
            Open(player.X);
        }
    }

    public void Disable() {
        if (!Disabled) {
            Disabled = true;

            if (SolidIfDisabled) {
                var solid = new Solid(Position - new Vector2(1f, 24f), 3, 24, false) { 
                    new ClimbBlocker(true) 
                };
                Scene.Add(solid);
            }

        }
    }

    public void Open(float x) {
        if (Sprite.CurrentAnimationID == "idle") {
            Audio.Play(OpenSfx, Position);
            Sprite.Play("open", false, false);
            if (X != x) {
                Sprite.Scale.X = Math.Sign(x - X);
                return;
            }
        } else if (Sprite.CurrentAnimationID == "close") {
            Sprite.Play("close", true, false);
        }
    }

    public override void Update() {
        string prevAnimId = Sprite.CurrentAnimationID;
        base.Update();

        bool idle = Occlude.Visible = Sprite.CurrentAnimationID == "idle";

        if (idle && prevAnimId == "close") {
            Audio.Play(CloseSfx, Position);
        }
    }
}
