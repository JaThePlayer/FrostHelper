namespace FrostHelper;

[CustomEntity("FrostHelper/SlowCrushBlock")]
public class CustomCrushBlock : Solid {
    public float CrushSpeed;
    public float CrushAccel;
    public float ReturnSpeed;
    public float ReturnAccel;
    public readonly string Directory;

    private readonly SfxData _sfx;
    
    private class SfxData(string prefix) {
        private static string CreatePrefix(string prefix, string affix) =>
            string.IsNullOrWhiteSpace(prefix) ? "event:/none" : $"{prefix}_{affix}";

        public string Activate { get; } = CreatePrefix(prefix, "activate");
        public string MoveLoop { get; } = CreatePrefix(prefix, "move_loop");
        public string Impact { get; } = CreatePrefix(prefix, "impact");
        public string ReturnLoop { get; } = CreatePrefix(prefix, "return_loop");
        public string Rest { get; } = CreatePrefix(prefix, "rest");//
        public string RestWaypoint { get; } = CreatePrefix(prefix, "rest_waypoint");
    }

    private bool IsNoReturn => ReturnSpeed <= 0f || ReturnAccel <= 0f;

    public CustomCrushBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
        CrushAccel = data.Float("crushAcceleration", 250f);
        ReturnAccel = data.Float("returnAcceleration", 160f);
        ReturnSpeed = data.Float("returnSpeed", 60f);
        CrushSpeed = data.Float("crushSpeed", 120f);
        _chillOut = data.Bool("chillout", false);
        Directory = data.Attr("directory", "objects/FrostHelper/slowcrushblock/");
        if (!Directory.EndsWith('/'))
            Directory += '/';
        _sfx = new SfxData(data.Attr("sfxPrefix", "event:/game/06_reflection/crushblock"));

        _fillColor = data.GetColor("fillColor", "62222b");
        _activeTopImages = [];
        _activeRightImages = [];
        _activeLeftImages = [];
        _activeBottomImages = [];
        OnDashCollide = OnDashed;
        _returnStack = [];

        _giant = Width >= 48f && Height >= 48f && _chillOut;
        _canActivate = true;
        _attackCoroutine = new Coroutine(true);
        _attackCoroutine.RemoveOnComplete = false;
        Add(_attackCoroutine);
        List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(Directory + "block");
        MTexture idle;
        switch (data.Enum("axes", Axes.Both)) {
            default:
                idle = atlasSubtextures[3];
                _canMoveHorizontally = _canMoveVertically = true;
                break;
            case Axes.Horizontal:
                idle = atlasSubtextures[1];
                _canMoveHorizontally = true;
                _canMoveVertically = false;
                break;
            case Axes.Vertical:
                idle = atlasSubtextures[2];
                _canMoveHorizontally = false;
                _canMoveVertically = true;
                break;
        }

        var faceXmlEntry = _giant ? "giant_crushblock_face" : "crushblock_face";
        _face = data.Bool("reskinFace", false)
            ? CustomSpriteHelper.CreateCustomSprite(faceXmlEntry, Directory)
            : GFX.SpriteBank.Create(faceXmlEntry);
        Add(_face);
        _face.Position = new Vector2(Width, Height) / 2f;
        _face.Play("idle", false, false);
        _face.OnLastFrame = f => {
            if (f == "hit") {
                _face.Play(_nextFaceDirection, false, false);
            }
        };
        
        int widthTiles = (int) (Width / 8f) - 1;
        int heightTiles = (int) (Height / 8f) - 1;
        AddImage(idle, 0, 0, 0, 0, -1, -1);
        AddImage(idle, widthTiles, 0, 3, 0, 1, -1);
        AddImage(idle, 0, heightTiles, 0, 3, -1, 1);
        AddImage(idle, widthTiles, heightTiles, 3, 3, 1, 1);
        for (int i = 1; i < widthTiles; i++) {
            AddImage(idle, i, 0, Calc.Random.Choose(1, 2), 0, 0, -1);
            AddImage(idle, i, heightTiles, Calc.Random.Choose(1, 2), 3, 0, 1);
        }
        for (int j = 1; j < heightTiles; j++) {
            AddImage(idle, 0, j, 0, Calc.Random.Choose(1, 2), -1, 0);
            AddImage(idle, widthTiles, j, 3, Calc.Random.Choose(1, 2), 1, 0);
        }
        Add(new LightOcclude(0.2f));
        Add(_returnLoopSfx = new SoundSource());
        Add(new WaterInteraction(() => _crushDir != Vector2.Zero));
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        _level = SceneAs<Level>();
    }

    public override void Update() {
        base.Update();

        if (_crushDir == Vector2.Zero) {
            _face.Position = new Vector2(Width, Height) / 2f;

            if (CollideCheck<Player>(Position + new Vector2(-1f, 0f))) {
                _face.X -= 1f;
            } else if (CollideCheck<Player>(Position + new Vector2(1f, 0f))) {
                _face.X += 1f;
            } else if (CollideCheck<Player>(Position + new Vector2(0f, -1f))) {
                _face.Y -= 1f;
            }
        }

        _currentMoveLoopSfx?.Param("submerged", Submerged ? 1 : 0);
        _returnLoopSfx?.Param("submerged", Submerged ? 1 : 0);
    }

    public override void Render() {
        Vector2 position = Position;
        Position += Shake;
        Draw.Rect(X + 2f, Y + 2f, Width - 4f, Height - 4f, _fillColor);
        base.Render();
        Position = position;
    }

    private bool Submerged => Scene.CollideCheck<Water>(new Rectangle((int) (Center.X - 4f), (int) Center.Y, 8, 4));

    private void AddImage(MTexture idle, int x, int y, int tx, int ty, int borderX = 0, int borderY = 0) {
        MTexture subtexture = idle.GetSubtexture(tx * 8, ty * 8, 8, 8, null);
        Vector2 imagePosition = new Vector2(x * 8, y * 8);

        if (borderX != 0) {
            Add(new Image(subtexture) {
                Color = Color.Black,
                Position = imagePosition + new Vector2(borderX, 0f)
            });
        }

        if (borderY != 0) {
            Add(new Image(subtexture) {
                Color = Color.Black,
                Position = imagePosition + new Vector2(0f, borderY)
            });
        }

        Add(new Image(subtexture) { Position = imagePosition });

        if (borderX != 0 || borderY != 0) {
            if (borderX < 0) {
                Image leftImg = new Image(GFX.Game[Directory + "lit_left"].GetSubtexture(0, ty * 8, 8, 8, null));
                _activeLeftImages.Add(leftImg);
                leftImg.Position = imagePosition;
                leftImg.Visible = false;
                Add(leftImg);
            } else if (borderX > 0) {
                Image rightImg = new Image(GFX.Game[Directory + "lit_right"].GetSubtexture(0, ty * 8, 8, 8, null));
                _activeRightImages.Add(rightImg);
                rightImg.Position = imagePosition;
                rightImg.Visible = false;
                Add(rightImg);
            }

            if (borderY < 0) {
                Image topImage = new Image(GFX.Game[Directory + "lit_top"].GetSubtexture(tx * 8, 0, 8, 8, null));
                _activeTopImages.Add(topImage);
                topImage.Position = imagePosition;
                topImage.Visible = false;
                Add(topImage);
            } else if (borderY > 0) {
                Image bottomImage = new Image(GFX.Game[Directory + "lit_bottom"].GetSubtexture(tx * 8, 0, 8, 8, null));
                _activeBottomImages.Add(bottomImage);
                bottomImage.Position = imagePosition;
                bottomImage.Visible = false;
                Add(bottomImage);
            }
        }
    }

    private void TurnOffImages() {
        foreach (Image image in _activeLeftImages) {
            image.Visible = false;
        }
        foreach (Image image in _activeRightImages) {
            image.Visible = false;
        }
        foreach (Image image in _activeTopImages) {
            image.Visible = false;
        }
        foreach (Image image in _activeBottomImages) {
            image.Visible = false;
        }
    }

    private DashCollisionResults OnDashed(Player player, Vector2 direction) {
        if (CanActivate(-direction)) {
            Attack(-direction);
            return DashCollisionResults.Rebound;
        }

        return DashCollisionResults.NormalCollision;
    }

    private bool CanActivate(Vector2 direction) {
        if (_giant && direction.X <= 0f) {
            return false;
        }

        if (_canActivate && _crushDir != direction) {
            if (direction.X != 0f && !_canMoveHorizontally) {
                return false;
            }

            return !(direction.Y != 0f && !_canMoveVertically);
        }

        return false;
    }

    private void Attack(Vector2 direction) {
        Audio.Play(_sfx.Activate, Center);

        if (_currentMoveLoopSfx != null) {
            _currentMoveLoopSfx.Param("end", 1f);
            SoundSource sfx = _currentMoveLoopSfx;
            Alarm.Set(this, 0.5f, delegate {
                sfx.RemoveSelf();
            }, Alarm.AlarmMode.Oneshot);
        }
        Add(_currentMoveLoopSfx = new SoundSource());
        _currentMoveLoopSfx.Position = new Vector2(Width, Height) / 2f;
        _currentMoveLoopSfx.Play(_sfx.MoveLoop, null, 0f);

        _face.Play("hit", false, false);
        _crushDir = direction;
        _canActivate = false;
        _attackCoroutine.Replace(AttackSequence());
        ClearRemainder();
        TurnOffImages();
        ActivateParticles(_crushDir);

        if (_crushDir.X < 0f) {
            foreach (Image image in _activeLeftImages) {
                image.Visible = true;
            }
            _nextFaceDirection = "left";
        } else if (_crushDir.X > 0f) {
            foreach (Image image in _activeRightImages) {
                image.Visible = true;
            }
            _nextFaceDirection = "right";
        } else if (_crushDir.Y < 0f) {
            foreach (Image image in _activeTopImages) {
                image.Visible = true;
            }
            _nextFaceDirection = "up";
        } else if (_crushDir.Y > 0f) {
            foreach (Image image in _activeBottomImages) {
                image.Visible = true;
            }
            _nextFaceDirection = "down";
        }

        var addToReturnStack = !IsNoReturn;
        if (_returnStack.Count > 0) {
            MoveState moveState = _returnStack[^1];
            if (moveState.Direction == direction || moveState.Direction == -direction) {
                addToReturnStack = false;
            }
        }

        if (addToReturnStack) {
            _returnStack.Add(new MoveState(Position, _crushDir));
        }
    }

    private void ActivateParticles(Vector2 dir) {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        int particleCount;
        if (dir == Vector2.UnitX) {
            direction = 0f;
            position = CenterRight - Vector2.UnitX;
            positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
            particleCount = (int) (Height / 8f) * 4;
        } else if (dir == -Vector2.UnitX) {
            direction = 3.14159274f;
            position = CenterLeft + Vector2.UnitX;
            positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
            particleCount = (int) (Height / 8f) * 4;
        } else if (dir == Vector2.UnitY) {
            direction = 1.57079637f;
            position = BottomCenter - Vector2.UnitY;
            positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
            particleCount = (int) (Width / 8f) * 4;
        } else {
            direction = -1.57079637f;
            position = TopCenter + Vector2.UnitY;
            positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
            particleCount = (int) (Width / 8f) * 4;
        }
        particleCount += 2;
        _level.Particles.Emit(CrushBlock.P_Activate, particleCount, position, positionRange, direction);
    }

    private IEnumerator AttackSequence() {
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        StartShaking(0.4f);
        yield return 0.4f;
        if (!_chillOut) {
            _canActivate = true;
        }
        StopPlayerRunIntoAnimation = false;
        bool slowing = false;
        float speed = 0f;
        Action som = null!;
        while (true) {
            if (!_chillOut) {
                speed = Calc.Approach(speed, CrushSpeed, CrushAccel * Engine.DeltaTime); // was speed, 240f, 500f
            } else {
                if (slowing || CollideCheck<SolidTiles>(Position + _crushDir * 256f)) {
                    speed = Calc.Approach(speed, 12f, CrushAccel * Engine.DeltaTime * 0.25f); // was speed, 24f, 500f

                    if (!slowing) {
                        slowing = true;
                        float duration = 0.5f;
                        Action onComplete;
                        if ((onComplete = som) == null) {
                            onComplete = som = delegate () {
                                _face.Play("hurt", false, false);
                                _currentMoveLoopSfx!.Stop(true);
                                TurnOffImages();
                            };
                        }
                        Alarm.Set(this, duration, onComplete, Alarm.AlarmMode.Oneshot);
                    }
                } else {
                    speed = Calc.Approach(speed, CrushSpeed, CrushAccel * Engine.DeltaTime); // was speed, 240f, 500f
                }
            }
            bool hit;
            if (_crushDir.X != 0f) {
                hit = MoveHCheck(speed * _crushDir.X * Engine.DeltaTime);
            } else {
                hit = MoveVCheck(speed * _crushDir.Y * Engine.DeltaTime);
            }

            if (hit) {
                break;
            }

            if (Scene.OnInterval(0.02f)) {
                Vector2 at;
                float dir;
                if (_crushDir == Vector2.UnitX) {
                    at = new Vector2(Left + 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                    dir = 3.14159274f;
                } else if (_crushDir == -Vector2.UnitX) {
                    at = new Vector2(Right - 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                    dir = 0f;
                } else if (_crushDir == Vector2.UnitY) {
                    at = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Top + 1f);
                    dir = -1.57079637f;
                } else {
                    at = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Bottom - 1f);
                    dir = 1.57079637f;
                }
                _level.Particles.Emit(CrushBlock.P_Crushing, at, dir);
            }
            yield return null;
        }

        var fallingBlock = CollideFirst<FallingBlock>(Position + _crushDir);
        fallingBlock?.Triggered = true;

        if (_crushDir == -Vector2.UnitX) {
            Vector2 add = new Vector2(0f, 2f);
            int i = 0;
            while (i < Height / 8f) {
                Vector2 at2 = new Vector2(Left - 1f, Top + 4f + i * 8);
                if (!Scene.CollideCheck<Water>(at2) && Scene.CollideCheck<Solid>(at2)) {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at2 + add, 0f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at2 - add, 0f);
                }

                i++;
            }
        } else if (_crushDir == Vector2.UnitX) {
            Vector2 add2 = new Vector2(0f, 2f);
            int j = 0;
            while (j < Height / 8f) {
                Vector2 at3 = new Vector2(Right + 1f, Top + 4f + j * 8);
                if (!Scene.CollideCheck<Water>(at3) && Scene.CollideCheck<Solid>(at3)) {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at3 + add2, 3.14159274f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at3 - add2, 3.14159274f);
                }
                j++;
            }
        } else if (_crushDir == -Vector2.UnitY) {
            Vector2 add3 = new Vector2(2f, 0f);
            int k = 0;
            while (k < Width / 8f) {
                Vector2 at4 = new Vector2(Left + 4f + k * 8, Top - 1f);
                bool flag17 = !Scene.CollideCheck<Water>(at4) && Scene.CollideCheck<Solid>(at4);
                if (flag17) {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at4 + add3, 1.57079637f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at4 - add3, 1.57079637f);
                }
                k++;
            }
        } else if (_crushDir == Vector2.UnitY) {
            Vector2 add4 = new Vector2(2f, 0f);
            int l = 0;
            while (l < Width / 8f) {
                Vector2 at5 = new Vector2(Left + 4f + l * 8, Bottom + 1f);
                if (!Scene.CollideCheck<Water>(at5) && Scene.CollideCheck<Solid>(at5)) {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at5 + add4, -1.57079637f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at5 - add4, -1.57079637f);
                }
                l++;
            }
        }
        Audio.Play(_sfx.Impact, Center);
        _level.DirectionalShake(_crushDir, 0.3f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        StartShaking(0.4f);
        StopPlayerRunIntoAnimation = true;
        SoundSource sfx = _currentMoveLoopSfx!;
        _currentMoveLoopSfx!.Param("end", 1f);
        _currentMoveLoopSfx = null;
        Alarm.Set(this, 0.5f, () => sfx.RemoveSelf(), Alarm.AlarmMode.Oneshot);
        _crushDir = Vector2.Zero;
        TurnOffImages();
        if (!_chillOut) {
            _face.Play("hurt", false, false);
            _returnLoopSfx.Play(_sfx.ReturnLoop, null, 0f);
            yield return 0.4f;
            float speed2 = 0f;
            float waypointSfxDelay = 0f;
            
            // Add a fake element when return acceleration/speed is 0 so that all the corresponding visual effects happen correctly.
            if (_returnStack.Count <= 0)
                _returnStack.Add(new MoveState(ExactPosition, default));
            
            while (_returnStack.Count > 0) {
                yield return null;
                StopPlayerRunIntoAnimation = false;
                MoveState ret = _returnStack[^1];
                speed2 = Calc.Approach(speed2, ReturnSpeed, ReturnAccel * Engine.DeltaTime);
                waypointSfxDelay -= Engine.DeltaTime;
                if (ret.Direction.X != 0f) {
                    MoveTowardsX(ret.From.X, speed2 * Engine.DeltaTime);
                }
                if (ret.Direction.Y != 0f) {
                    MoveTowardsY(ret.From.Y, speed2 * Engine.DeltaTime);
                }

                if ((ret.Direction.X == 0f || ExactPosition.X == ret.From.X) && (ret.Direction.Y == 0f || ExactPosition.Y == ret.From.Y)) {
                    speed2 = 0f;
                    _returnStack.RemoveAt(_returnStack.Count - 1);
                    StopPlayerRunIntoAnimation = true;
                    if (_returnStack.Count <= 0) {
                        _face.Play("idle", false, false);
                        if (_returnLoopSfx.Playing)
                            _returnLoopSfx.Stop(true);
                        if (waypointSfxDelay <= 0f)
                            Audio.Play(_sfx.Rest, Center);
                    } else if (waypointSfxDelay <= 0f) {
                        Audio.Play(_sfx.RestWaypoint, Center);
                    }
                    waypointSfxDelay = 0.1f;
                    StartShaking(0.2f);
                    yield return 0.2f;
                }
            }
        }
    }

    private bool MoveHCheck(float amount) {
        if (!MoveHCollideSolidsAndBounds(_level, amount, true, null))
            return false;

        if (amount < 0f && Left <= _level.Bounds.Left)
            return true;

        if (amount > 0f && Right >= _level.Bounds.Right)
            return true;

        for (int i = 1; i <= 4; i++) {
            for (int j = 1; j >= -1; j -= 2) {
                Vector2 value = new Vector2(Math.Sign(amount), i * j);
                if (!CollideCheck<Solid>(Position + value)) {
                    MoveVExact(i * j);
                    MoveHExact(Math.Sign(amount));
                    return false;
                }
            }
        }

        return true;
    }

    private bool MoveVCheck(float amount) {
        if (!MoveVCollideSolidsAndBounds(_level, amount, true, null))
            return false;
        
        if (amount < 0f && Top <= _level.Bounds.Top)
            return true;

        if (amount > 0f && Bottom >= _level.Bounds.Bottom + 32)
            return true;

        for (int i = 1; i <= 4; i++) {
            for (int j = 1; j >= -1; j -= 2) {
                Vector2 value = new Vector2(i * j, Math.Sign(amount));
                if (!CollideCheck<Solid>(Position + value)) {
                    MoveHExact(i * j);
                    MoveVExact(Math.Sign(amount));
                    return false;
                }
            }
        }

        return true;
    }

    public static ParticleType PImpact => CrushBlock.P_Impact;
    public static ParticleType PCrushing => CrushBlock.P_Crushing;
    public static ParticleType PActivate => CrushBlock.P_Activate;



    private readonly Color _fillColor;
    private Level _level;
    private bool _canActivate;
    private Vector2 _crushDir;
    private readonly List<MoveState> _returnStack;
    private readonly Coroutine _attackCoroutine;
    private readonly bool _canMoveVertically;
    private readonly bool _canMoveHorizontally;
    private readonly bool _chillOut;
    private readonly bool _giant;
    private readonly Sprite _face;
    private string _nextFaceDirection;
    private readonly List<Image> _activeTopImages;
    private readonly List<Image> _activeRightImages;
    private readonly List<Image> _activeLeftImages;
    private readonly List<Image> _activeBottomImages;
    private SoundSource? _currentMoveLoopSfx;
    private readonly SoundSource _returnLoopSfx;

    private enum Axes {
        Both,
        Horizontal,
        Vertical
    }

    private struct MoveState(Vector2 from, Vector2 direction) {
        public readonly Vector2 From = from;
        public readonly Vector2 Direction = direction;
    }
}