namespace FrostHelper;

[CustomEntity("FrostHelper/FallingBlockIgnoreSolids")]
public class FallingBlockIgnoreSolids : FallingBlock {
    public bool Wrap;
    public bool WaitForPlayer;

    public FallingBlockIgnoreSolids(EntityData data, Vector2 offset) : base(data, offset) {
        Get<Coroutine>().RemoveSelf();
        Add(new Coroutine(Sequence()));

        AllowStaticMovers = data.Bool("allowStaticMovers", true);
        Wrap = data.Bool("wrap", false);
        WaitForPlayer = data.Bool("waitForPlayer", true);
    }

    public bool PlayerFallCheckShim() => this.Invoke<bool>("PlayerFallCheck");
    public bool PlayerWaitCheckShim() => this.Invoke<bool>("PlayerWaitCheck");
    public void ShakeSfxShim() => this.Invoke("ShakeSfx");
    public void ImpactSfxShim() => this.Invoke("ImpactSfx");
    public void LandParticlesShim() => this.Invoke("LandParticles");

    private IEnumerator Sequence() {
        Level level = SceneAs<Level>();
        if (WaitForPlayer) {
            while (!Triggered && !PlayerFallCheckShim()) {
                yield return null;
            }

            while (FallDelay > 0f) {
                FallDelay -= Engine.DeltaTime;
                yield return null;
            }
        }

        while (true) {
            if (WaitForPlayer) {
                ShakeSfxShim();
                StartShaking(0f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                yield return 0.2f;

                float timer = 0.4f;
                while (timer > 0f && PlayerWaitCheckShim()) {
                    yield return null;
                    timer -= Engine.DeltaTime;
                }
                StopShaking();

                int xOffset = 2;
                while (xOffset < Width) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(xOffset, -2f))) {
                        level.Particles.Emit(P_FallDustA, 2, new Vector2(X + xOffset, Y), Vector2.One * 4f, MathHelper.PiOver2);
                    }
                    level.Particles.Emit(P_FallDustB, 2, new Vector2(X + xOffset, Y), Vector2.One * 4f);
                    xOffset += 4;
                }
            }


            float speed = 0f;
            float maxSpeed = 160f;
            while (true) {

                speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);

                MoveV(speed * Engine.DeltaTime);

                var top = GetTop();

                if (top > (level.Bounds.Bottom + 16) || (top > (level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + Vector2.UnitY))) {
                    if (Wrap) {
                        var old = Bottom;
                        var rBottom = GetBottom();
                        Bottom = level.Bounds.Top - 10f + (Bottom - rBottom);
                        var diff = Bottom - old;
                        foreach (var mover in staticMovers) {
                            mover.Entity.Position.Y += diff;
                        }
                        yield return null;

                    } else {
                        Collidable = Visible = false;
                        yield return 0.2f;
                        if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f))) {
                            yield return 0.2f;
                            SceneAs<Level>().Shake(0.3f);
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                        }

                        RemoveSelf();
                        DestroyStaticMovers();
                        yield break;
                    }

                    
                }
                yield return null;
            }
        }
    }

    public float GetTop() {
        float top = Top;
        foreach (var mover in staticMovers) {
            float entityTop = mover.Entity.Top;
            if (entityTop < top) {
                top = entityTop;
            }
        }

        return top;
    }

    public float GetBottom() {
        float bot = Bottom;
        foreach (var mover in staticMovers) {
            float entityBot = mover.Entity.Bottom;
            if (entityBot > bot) {
                bot = entityBot;
            }
        }

        return bot;
    }
}
