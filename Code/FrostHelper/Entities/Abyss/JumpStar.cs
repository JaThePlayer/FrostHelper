using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

/// <summary>
/// Jump Star from Indecx's map The Abyss
/// </summary>
[CustomEntity("FrostHelper/JumpStar")]
public class JumpStar : Entity {
    public int Strength;
    public JumpStarModes Mode;

    public enum JumpStarModes {
        Jump,
        Dash,
    }

    public Sprite Sprite;

    public JumpStar(EntityData data, Vector2 offset) : base(data.Position + offset) {
        ExtVariantsAPI.LoadIfNeeded();
        
        if (!ExtVariantsAPI.Available) {
            throw new Exception("Jump stars require the Extended Variants mod, but it is not installed.");
        }

        Strength = data.Int("strength", 0);
        Mode = data.Enum("mode", JumpStarModes.Jump);
        Sprite = new Sprite(GFX.Game, $"{data.Attr("directory", "theAbyssJumpStar")}/{Mode}/{Strength}star");
        Sprite.AddLoop("idle", "", 0f, 0);
        Sprite.Add("active", "", 0.1f, "idle", 1, 2, 3, 4, 5, 6, 7);
        Sprite.Play("idle");
        Sprite.CenterOrigin();
        Add(Sprite);
        Depth = 100;

        Collider = new Hitbox(16, 16, -8, -8);
        Add(new PlayerCollider(OnPlayer));
        Add(new BloomPoint(0.4f, 16f));
    }

    public void OnPlayer(Player player) {
        int amt;
        switch (Mode) {
            case JumpStarModes.Jump:
                amt = Strength + 1;
                int prevJumpCount = ExtVariantsAPI.GetVariantInt(ExtVariantsAPI.Variant.JumpCount) ?? 0;

                if (prevJumpCount != amt) {
                    Sprite.Play("active");
                    Sprite.OnFinish = (string s) => { Sprite.Play("idle"); };
                    
                    ExtVariantsAPI.SetVariant(ExtVariantsAPI.Variant.JumpCount, amt, false);
                    ExtVariantsAPI.SetJumpCount?.Invoke(amt - 1);
                    ExtVariantsAPI.CapJumpCount?.Invoke(amt);
                }
                break;
            case JumpStarModes.Dash:
                amt = Strength;
                if (player.SceneAs<Level>().Session.Inventory.Dashes != amt) {
                    Sprite.Play("active");
                    Sprite.OnFinish = (string s) => { Sprite.Play("idle"); };
                    player.Dashes = amt;
                    player.SceneAs<Level>().Session.Inventory.Dashes = amt;
                }
                break;
        }
    }
}

