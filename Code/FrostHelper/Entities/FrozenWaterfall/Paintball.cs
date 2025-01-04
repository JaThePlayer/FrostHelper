namespace FrostHelper;

[Tracked]
[CustomEntity("fh/blah")]
internal sealed class Paintball : Actor {
    private readonly bool _tutorial;
    private bool _bubble;
    private readonly SineWave _bubbleWave;
    public readonly Color Color;

    public Paintball(EntityData data, Vector2 offset) : base(data.Position + offset) {
        _tutorial = data.Bool("tutorial", false);
        Color = ColorHelper.GetColor(data.Attr("color", "87CEFA"));
        _bubble = data.Bool("bubble", false);

        if (_bubble) {
            _bubbleWave = new SineWave(0.3f, 0f);
            Add(_bubbleWave);
        }
        hardVerticalHitSoundCooldown = 0f;
        tutorialTimer = 0f;
        Depth = 100;
        Collider = new Hitbox(8f, 10f, -4f, -2f); // -10f
        Add(Sprite = GFX.SpriteBank.Create("snowball"));
        Sprite.Color = Color;
        if (Color == Color.Black)
            Sprite.Color = new Color(0.15f, 0.15f, 0.15f);
        Sprite.Scale.X = -1f;
        Sprite.Play("spin");
        Sprite.Position.Y += 1f;

        Add(Hold = new Holdable(0.1f) {
            PickupCollider = new Hitbox(16f, 22f, -8f, -14f),
            SlowFall = false,
            SlowRun = true,
            OnPickup = OnPickup,
            OnRelease = OnRelease,
            DangerousCheck = Dangerous,
            OnHitSeeker = HitSeeker,
            OnSwat = Swat,
            OnHitSpring = HitSpring,
            OnHitSpinner = HitSpinner,
            SpeedGetter = () => Speed,
        });


        onCollideH = OnCollideH;
        onCollideV = OnCollideV;
        LiftSpeedGraceTime = 0.1f;
        Add(new VertexLight(Collider.Center, Color.White, 1f, 32, 64));
        Tag = Tags.TransitionUpdate;
        Add(new MirrorReflection());
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        Level = SceneAs<Level>();

        if (_tutorial) {
            tutorialGui = new BirdTutorialGui(this, new Vector2(0f, -10f), Dialog.Clean("tutorial_carry", null), 
                Dialog.Clean("tutorial_hold", null), 
                Input.Grab);
            tutorialGui.Open = false;
            Scene.Add(tutorialGui);
        }
    }

    #region bubble
    public override void Render() {
        Sprite.DrawOutline(1);
        base.Render();

        if (_bubble) {
            Sprite.Position.Y = _bubbleWave.Value + 1f;
            for (int i = 0; i < 24; i++) {
                Draw.Point(Position + Vector2.UnitY * (8f + _bubbleWave.Value) + PlatformAdd(i), PlatformColor(i));
            }
        }
    }

    private Color PlatformColor(int num) {
        if (num is <= 1 or >= 22) {
            return Color.White * 0.4f;
        } else {
            return Color.White * 0.8f;
        }
    }

    private Vector2 PlatformAdd(int num) {
        return new Vector2(-12 + num, -5 + (int) Math.Round(Math.Sin(Scene.TimeActive + num * 0.2f) * 1.7999999523162842));
    }
    #endregion
    
    public override void Update() {
        /*
        foreach (ColoredWaterfall water in Scene.Tracker.GetEntities<ColoredWaterfall>()) {
            if (color != water.baseColor) {
                if (water.CollideRect(new Rectangle((int) X, (int) Y, (int) Width, (int) Height))) {
                    water.baseColor = color;
                    water.surfaceColor = water.baseColor * 0.8f;
                    water.fillColor = water.baseColor * 0.3f;
                    water.rayTopColor = water.baseColor * 0.6f;
                }
            }
        }
        if (!shattering) {
            foreach (ColoredWater water in Scene.Tracker.GetEntities<ColoredWater>()) {
                if (color != water.baseColor) {
                    if (water.CollideRect(new Rectangle((int) X, (int) Y, (int) Width, (int) Height))) {
                        water.prevColor = water.baseColor;
                        water.baseColor = color;
                        water.surfaceColor = water.baseColor * 0.8f;
                        water.fillColor = water.baseColor * 0.3f;
                        water.rayTopColor = water.baseColor * 0.6f;
                        water.fixedSurfaces = false;
                        Add(new Coroutine(Shatter()));

                        break;
                    }
                }
            }
            foreach (SeekerBarrier seekerBarrier in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                seekerBarrier.Collidable = true;
                bool collided = CollideCheck(seekerBarrier);
                seekerBarrier.Collidable = false;
                if (collided)
                    Add(new Coroutine(Shatter()));
            }
        }*/


        base.Update();
        if (!(shattering || dead)) {
            if (swatTimer > 0f) {
                swatTimer -= Engine.DeltaTime;
            }
            hardVerticalHitSoundCooldown -= Engine.DeltaTime;

            if (OnPedestal) {
                Depth = 8999;
            } else if (_bubble) {
                Depth = 100;
            } else {
                Depth = 100;

                if (Hold.IsHeld) {
                    prevLiftSpeed = Vector2.Zero;
                } else {
                    if (OnGround(1)) {
                        float target;
                        if (!OnGround(Position + Vector2.UnitX * 3f, 1)) {
                            target = 20f;
                        } else if (!OnGround(Position - Vector2.UnitX * 3f, 1)) {
                            target = -20f;
                        } else {
                            target = 0f;
                        }

                        Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                        Vector2 liftSpeed = LiftSpeed;

                        if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero) {
                            Speed = prevLiftSpeed;
                            prevLiftSpeed = Vector2.Zero;
                            Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                            if (Speed.X != 0f && Speed.Y == 0f) {
                                Speed.Y = -60f;
                            }
                            if (Speed.Y < 0f) {
                                noGravityTimer = 0.15f;
                            }
                        } else {
                            prevLiftSpeed = liftSpeed;
                            if (liftSpeed.Y < 0f && Speed.Y < 0f) {
                                Speed.Y = 0f;
                            }
                        }
                    } else if (Hold.ShouldHaveGravity) {
                        float yMove = 800f;
                        if (Math.Abs(Speed.Y) <= 30f) {
                            yMove *= 0.5f;
                        }
                        float xMove = 350f;
                        if (Speed.Y < 0f) {
                            xMove *= 0.5f;
                        }
                        Speed.X = Calc.Approach(Speed.X, 0f, xMove * Engine.DeltaTime);
                        if (noGravityTimer > 0f) {
                            noGravityTimer -= Engine.DeltaTime;
                        } else {
                            Speed.Y = Calc.Approach(Speed.Y, 200f, yMove * Engine.DeltaTime);
                        }
                    }
                    previousPosition = ExactPosition;
                    MoveH(Speed.X * Engine.DeltaTime, onCollideH, null);
                    MoveV(Speed.Y * Engine.DeltaTime, onCollideV, null);
                    if (Center.X <= Level.Bounds.Right && Left >= Level.Bounds.Left) {
                        if (Top < Level.Bounds.Top - 4) {
                            Top = Level.Bounds.Top + 4;
                            Speed.Y = 0f;
                        } else if (Top > Level.Bounds.Bottom) {
                            return;
                        }
                    }
                    Player player = Scene.Tracker.GetEntity<Player>();
                    TempleGate templeGate = CollideFirst<TempleGate>();
                    if (templeGate is not null && player is not null) {
                        templeGate.Collidable = false;
                        MoveH(Math.Sign(player.X - X) * 32 * Engine.DeltaTime, null, null);
                        templeGate.Collidable = true;
                    }
                }
                if (!dead) {
                    Hold.CheckAgainstColliders();
                }
                if (hitSeeker != null && swatTimer <= 0f && !hitSeeker.Check(Hold)) {
                    hitSeeker = null;
                }
                if (tutorialGui != null) {
                    if (!OnPedestal && !Hold.IsHeld && OnGround(1)) {
                        tutorialTimer += Engine.DeltaTime;
                    } else {
                        tutorialTimer = 0f;
                    }
                    tutorialGui.Open = tutorialTimer > 0.25f;
                }
            }
        }
        if (shattering) {
            Speed = Vector2.Zero;
        }
    }

    public IEnumerator Shatter() {
        shattering = true;
        Collidable = false;
        AllowPushing = false;
        if (Hold.IsHeld) {
            Hold.Holder.Drop();
        }
        Add(new BloomPoint(0f, 32f));
        Add(new VertexLight(Color.AliceBlue, 0f, 64, 200));
        Sprite.Play("break", false, false);
        yield return 0.3f;
        RemoveSelf();
        yield break;
    }

    public void ExplodeLaunch(Vector2 from) {
        if (!Hold.IsHeld) {
            Speed = (Center - from).SafeNormalize(120f);
            SlashFx.Burst(Center, Speed.Angle());
        }
    }

    public void Swat(HoldableCollider hc, int dir) {
        if (Hold.IsHeld && hitSeeker is null) {
            swatTimer = 0.1f;
            hitSeeker = hc;
            Hold.Holder.Swat(dir);
        }
    }

    public bool Dangerous(HoldableCollider holdableCollider) {
        return !Hold.IsHeld && Speed != Vector2.Zero && hitSeeker != holdableCollider;
    }

    public void HitSeeker(Seeker seeker) {
        if (!Hold.IsHeld) {
            Speed = (Center - seeker.Center).SafeNormalize(120f);
        }
        Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
    }

    public void HitSpinner(Entity spinner) {
    }

    public bool HitSpring(Spring spring) {
        if (!Hold.IsHeld) {
            if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f) {
                Speed.X *= 0.5f;
                Speed.Y = -160f;
                noGravityTimer = 0.15f;
                return true;
            }

            if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f) {
                MoveTowardsY(spring.CenterY + 5f, 4f, null);
                Speed.X = 220f;
                Speed.Y = -80f;
                noGravityTimer = 0.1f;
                return true;
            }

            if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f) {
                MoveTowardsY(spring.CenterY + 5f, 4f, null);
                Speed.X = -220f;
                Speed.Y = -80f;
                noGravityTimer = 0.1f;
                return true;
            }
        }
        return false;
    }

    private void OnCollideH(CollisionData data) {
        if (data.Hit is DashSwitch dashSwitch) {
            dashSwitch.OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
        }
        /*
        if (data.Hit is WaterDashSwitch.DashWaterSwitch waterSwitch) {
            waterSwitch.OnDashCollide(null, Vector2.UnitX * (float) Math.Sign(Speed.X));
        }*/
        Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);

        if (Math.Abs(Speed.X) > 100f) {
            ImpactParticles(data.Direction);
        }
        Speed.X *= -0.4f;
    }

    private void OnCollideV(CollisionData data) {
        if (data.Hit is DashSwitch dashSwitch) {
            dashSwitch.OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
        }
        /*
        if (data.Hit is WaterDashSwitch.DashWaterSwitch waterDashSwitch) {
            waterDashSwitch.OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            waterDashSwitch.OnDashed(null, Vector2.UnitY * Math.Sign(Speed.Y));
        }*/

        if (Speed.Y > 0f) {
            if (hardVerticalHitSoundCooldown <= 0f) {
                Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f, 0f, 1f));
                hardVerticalHitSoundCooldown = 0.5f;
            } else {
                Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
            }
        }

        if (Speed.Y > 160f) {
            ImpactParticles(data.Direction);
        }

        if (Speed.Y > 140f && data.Hit is not SwapBlock && data.Hit is not DashSwitch) {
            Speed.Y *= -0.6f;
        } else {
            Speed.Y = 0f;
        }
    }

    private void ImpactParticles(Vector2 dir) {
        float direction;
        Vector2 position;
        Vector2 positionRange;

        switch (dir) {
            case { X: > 0f }:
                direction = MathHelper.Pi;
                position = new Vector2(Right, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
                break;
            case { X: < 0f }:
                direction = 0f;
                position = new Vector2(Left, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
                break;
            case { Y: > 0f }:
                direction = -MathHelper.PiOver2;
                position = new Vector2(X, Bottom);
                positionRange = Vector2.UnitX * 6f;
                break;
            default:
                direction = MathHelper.PiOver2;
                position = new Vector2(X, Top);
                positionRange = Vector2.UnitX * 6f;
                break;
        }

        Level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
    }

    public override bool IsRiding(Solid solid) {
        return Speed.Y == 0f && base.IsRiding(solid);
    }

    public override void OnSquish(CollisionData data) {
        if (!TrySquishWiggle(data) && !SaveData.Instance.Assists.Invincible) {
            Die();
        }
    }

    private void OnPickup() {
        if (_bubble) {
            for (int i = 0; i < 24; i++) {
                SceneAs<Level>().Particles.Emit(Glider.P_Platform, Position + PlatformAdd(i) + Vector2.UnitY * (8f + _bubbleWave.Value), PlatformColor(i));
            }
            _bubble = false;
            Sprite.Position.Y = 1f;
        }
        Speed = Vector2.Zero;
        AddTag(Tags.Persistent);
    }

    private void OnRelease(Vector2 force) {
        RemoveTag(Tags.Persistent);
        if (force.X != 0f && force.Y == 0f) {
            force.Y = -0.4f;
        }
        Speed = force * 200f;
        if (Speed != Vector2.Zero) {
            noGravityTimer = 0.1f;
        }
    }

    public void Die() {
        if (!dead) {
            dead = true;
            Add(new Coroutine(Shatter()));
        }
    }

    public Vector2 Speed;

    public bool OnPedestal;

    public Holdable Hold;

    public Sprite Sprite;

    private bool dead;

    private Level Level;

    private Collision onCollideH;

    private Collision onCollideV;

    private float noGravityTimer;

    private Vector2 prevLiftSpeed;

    private Vector2 previousPosition;

    private HoldableCollider? hitSeeker;

    private float swatTimer;

    private bool shattering;

    private float hardVerticalHitSoundCooldown;

    private BirdTutorialGui tutorialGui;

    private float tutorialTimer;
}
