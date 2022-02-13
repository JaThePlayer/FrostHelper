namespace FrostHelper;

public class DirectionalFallingBlock : FallingBlock {
    public Vector2 Direction;

    public DirectionalFallingBlock(EntityData data, Vector2 offset) : base(data, offset) {
        Get<Coroutine>().RemoveSelf();
        Add(new Coroutine(Sequence()));
    }

    public bool PlayerFallCheckShim() => this.Invoke<bool>("PlayerFallCheck");
    public bool PlayerWaitCheckShim() => this.Invoke<bool>("PlayerWaitCheck");
    public void ShakeSfxShim() => this.Invoke("ShakeSfx");
    public void ImpactSfxShim() => this.Invoke("ImpactSfx");
    public void LandParticlesShim() => this.Invoke("LandParticles");

    private IEnumerator Sequence() {
        while (!Triggered && !PlayerFallCheckShim()) {
            yield return null;
        }
        while (FallDelay > 0f) {
            FallDelay -= Engine.DeltaTime;
            yield return null;
        }
        //HasStartedFalling = true;
        Level level;
        while (true)
        {
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
            int num = 2;
            while (num < Width) {
                if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(num, -2f))) {
                    SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + num, Y), Vector2.One * 4f, 1.57079637f);
                }
                SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + num, Y), Vector2.One * 4f);
                num += 4;
            }
            float speed = 0f;
            float maxSpeed = 160f;
            while (true)
            {
                level = SceneAs<Level>();
                speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                if (MoveVCollideSolids(speed * Engine.DeltaTime, true, null)) {
                    break;
                }
                if (Top > (level.Bounds.Bottom + 16) || (Top > (level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + new Vector2(0f, 1f)))) {
                    goto End;
                }
                yield return null;
            }
            ImpactSfxShim();
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
            StartShaking(0f);
            LandParticlesShim();
            yield return 0.2f;
            StopShaking();
            if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f))) {
                Safe = true;
                yield break;
            }
            while (CollideCheck<Platform>(Position + new Vector2(0f, 1f))) {
                yield return 0.1f;
            }
        }

    End:
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
