using Celeste.Mod.Entities;

namespace FrostHelper;

[CustomEntity("FrostHelper/SwingRope")]
public class SwingRope : Entity {
    public float PlayerSpeedCarryMult;

    public Player CarriedPlayer;

    float preservedSpeed;
    float playerGrabCooldown = 0f;

    Image BottomImage;

    public SwingRope(EntityData data, Vector2 offset) : base(data.Position + offset + new Vector2(4f, 0f)) {
        PlayerSpeedCarryMult = data.Float("playerSpeedCarryMult", 0.005f);

        images = new();
        Length = Math.Max(16, data.Height);
        Depth = 2000;
        MTexture mtexture = GFX.Game["objects/hanginglamp"];
        Image image;
        var middleSubtexture = mtexture.GetSubtexture(0, 8, 8, 8);

        for (int i = 0; i < Length - 8; i += 8) {
            Add(image = new Image(middleSubtexture) {
                Origin = new Vector2(4f, -i),
            });
            images.Add(image);
        }
        Add(new Image(mtexture.GetSubtexture(0, 0, 8, 8)) {
            Origin = new(4f, 0f),
        });
        Add(image = new Image(mtexture.GetSubtexture(0, 16, 8, 8)) {
            Origin = new(4f, -(Length - 8))
        });
        BottomImage = image;
        images.Add(image);

        Add(bloom = new BloomPoint(Vector2.UnitY * (Length - 4), 1f, 48f));
        Add(light = new VertexLight(Vector2.UnitY * (Length - 4), Color.White, 1f, 24, 48));
        Add(sfx = new SoundSource());
        Collider = new Hitbox(8f, Length, -4f, 0f);
    }

    public override void Update() {
        base.Update();
        soundDelay -= Engine.DeltaTime;
        playerGrabCooldown -= Engine.DeltaTime;

        Player player = Scene.Tracker.GetEntity<Player>();
        if (CarriedPlayer is null && player is not null && /*Collider.Collide(player) &&*/ Vector2.DistanceSquared(player.Position, Position + bloom.Position) < 15f * 15f) {
            if (playerGrabCooldown <= 0f && Input.GrabCheck) {
                speed = -player.Speed.X * PlayerSpeedCarryMult * ((player.Y - Y) / Length);
                if (Math.Abs(speed) < 0.1f) {
                    speed = 0f;
                } else if (soundDelay <= 0f) {
                    sfx.Play("event:/game/02_old_site/lantern_hit", null, 0f);
                    soundDelay = 0.25f;
                }
                preservedSpeed = speed;

                CarriedPlayer = player;
                player.ForceCameraUpdate = true;
                player.StateMachine.State = Player.StDummy;
                player.StateMachine.Locked = true;
            }
        }

        float speedDelta = (Math.Sign(rotation) == Math.Sign(speed)) ? 8f : /*6f*/4f;
        if (Math.Abs(rotation) < 0.5f) {
            speedDelta *= 0.5f;
        }
        if (Math.Abs(rotation) < 0.25f) {
            speedDelta *= 0.5f;
        }

        float previousRotation = rotation;
        speed += -Math.Sign(rotation) * speedDelta * Engine.DeltaTime;
        if (speed != 0f)
            preservedSpeed = speed;

        rotation += speed * Engine.DeltaTime;
        rotation = Calc.Clamp(rotation, -1.2f, 1.2f);
        if (Math.Abs(rotation) < 0.02f && Math.Abs(speed) < 0.2f) {
            rotation = speed = 0f;
        } else if (Math.Sign(rotation) != Math.Sign(previousRotation) && soundDelay <= 0f && Math.Abs(speed) > 0.5f) {
            sfx.Play("event:/game/02_old_site/lantern_hit", null, 0f);
            soundDelay = 0.25f;
        }
        foreach (Image image in images) {
            image.Rotation = rotation;
        }

        sfx.Position = bloom.Position = light.Position = Calc.AngleToVector(rotation + 1.57079637f, Length - 4f);


        if (CarriedPlayer is not null) {
            var pos = bloom.Position + Position;
            CarriedPlayer.MoveToX(pos.X, OnPlayerCollide);
            CarriedPlayer.MoveToY(pos.Y, OnPlayerCollide);

            if (CarriedPlayer.CanDash) {
                CarriedPlayer.StateMachine.Locked = false;
                CarriedPlayer.StateMachine.State = CarriedPlayer.StartDash();
                CarriedPlayer = null;
                playerGrabCooldown = 0.2f;
                return;
            }

            if (Input.Jump.Pressed) {
                Input.Jump.ConsumePress();
                ReleasePlayer(true);
                return;
            }

            if (!Input.GrabCheck) {
                ReleasePlayer(false);
                return;
            }
        }
    }

    public void ReleasePlayer(bool jump) {
        if (CarriedPlayer != null) {
            playerGrabCooldown = 0.2f;
            CarriedPlayer.StateMachine.Locked = false;
            CarriedPlayer.StateMachine.State = Player.StNormal;

            var playerSpeed = /*Math.Abs*/(preservedSpeed / PlayerSpeedCarryMult / 1.35f);
            CarriedPlayer.Speed = new Vector2(-playerSpeed * 1.35f, -Math.Abs(playerSpeed) / 1.35f);//Calc.AngleToVector(rotation + 1.57079637f, speed);//new Vector2(speed / PlayerSpeedCarryMult);

            CarriedPlayer.ForceCameraUpdate = false;
            CarriedPlayer.LiftSpeed = new(CarriedPlayer.LiftSpeed.X, CarriedPlayer.Speed.Y);
            if (jump) {
                CarriedPlayer.Jump();
            }

            CarriedPlayer = null;
        }
    }

    private void OnPlayerCollide(CollisionData data) {
        ReleasePlayer(false);
    }

    public override void Render() {
        foreach (Component component in Components) {
            if (component is Image image) {
                image.DrawOutline(1);
            }
        }
        base.Render();

        // DEBUG
        Draw.Circle(bloom.Position + Position, 4f, Color.Red, 8);
    }

    public readonly int Length;

    private List<Image> images;

    private BloomPoint bloom;

    private VertexLight light;

    private float speed;

    private float rotation;

    private float soundDelay;

    private SoundSource sfx;
}
