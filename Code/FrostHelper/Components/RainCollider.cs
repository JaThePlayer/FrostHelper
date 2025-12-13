namespace FrostHelper.Components;

[Tracked]
internal sealed class RainCollider(Collider collider, bool stationary) : Component(false, false) {
    internal Collider Collider { get; } = collider;

    internal bool Stationary { get; } = stationary;

    internal bool MakeSplashes { get; init; } = true;
    
    internal float PassThroughChance { get; init; } = 0f;
    
    internal Func<DynamicRainGenerator.Rain, bool>? TryCollide { get; set; } = null;
    
    internal delegate bool OnHitCallback(ParticleSystem particleSystem, ref DynamicRainGenerator.Rain rain);
    
    internal OnHitCallback? OnMakeSplashes { get; set; } = null;
    
    internal bool TryHit(ref DynamicRainGenerator.Rain pos) {
        return TryCollide?.Invoke(pos) ?? true;
    }

    internal void MakeSplashesImpl(ParticleSystem particleSystem, ref DynamicRainGenerator.Rain rain) {
        if (OnMakeSplashes?.Invoke(particleSystem, ref rain) ?? false)
            return;
        
        particleSystem.Emit(
            Water.P_Splash, // WaterInteraction.P_Drip,
            rain.Position.ToXna(), rain.Color, float.Pi + rain.Rotation);
    }
}