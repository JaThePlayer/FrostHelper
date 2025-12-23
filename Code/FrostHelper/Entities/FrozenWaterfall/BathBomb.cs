using FrostHelper.Components;
using FrostHelper.Entities.FrozenWaterfall;

namespace FrostHelper;

[Tracked]
[CustomEntity("FrostHelper/BathBomb")]
internal sealed class BathBomb : Actor {
    private readonly bool _tutorial;
    private bool _bubble;
    private readonly SineWave _bubbleWave;
    internal Color Color { get; private set; }

    public BathBomb(EntityData data, Vector2 offset) : base(data.Position + offset) {
        _tutorial = data.Bool("tutorial", false);
        Color = ColorHelper.GetColor(data.Attr("color", "87CEFA"));
        _bubble = data.Bool("bubble", false);

        if (_bubble) {
            _bubbleWave = new SineWave(0.3f, 0f);
            Add(_bubbleWave);
        }
        _hardVerticalHitSoundCooldown = 0f;
        _tutorialTimer = 0f;
        Depth = 100;
        Collider = new Hitbox(8f, 10f, -4f, -2f); // -10f
        Add(Sprite = CustomSpriteHelper.CreateCustomSprite("snowball", data.Attr("directory", "danger/")));
        Sprite.Color = Color;
        if (Color == Color.Black)
            Sprite.Color = new Color(0.15f, 0.15f, 0.15f);
        Sprite.Scale.X = -1f;
        Sprite.Play("spin");
        Sprite.Position.Y += 1f;

        Add(Hold = new Holdable(0.1f) {
            PickupCollider = new Hitbox(16f, 16f, -8f, -8f), // 16f, 22f, -8f, -14f
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
            SpeedSetter = v => Speed = v,
        });
        Hold.PickupCollider.Entity = this;
        
        _onCollideH = OnCollideH;
        _onCollideV = OnCollideV;
        LiftSpeedGraceTime = 0.1f;
        Add(new VertexLight(Collider.Center, Color.White, 1f, 32, 64));
        Tag = Tags.TransitionUpdate;
        Add(new MirrorReflection());
        
        Add(new RainCollider(Hold.PickupCollider, false) {
           // OnMakeSplashes = OnMakeSplashes,
            TryCollide = TryCollideWithRain,
        });
    }

    private bool TryCollideWithRain(DynamicRainGenerator.Rain rain) {
        SetColor(rain.Color);

        return false;
    }

    private void SetColor(Color newColor) {
        Color = newColor;
        Sprite.Color = Color;
    }

    private bool OnMakeSplashes(ParticleSystem particleSystem, ref DynamicRainGenerator.Rain rain) {
        SetColor(rain.Color);

        return false;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        _level = SceneAs<Level>();

        if (_tutorial) {
            _tutorialGui = new BirdTutorialGui(this, new Vector2(0f, -10f), Dialog.Clean("tutorial_carry", null), 
                Dialog.Clean("tutorial_hold", null), 
                Input.Grab);
            _tutorialGui.Open = false;
            Scene.Add(_tutorialGui);
        }
    }

    #region bubble
    public override void Render() {
        Sprite.DrawOutline();
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
        }

        return Color.White * 0.8f;
    }

    private Vector2 PlatformAdd(int num) {
        return new Vector2(-12 + num, -5 + (int) Math.Round(Math.Sin(Scene.TimeActive + num * 0.2f) * 1.7999999523162842));
    }
    #endregion
    
    public override void Update() {
        Hold.PickupCollider.Entity = this;

        if (!_shattering) {
            foreach (BathBombCollider collider in Scene.Tracker.SafeGetComponents<BathBombCollider>()) {
                if (collider.CanCollideWith(this) && Hold.PickupCollider.Collide(collider.Collider)) {
                    collider.OnCollide(this);
                }
            }
            
            foreach (SeekerBarrier seekerBarrier in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                seekerBarrier.Collidable = true;
                bool collided = CollideCheck(seekerBarrier);
                seekerBarrier.Collidable = false;
                if (collided)
                    ShatterIfPossible();
            }
        }

        base.Update();
        if (!(_shattering || _dead)) {
            if (_swatTimer > 0f) {
                _swatTimer -= Engine.DeltaTime;
            }
            _hardVerticalHitSoundCooldown -= Engine.DeltaTime;

            if (_bubble) {
                Depth = 100;
            } else {
                Depth = 100;

                if (Hold.IsHeld) {
                    _prevLiftSpeed = Vector2.Zero;
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

                        if (liftSpeed == Vector2.Zero && _prevLiftSpeed != Vector2.Zero) {
                            Speed = _prevLiftSpeed;
                            _prevLiftSpeed = Vector2.Zero;
                            Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                            if (Speed.X != 0f && Speed.Y == 0f) {
                                Speed.Y = -60f;
                            }
                            if (Speed.Y < 0f) {
                                _noGravityTimer = 0.15f;
                            }
                        } else {
                            _prevLiftSpeed = liftSpeed;
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
                        if (_noGravityTimer > 0f) {
                            _noGravityTimer -= Engine.DeltaTime;
                        } else {
                            Speed.Y = Calc.Approach(Speed.Y, 200f, yMove * Engine.DeltaTime);
                        }
                    }

                    MoveH(Speed.X * Engine.DeltaTime, _onCollideH, null);
                    MoveV(Speed.Y * Engine.DeltaTime, _onCollideV, null);
                    if (Center.X <= _level.Bounds.Right && Left >= _level.Bounds.Left) {
                        if (Top < _level.Bounds.Top - 4) {
                            Top = _level.Bounds.Top + 4;
                            Speed.Y = 0f;
                        } else if (Top > _level.Bounds.Bottom) {
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
                if (!_dead) {
                    Hold.CheckAgainstColliders();
                }
                if (_hitSeeker != null && _swatTimer <= 0f && !_hitSeeker.Check(Hold)) {
                    _hitSeeker = null;
                }
                if (_tutorialGui != null) {
                    if (!Hold.IsHeld && OnGround(1)) {
                        _tutorialTimer += Engine.DeltaTime;
                    } else {
                        _tutorialTimer = 0f;
                    }
                    _tutorialGui.Open = _tutorialTimer > 0.25f;
                }
            }
        }
        if (_shattering) {
            Speed = Vector2.Zero;
        }
    }

    public void ShatterIfPossible() {
        if (_shattering) {
            return;
        }

        _shattering = true;
        Add(new Coroutine(Shatter()));
    }

    public IEnumerator Shatter() {
        _shattering = true;
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
    }

    public void ExplodeLaunch(Vector2 from) {
        if (!Hold.IsHeld) {
            Speed = (Center - from).SafeNormalize(120f);
            SlashFx.Burst(Center, Speed.Angle());
        }
    }

    public void Swat(HoldableCollider hc, int dir) {
        if (Hold.IsHeld && _hitSeeker is null) {
            _swatTimer = 0.1f;
            _hitSeeker = hc;
            Hold.Holder.Swat(dir);
        }
    }

    public bool Dangerous(HoldableCollider holdableCollider) {
        return !Hold.IsHeld && Speed != Vector2.Zero && _hitSeeker != holdableCollider;
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
                _noGravityTimer = 0.15f;
                return true;
            }

            if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f) {
                MoveTowardsY(spring.CenterY + 5f, 4f, null);
                Speed.X = 220f;
                Speed.Y = -80f;
                _noGravityTimer = 0.1f;
                return true;
            }

            if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f) {
                MoveTowardsY(spring.CenterY + 5f, 4f, null);
                Speed.X = -220f;
                Speed.Y = -80f;
                _noGravityTimer = 0.1f;
                return true;
            }
        }
        return false;
    }

    private void OnCollideH(CollisionData data) {
        if (data.Hit is DashSwitch dashSwitch) {
            dashSwitch.OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
        }

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

        if (Speed.Y > 0f) {
            if (_hardVerticalHitSoundCooldown <= 0f) {
                Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f, 0f, 1f));
                _hardVerticalHitSoundCooldown = 0.5f;
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

        _level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
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
            _noGravityTimer = 0.1f;
        }
    }

    public void Die() {
        if (!_dead) {
            _dead = true;
            ShatterIfPossible();
        }
    }

    public Vector2 Speed;

    public readonly Holdable Hold;

    public readonly Sprite Sprite;

    private bool _dead;

    private Level _level;

    private readonly Collision _onCollideH;

    private readonly Collision _onCollideV;

    private float _noGravityTimer;

    private Vector2 _prevLiftSpeed;

    private HoldableCollider? _hitSeeker;

    private float _swatTimer;

    private bool _shattering;

    private float _hardVerticalHitSoundCooldown;

    private BirdTutorialGui? _tutorialGui;

    private float _tutorialTimer;
}
