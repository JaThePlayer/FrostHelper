using FMOD.Studio;

namespace FrostHelper;

[CustomEntity("FrostHelper/ToggleSwapBlock")]
[Tracked(false)]
public class ToggleSwapBlock : Solid {
    string directory = "objects/swapblock";
    bool renderBG = false;

    public string MoveSFX, MoveEndSFX;
    public bool EmitParticles;
    public Color? ParticleColor;

    public ToggleSwapBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, true) {
        MoveSFX = data.Attr("moveSFX", "event:/game/05_mirror_temple/swapblock_move");
        MoveEndSFX = data.Attr("moveEndSFX", "event:/game/05_mirror_temple/swapblock_move_end");

        directory = data.Attr("directory", "objects/swapblock");
        if (directory == "objects/swapblock") {
            directory = data.Attr("sprite", "objects/swapblock");
        }
        renderBG = data.Bool("renderBG", false);
        Vector2 node = data.Nodes[0] + offset;
        redAlpha = 1f;
        start = Position;
        end = node;
        maxForwardSpeed = data.Float("speed", 360f) / Vector2.Distance(start, end);
        maxBackwardSpeed = maxForwardSpeed * 0.4f;
        Direction.X = Math.Sign(end.X - start.X);
        Direction.Y = Math.Sign(end.Y - start.Y);
        Add(new DashListener {
            OnDash = OnDash
        });
        int num = (int) MathHelper.Min(X, node.X);
        int num2 = (int) MathHelper.Min(Y, node.Y);
        int num3 = (int) MathHelper.Max(X + Width, node.X + Width);
        int num4 = (int) MathHelper.Max(Y + Height, node.Y + Height);
        moveRect = new Rectangle(num, num2, num3 - num, num4 - num2);
        MTexture mtexture = GFX.Game[directory + "/block"];
        MTexture mtexture2 = GFX.Game[directory + "/blockRed"];
        MTexture mtexture3 = GFX.Game[directory + "/target"];
        nineSliceGreen = new MTexture[3, 3];
        nineSliceRed = new MTexture[3, 3];
        nineSliceTarget = new MTexture[3, 3];
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                nineSliceGreen[i, j] = mtexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                nineSliceRed[i, j] = mtexture2.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                nineSliceTarget[i, j] = mtexture3.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
            }
        }
        middleGreen = new Sprite(GFX.Game, directory + "/midBlock");
        middleGreen.AddLoop("idle", "", 0.08f);
        middleGreen.Justify = new Vector2(0.5f, 0.5f);
        middleGreen.Play("idle");
        Add(middleGreen);
        middleRed = new Sprite(GFX.Game, directory + "/midBlockRed");
        middleRed.AddLoop("idle", "", 0.08f);
        middleRed.Justify = new Vector2(0.5f, 0.5f);
        middleRed.Play("idle");

        Add(middleRed);
        Add(new LightOcclude(0.2f));
        Depth = -9999;

        ParticleType = new ParticleType(SwapBlock.P_Move) {
            Color = Calc.HexToColor(data.Attr("particleColor1", "fbf236")),
            Color2 = Calc.HexToColor(data.Attr("particleColor2", "6abe30"))
        };

        EmitParticles = data.Bool("emitParticles", true);
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        scene.Add(path = new PathRenderer(this));
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        Audio.Stop(moveSfx, true);
        Audio.Stop(returnSfx, true);
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        Audio.Stop(moveSfx, true);
        Audio.Stop(returnSfx, true);
    }

    private void OnDash(Vector2 direction) {

        Swapping = lerp < 1f;
        Audio.Stop(returnSfx, true);
        Audio.Stop(moveSfx, true);
        moveSfx = Audio.Play(MoveSFX, Center);
        target = (target == 1) ? 0 : 1;
        returnTimer = 0.8f;
        burst = (Scene as Level)!.Displacement.AddBurst(Center, 0.2f, 0f, 16f, 1f, null, null);
        if (lerp >= 0.2f) {
            speed = maxForwardSpeed;
        } else {
            speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
        }
        if (!Swapping) {
            Audio.Play(MoveEndSFX, Center);
            return;
        }
    }

    public override void Update() {
        base.Update();
        if (returnTimer > 0f) {
            returnTimer -= Engine.DeltaTime;
            if (returnTimer <= 0f) {
                speed = 0f;
            }
        }
        if (burst != null) {
            burst.Position = Center;
        }
        redAlpha = Calc.Approach(redAlpha, (target == 1) ? 0 : 1, Engine.DeltaTime * 32f);
        if (target == 0 && lerp == 0f) {
            middleRed.SetAnimationFrame(0);
            middleGreen.SetAnimationFrame(0);
        }
        if (target == 1) {
            speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
        } else {
            speed = Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime);
        }
        float num = lerp;
        lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
        if (lerp != num) {
            Vector2 vector = (end - start) * speed;
            Vector2 position = Position;
            if (target == 1) {
                vector = (end - start) * maxForwardSpeed;
            }
            if (lerp < num) {
                vector *= -1f;
            }
            if (/*target == 1 &&*/ Scene.OnInterval(0.02f)) {
                MoveParticles(target switch {
                    1 => end - start,
                    _ => start - end,
                });
            }
            MoveTo(Vector2.Lerp(start, end, lerp), vector);
            if (position != Position) {
                Audio.Position(moveSfx, Center);
                Audio.Position(returnSfx, Center);
                if (Position == start && target == 0) {
                    Audio.SetParameter(returnSfx, "end", 1f);
                    Audio.Play(MoveEndSFX, Center);
                } else if (Position == end && target == 1) {
                    Audio.SetParameter(moveSfx, "end", 1f);
                    Audio.Play(MoveEndSFX, Center);
                }
            }
        }
        if (Swapping && lerp >= 1f) {
            Swapping = false;
        }
        //Audio.Stop(this.returnSfx, true);
        //Audio.Stop(this.moveSfx, true);
        StopPlayerRunIntoAnimation = lerp <= 0f || lerp >= 1f;
    }

    private void MoveParticles(Vector2 normal) {
        if (!EmitParticles) {
            return;
        }

        Vector2 position;
        Vector2 positionRange;
        float direction;
        float newParticles;
        if (normal.X > 0f) {
            position = CenterLeft;
            positionRange = Vector2.UnitY * (Height - 6f);
            direction = 3.14159274f;
            newParticles = Math.Max(2f, Height / 14f);
        } else if (normal.X < 0f) {
            position = CenterRight;
            positionRange = Vector2.UnitY * (Height - 6f);
            direction = 0f;
            newParticles = Math.Max(2f, Height / 14f);
        } else if (normal.Y > 0f) {
            position = TopCenter;
            positionRange = Vector2.UnitX * (Width - 6f);
            direction = -1.57079637f;
            newParticles = Math.Max(2f, Width / 14f);
        } else {
            position = BottomCenter;
            positionRange = Vector2.UnitX * (Width - 6f);
            direction = 1.57079637f;
            newParticles = Math.Max(2f, Width / 14f);
        }
        particlesRemainder += newParticles;
        int particleAmt = (int) particlesRemainder;
        particlesRemainder -= particleAmt;
        positionRange *= 0.5f;

        SceneAs<Level>().Particles.Emit(ParticleType, particleAmt, position, positionRange, direction);
    }

    public override void Render() {
        Vector2 vector = Position + Shake;
        if (lerp != target && speed > 0f) {
            Vector2 value = (end - start).SafeNormalize();
            if (target == 1) {
                value *= -1f;
            }
            float num = speed / maxForwardSpeed;
            float num2 = 16f * num;
            int num3 = 2;
            while (num3 < num2) {
                DrawBlockStyle(vector + value * num3, Width, Height, nineSliceGreen, middleGreen, Color.White * (1f - num3 / num2));
                num3 += 2;
            }
        }
        if (redAlpha < 1f) {
            DrawBlockStyle(vector, Width, Height, nineSliceGreen, middleGreen, Color.White);
        }
        if (redAlpha > 0f) {
            DrawBlockStyle(vector, Width, Height, nineSliceRed, middleRed, Color.White * redAlpha);
        }
    }

    private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite? middle, Color color) {
        int num = (int) (width / 8f);
        int num2 = (int) (height / 8f);
        ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
        ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
        ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
        ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
        for (int i = 1; i < num - 1; i++) {
            ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
            ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
        }
        for (int j = 1; j < num2 - 1; j++) {
            ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
            ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
        }
        for (int k = 1; k < num - 1; k++) {
            for (int l = 1; l < num2 - 1; l++) {
                ninSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, color);
            }
        }
        if (middle != null) {
            middle.Color = color;
            middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
            middle.Render();
        }
    }

    public ParticleType ParticleType;

    private const float ReturnTime = 0.8f;

    public Vector2 Direction;

    public bool Swapping;

    private Vector2 start;

    private Vector2 end;

    private float lerp;

    private int target;

    private Rectangle moveRect;

    private float speed;

    private float maxForwardSpeed;

    private float maxBackwardSpeed;

    private float returnTimer;

    private float redAlpha;

    private MTexture[,] nineSliceGreen;

    private MTexture[,] nineSliceRed;

    private MTexture[,] nineSliceTarget;

    private Sprite middleGreen;

    private Sprite middleRed;

    private PathRenderer path;

    private EventInstance moveSfx;

    private EventInstance returnSfx;

    private DisplacementRenderer.Burst burst;

    private float particlesRemainder;

    private class PathRenderer : Entity {
        public PathRenderer(ToggleSwapBlock block) {
            clipTexture = new MTexture();
            //base..ctor(block.Position);
            this.block = block;
            Depth = 8999;
            pathTexture = GFX.Game[block.directory + "/path" + ((block.start.X == block.end.X) ? "V" : "H")];
            timer = Calc.Random.NextFloat();
        }

        public override void Update() {
            base.Update();
            timer += Engine.DeltaTime * 4f;
        }

        public override void Render() {
            for (int i = block.moveRect.Left; i < block.moveRect.Right; i += pathTexture.Width) {
                for (int j = block.moveRect.Top; j < block.moveRect.Bottom; j += pathTexture.Height) {
                    pathTexture.GetSubtexture(0, 0, Math.Min(pathTexture.Width, block.moveRect.Right - i), Math.Min(pathTexture.Height, block.moveRect.Bottom - j), clipTexture);
                    if (block.renderBG)
                        clipTexture.DrawCentered(new Vector2(i + clipTexture.Width / 2, j + clipTexture.Height / 2), Color.White);
                }
            }
            float scale = 0.5f * (0.5f + ((float) Math.Sin(timer) + 1f) * 0.25f);
            block.DrawBlockStyle(new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, null, Color.White * scale);
        }

        private ToggleSwapBlock block;

        private MTexture pathTexture;

        private MTexture clipTexture;

        private float timer;
    }
}
