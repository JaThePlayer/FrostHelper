namespace FrostHelper;

[CustomEntity("FrostHelper/SlowCrushBlock")]
public class CustomCrushBlock : Solid {
    public float CrushSpeed;
    public float CrushAccel;
    public float ReturnSpeed;
    public float ReturnAccel;
    public string Directory;

    public CustomCrushBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
        CrushAccel = data.Float("crushAcceleration", 250f);
        ReturnAccel = data.Float("returnAcceleration", 160f);
        ReturnSpeed = data.Float("returnSpeed", 60f);
        CrushSpeed = data.Float("crushSpeed", 120f);
        chillOut = data.Bool("chillout", false);
        Directory = data.Attr("directory", "objects/FrostHelper/slowcrushblock/");
        if (!Directory.EndsWith("/"))
            Directory += '/';

        fillColor = Calc.HexToColor("62222b");
        idleImages = new List<Image>();
        activeTopImages = new List<Image>();
        activeRightImages = new List<Image>();
        activeLeftImages = new List<Image>();
        activeBottomImages = new List<Image>();
        OnDashCollide = new DashCollision(OnDashed);
        returnStack = new List<MoveState>();

        giant = Width >= 48f && Height >= 48f && chillOut;
        canActivate = true;
        attackCoroutine = new Coroutine(true);
        attackCoroutine.RemoveOnComplete = false;
        Add(attackCoroutine);
        List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(Directory + "block");
        MTexture idle;
        switch (data.Enum("axes", Axes.Both)) {
            default:
                idle = atlasSubtextures[3];
                canMoveHorizontally = canMoveVertically = true;
                break;
            case Axes.Horizontal:
                idle = atlasSubtextures[1];
                canMoveHorizontally = true;
                canMoveVertically = false;
                break;
            case Axes.Vertical:
                idle = atlasSubtextures[2];
                canMoveHorizontally = false;
                canMoveVertically = true;
                break;
        }
        Add(face = GFX.SpriteBank.Create(giant ? "giant_crushblock_face" : "crushblock_face"));
        face.Position = new Vector2(Width, Height) / 2f;
        face.Play("idle", false, false);
        face.OnLastFrame = delegate (string f) {
            bool flag = f == "hit";
            if (flag) {
                face.Play(nextFaceDirection, false, false);
            }
        };
        int num = (int) (Width / 8f) - 1;
        int num2 = (int) (Height / 8f) - 1;
        AddImage(idle, 0, 0, 0, 0, -1, -1);
        AddImage(idle, num, 0, 3, 0, 1, -1);
        AddImage(idle, 0, num2, 0, 3, -1, 1);
        AddImage(idle, num, num2, 3, 3, 1, 1);
        for (int i = 1; i < num; i++) {
            AddImage(idle, i, 0, Calc.Random.Choose(1, 2), 0, 0, -1);
            AddImage(idle, i, num2, Calc.Random.Choose(1, 2), 3, 0, 1);
        }
        for (int j = 1; j < num2; j++) {
            AddImage(idle, 0, j, 0, Calc.Random.Choose(1, 2), -1, 0);
            AddImage(idle, num, j, 3, Calc.Random.Choose(1, 2), 1, 0);
        }
        Add(new LightOcclude(0.2f));
        Add(returnLoopSfx = new SoundSource());
        Add(new WaterInteraction(() => crushDir != Vector2.Zero));
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        level = SceneAs<Level>();
    }

    public override void Update() {
        base.Update();

        if (crushDir == Vector2.Zero) {
            face.Position = new Vector2(Width, Height) / 2f;

            if (CollideCheck<Player>(Position + new Vector2(-1f, 0f))) {
                face.X -= 1f;
            } else if (CollideCheck<Player>(Position + new Vector2(1f, 0f))) {
                face.X += 1f;
            } else if (CollideCheck<Player>(Position + new Vector2(0f, -1f))) {
                face.Y -= 1f;
            }
        }

        currentMoveLoopSfx?.Param("submerged", Submerged ? 1 : 0);
        returnLoopSfx?.Param("submerged", Submerged ? 1 : 0);
    }

    public override void Render() {
        Vector2 position = Position;
        Position += Shake;
        Draw.Rect(X + 2f, Y + 2f, Width - 4f, Height - 4f, fillColor);
        base.Render();
        Position = position;
    }

    private bool Submerged => Scene.CollideCheck<Water>(new Rectangle((int) (Center.X - 4f), (int) Center.Y, 8, 4));

    private void AddImage(MTexture idle, int x, int y, int tx, int ty, int borderX = 0, int borderY = 0) {
        MTexture subtexture = idle.GetSubtexture(tx * 8, ty * 8, 8, 8, null);
        Vector2 imagePosition = new Vector2(x * 8, y * 8);
        bool flag = borderX != 0;
        if (flag) {
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
        Image image = new Image(subtexture);
        image.Position = imagePosition;
        Add(image);
        idleImages.Add(image);

        if (borderX != 0 || borderY != 0) {
            if (borderX < 0) {
                Image leftImg = new Image(GFX.Game[Directory + "lit_left"].GetSubtexture(0, ty * 8, 8, 8, null));
                activeLeftImages.Add(leftImg);
                leftImg.Position = imagePosition;
                leftImg.Visible = false;
                Add(leftImg);
            } else {
                if (borderX > 0) {
                    Image rightImg = new Image(GFX.Game[Directory + "lit_right"].GetSubtexture(0, ty * 8, 8, 8, null));
                    activeRightImages.Add(rightImg);
                    rightImg.Position = imagePosition;
                    rightImg.Visible = false;
                    Add(rightImg);
                }
            }

            if (borderY < 0) {
                Image topImage = new Image(GFX.Game[Directory + "lit_top"].GetSubtexture(tx * 8, 0, 8, 8, null));
                activeTopImages.Add(topImage);
                topImage.Position = imagePosition;
                topImage.Visible = false;
                Add(topImage);
            } else {
                bool flag7 = borderY > 0;
                if (flag7) {
                    Image bottomImage = new Image(GFX.Game[Directory + "lit_bottom"].GetSubtexture(tx * 8, 0, 8, 8, null));
                    activeBottomImages.Add(bottomImage);
                    bottomImage.Position = imagePosition;
                    bottomImage.Visible = false;
                    Add(bottomImage);
                }
            }
        }
    }

    private void TurnOffImages() {
        foreach (Image image in activeLeftImages) {
            image.Visible = false;
        }
        foreach (Image image2 in activeRightImages) {
            image2.Visible = false;
        }
        foreach (Image image3 in activeTopImages) {
            image3.Visible = false;
        }
        foreach (Image image4 in activeBottomImages) {
            image4.Visible = false;
        }
    }

    private DashCollisionResults OnDashed(Player player, Vector2 direction) {
        bool flag = CanActivate(-direction);
        DashCollisionResults result;
        if (flag) {
            Attack(-direction);
            result = DashCollisionResults.Rebound;
        } else {
            result = DashCollisionResults.NormalCollision;
        }
        return result;
    }

    private bool CanActivate(Vector2 direction) {
        if (giant && direction.X <= 0f) {
            return false;
        }

        if (canActivate && crushDir != direction) {
            if (direction.X != 0f && !canMoveHorizontally) {
                return false;
            }

            return !(direction.Y != 0f && !canMoveVertically);
        }

        return false;
    }

    private void Attack(Vector2 direction) {
        Audio.Play("event:/game/06_reflection/crushblock_activate", Center);

        if (currentMoveLoopSfx != null) {
            currentMoveLoopSfx.Param("end", 1f);
            SoundSource sfx = currentMoveLoopSfx;
            Alarm.Set(this, 0.5f, delegate {
                sfx.RemoveSelf();
            }, Alarm.AlarmMode.Oneshot);
        }
        Add(currentMoveLoopSfx = new SoundSource());
        currentMoveLoopSfx.Position = new Vector2(Width, Height) / 2f;
        currentMoveLoopSfx.Play("event:/game/06_reflection/crushblock_move_loop", null, 0f);

        face.Play("hit", false, false);
        crushDir = direction;
        canActivate = false;
        attackCoroutine.Replace(AttackSequence());
        ClearRemainder();
        TurnOffImages();
        ActivateParticles(crushDir);

        if (crushDir.X < 0f) {
            foreach (Image image in activeLeftImages) {
                image.Visible = true;
            }
            nextFaceDirection = "left";
        } else {
            if (crushDir.X > 0f) {
                foreach (Image image2 in activeRightImages) {
                    image2.Visible = true;
                }
                nextFaceDirection = "right";
            } else {
                if (crushDir.Y < 0f) {
                    foreach (Image image3 in activeTopImages) {
                        image3.Visible = true;
                    }
                    nextFaceDirection = "up";
                } else {
                    if (crushDir.Y > 0f) {
                        foreach (Image image4 in activeBottomImages) {
                            image4.Visible = true;
                        }
                        nextFaceDirection = "down";
                    }
                }
            }
        }

        bool addToReturnStack = true;
        if (returnStack.Count > 0) {
            MoveState moveState = returnStack[returnStack.Count - 1];
            if (moveState.Direction == direction || moveState.Direction == -direction) {
                addToReturnStack = false;
            }
        }

        if (addToReturnStack) {
            returnStack.Add(new MoveState(Position, crushDir));
        }
    }

    private void ActivateParticles(Vector2 dir) {
        bool flag = dir == Vector2.UnitX;
        float direction;
        Vector2 position;
        Vector2 positionRange;
        int num;
        if (flag) {
            direction = 0f;
            position = CenterRight - Vector2.UnitX;
            positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
            num = (int) (Height / 8f) * 4;
        } else {
            bool flag2 = dir == -Vector2.UnitX;
            if (flag2) {
                direction = 3.14159274f;
                position = CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
                num = (int) (Height / 8f) * 4;
            } else {
                bool flag3 = dir == Vector2.UnitY;
                if (flag3) {
                    direction = 1.57079637f;
                    position = BottomCenter - Vector2.UnitY;
                    positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
                    num = (int) (Width / 8f) * 4;
                } else {
                    direction = -1.57079637f;
                    position = TopCenter + Vector2.UnitY;
                    positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
                    num = (int) (Width / 8f) * 4;
                }
            }
        }
        num += 2;
        level.Particles.Emit(CrushBlock.P_Activate, num, position, positionRange, direction);
    }

    private IEnumerator AttackSequence() {
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        StartShaking(0.4f);
        yield return 0.4f;
        bool flag = !chillOut;
        if (flag) {
            canActivate = true;
        }
        StopPlayerRunIntoAnimation = false;
        bool slowing = false;
        float speed = 0f;
        Action som = null!;
        while (true) {
            if (!chillOut) {
                speed = Calc.Approach(speed, CrushSpeed, CrushAccel * Engine.DeltaTime); // was speed, 240f, 500f
            } else {
                if (slowing || CollideCheck<SolidTiles>(Position + crushDir * 256f)) {
                    speed = Calc.Approach(speed, 12f, CrushAccel * Engine.DeltaTime * 0.25f); // was speed, 24f, 500f

                    if (!slowing) {
                        slowing = true;
                        float duration = 0.5f;
                        Action onComplete;
                        if ((onComplete = som) == null) {
                            onComplete = som = delegate () {
                                face.Play("hurt", false, false);
                                currentMoveLoopSfx!.Stop(true);
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
            if (crushDir.X != 0f) {
                hit = MoveHCheck(speed * crushDir.X * Engine.DeltaTime);
            } else {
                hit = MoveVCheck(speed * crushDir.Y * Engine.DeltaTime);
            }

            if (hit) {
                break;
            }

            if (Scene.OnInterval(0.02f)) {
                Vector2 at;
                float dir;
                if (crushDir == Vector2.UnitX) {
                    at = new Vector2(Left + 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                    dir = 3.14159274f;
                } else {
                    if (crushDir == -Vector2.UnitX) {
                        at = new Vector2(Right - 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                        dir = 0f;
                    } else {
                        if (crushDir == Vector2.UnitY) {
                            at = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Top + 1f);
                            dir = -1.57079637f;
                        } else {
                            at = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Bottom - 1f);
                            dir = 1.57079637f;
                        }
                    }
                }
                level.Particles.Emit(CrushBlock.P_Crushing, at, dir);
                at = default;
            }
            yield return null;
        }

        FallingBlock fallingBlock = CollideFirst<FallingBlock>(Position + crushDir);
        if (fallingBlock != null) {
            fallingBlock.Triggered = true;
        }

        if (crushDir == -Vector2.UnitX) {
            Vector2 add = new Vector2(0f, 2f);
            int i = 0;
            while (i < Height / 8f) {
                Vector2 at2 = new Vector2(Left - 1f, Top + 4f + i * 8);
                bool flag13 = !Scene.CollideCheck<Water>(at2) && Scene.CollideCheck<Solid>(at2);
                if (flag13) {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at2 + add, 0f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at2 - add, 0f);
                }
                at2 = default;
                int num = i;
                i = num + 1;
            }
            add = default;
        } else {
            if (crushDir == Vector2.UnitX) {
                Vector2 add2 = new Vector2(0f, 2f);
                int j = 0;
                while (j < Height / 8f) {
                    Vector2 at3 = new Vector2(Right + 1f, Top + 4f + j * 8);
                    bool flag15 = !Scene.CollideCheck<Water>(at3) && Scene.CollideCheck<Solid>(at3);
                    if (flag15) {
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at3 + add2, 3.14159274f);
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at3 - add2, 3.14159274f);
                    }
                    at3 = default;
                    int num = j;
                    j = num + 1;
                }
                add2 = default;
            } else {
                bool flag16 = crushDir == -Vector2.UnitY;
                if (flag16) {
                    Vector2 add3 = new Vector2(2f, 0f);
                    int k = 0;
                    while (k < Width / 8f) {
                        Vector2 at4 = new Vector2(Left + 4f + k * 8, Top - 1f);
                        bool flag17 = !Scene.CollideCheck<Water>(at4) && Scene.CollideCheck<Solid>(at4);
                        if (flag17) {
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at4 + add3, 1.57079637f);
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at4 - add3, 1.57079637f);
                        }
                        at4 = default;
                        int num = k;
                        k = num + 1;
                    }
                    add3 = default;
                } else {
                    bool flag18 = crushDir == Vector2.UnitY;
                    if (flag18) {
                        Vector2 add4 = new Vector2(2f, 0f);
                        int l = 0;
                        while (l < Width / 8f) {
                            Vector2 at5 = new Vector2(Left + 4f + l * 8, Bottom + 1f);
                            bool flag19 = !Scene.CollideCheck<Water>(at5) && Scene.CollideCheck<Solid>(at5);
                            if (flag19) {
                                SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at5 + add4, -1.57079637f);
                                SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, at5 - add4, -1.57079637f);
                            }
                            at5 = default;
                            int num = l;
                            l = num + 1;
                        }
                        add4 = default;
                    }
                }
            }
        }
        Audio.Play("event:/game/06_reflection/crushblock_impact", Center);
        level.DirectionalShake(crushDir, 0.3f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        StartShaking(0.4f);
        StopPlayerRunIntoAnimation = true;
        SoundSource sfx = currentMoveLoopSfx!;
        currentMoveLoopSfx!.Param("end", 1f);
        currentMoveLoopSfx = null;
        Alarm.Set(this, 0.5f, () => sfx.RemoveSelf(), Alarm.AlarmMode.Oneshot);
        crushDir = Vector2.Zero;
        TurnOffImages();
        bool flag20 = !chillOut;
        if (flag20) {
            face.Play("hurt", false, false);
            returnLoopSfx.Play("event:/game/06_reflection/crushblock_return_loop", null, 0f);
            yield return 0.4f;
            float speed2 = 0f;
            float waypointSfxDelay = 0f;
            while (returnStack.Count > 0) {
                yield return null;
                StopPlayerRunIntoAnimation = false;
                CustomCrushBlock.MoveState ret = returnStack[returnStack.Count - 1];
                speed2 = Calc.Approach(speed2, ReturnSpeed, ReturnAccel * Engine.DeltaTime);
                waypointSfxDelay -= Engine.DeltaTime;
                bool flag21 = ret.Direction.X != 0f;
                if (flag21) {
                    MoveTowardsX(ret.From.X, speed2 * Engine.DeltaTime);
                }
                bool flag22 = ret.Direction.Y != 0f;
                if (flag22) {
                    MoveTowardsY(ret.From.Y, speed2 * Engine.DeltaTime);
                }
                bool atTarget = (ret.Direction.X == 0f || ExactPosition.X == ret.From.X) && (ret.Direction.Y == 0f || ExactPosition.Y == ret.From.Y);
                bool flag23 = atTarget;
                if (flag23) {
                    speed2 = 0f;
                    returnStack.RemoveAt(returnStack.Count - 1);
                    StopPlayerRunIntoAnimation = true;
                    bool flag24 = returnStack.Count <= 0;
                    if (flag24) {
                        face.Play("idle", false, false);
                        returnLoopSfx.Stop(true);
                        bool flag25 = waypointSfxDelay <= 0f;
                        if (flag25) {
                            Audio.Play("event:/game/06_reflection/crushblock_rest", Center);
                        }
                    } else {
                        if (waypointSfxDelay <= 0f) {
                            Audio.Play("event:/game/06_reflection/crushblock_rest_waypoint", Center);
                        }
                    }
                    waypointSfxDelay = 0.1f;
                    StartShaking(0.2f);
                    yield return 0.2f;
                }
                ret = default;
            }
        }
        yield break;
    }

    private bool MoveHCheck(float amount) {
        bool flag = MoveHCollideSolidsAndBounds(level, amount, true, null);
        bool result;
        if (flag) {
            bool flag2 = amount < 0f && Left <= level.Bounds.Left;
            if (flag2) {
                result = true;
            } else {
                bool flag3 = amount > 0f && Right >= level.Bounds.Right;
                if (flag3) {
                    result = true;
                } else {
                    for (int i = 1; i <= 4; i++) {
                        for (int j = 1; j >= -1; j -= 2) {
                            Vector2 value = new Vector2(Math.Sign(amount), i * j);
                            bool flag4 = !CollideCheck<Solid>(Position + value);
                            if (flag4) {
                                MoveVExact(i * j);
                                MoveHExact(Math.Sign(amount));
                                return false;
                            }
                        }
                    }
                    result = true;
                }
            }
        } else {
            result = false;
        }
        return result;
    }

    private bool MoveVCheck(float amount) {
        bool flag = MoveVCollideSolidsAndBounds(level, amount, true, null);
        bool result;
        if (flag) {
            bool flag2 = amount < 0f && Top <= level.Bounds.Top;
            if (flag2) {
                result = true;
            } else {
                bool flag3 = amount > 0f && Bottom >= level.Bounds.Bottom + 32;
                if (flag3) {
                    result = true;
                } else {
                    for (int i = 1; i <= 4; i++) {
                        for (int j = 1; j >= -1; j -= 2) {
                            Vector2 value = new Vector2(i * j, Math.Sign(amount));
                            bool flag4 = !CollideCheck<Solid>(Position + value);
                            if (flag4) {
                                MoveHExact(i * j);
                                MoveVExact(Math.Sign(amount));
                                return false;
                            }
                        }
                    }
                    result = true;
                }
            }
        } else {
            result = false;
        }
        return result;
    }

    public static ParticleType P_Impact => CrushBlock.P_Impact;
    public static ParticleType P_Crushing => CrushBlock.P_Crushing;
    public static ParticleType P_Activate => CrushBlock.P_Activate;



    private Color fillColor;
    private Level level;
    private bool canActivate;
    private Vector2 crushDir;
    private List<CustomCrushBlock.MoveState> returnStack;
    private Coroutine attackCoroutine;
    private bool canMoveVertically;
    private bool canMoveHorizontally;
    private bool chillOut;
    private bool giant;
    private Sprite face;
    private string nextFaceDirection;
    private List<Image> idleImages;
    private List<Image> activeTopImages;
    private List<Image> activeRightImages;
    private List<Image> activeLeftImages;
    private List<Image> activeBottomImages;
    private SoundSource? currentMoveLoopSfx;
    private SoundSource returnLoopSfx;

    public enum Axes {
        Both,
        Horizontal,
        Vertical
    }

    private struct MoveState {
        public MoveState(Vector2 from, Vector2 direction) {
            From = from;
            Direction = direction;
        }
        public Vector2 From;
        public Vector2 Direction;
    }
}