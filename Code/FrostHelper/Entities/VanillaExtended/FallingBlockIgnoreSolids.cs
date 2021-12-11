using Celeste.Mod.Entities;
using Platform = Celeste.Platform;

namespace FrostHelper;

[CustomEntity("FrostHelper/FallingBlockIgnoreSolids")]
public class FallingBlockIgnoreSolids : FallingBlock {
    public FallingBlockIgnoreSolids(EntityData data, Vector2 offset) : base(data, offset) {
        Get<Coroutine>().RemoveSelf();
        Add(new Coroutine(Sequence()));
    }

    public bool PlayerFallCheckShim() => this.Invoke<bool>("PlayerFallCheck");
    public bool PlayerWaitCheckShim() => this.Invoke<bool>("PlayerWaitCheck");
    public void ShakeSfxShim() => this.Invoke("ShakeSfx");
    public void ImpactSfxShim() => this.Invoke("ImpactSfx");
    public void LandParticlesShim() => this.Invoke("LandParticles");

    private IEnumerator Sequence() {
        Level level = SceneAs<Level>();
        while (!Triggered && !PlayerFallCheckShim()) {
            yield return null;
        }

        while (FallDelay > 0f) {
            FallDelay -= Engine.DeltaTime;
            yield return null;
        }
        //this.SetValue("HasStartedFalling", true);
        while (true) {
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

            float speed = 0f;
            float maxSpeed = 160f;
            while (true) {

                speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);

                //if (MoveVCollideSolids(speed * Engine.DeltaTime, true, null)) {
                //    break;
                //}
                MoveV(speed * Engine.DeltaTime);

                if (Top > (level.Bounds.Bottom + 16) || (Top > (level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + Vector2.UnitY))) {
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
                yield return null;
            }

            ImpactSfxShim();
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
            StartShaking(0f);
            LandParticlesShim();
            yield return 0.2f;
            StopShaking();
            if (CollideCheck<SolidTiles>(Position + Vector2.UnitY)) {
                Safe = true;
                yield break;
            }
            while (CollideCheck<Platform>(Position + Vector2.UnitY)) {
                yield return 0.1f;
            }
        }
    }
}
