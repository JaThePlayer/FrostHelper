using FrostHelper.Components;

namespace FrostHelper.API;

// [ModExportName("FrostHelper")] - defined in API.cs
public partial class Api_NOT_YET_READY {
    /// <summary>
    /// Creates a new RainCollider component, which blocks Dynamic Rain.
    /// </summary>
    /// <param name="collider"></param>
    /// <param name="stationary"></param>
    /// <param name="passThroughChance">The chance for a rain droplet to pass through this collider</param>
    /// <param name="makeSplashes">Whether the collider should make splashes</param>
    public static Component MakeRainCollider(Collider collider, bool stationary, float passThroughChance, bool makeSplashes) {
        return new RainCollider(collider, stationary) {
            PassThroughChance = passThroughChance,
            MakeSplashes = makeSplashes
        };
    }

    /// <summary>
    /// Sets the TryCollide callback for the given RainCollider, which gets called when checking for collision with this collider,
    /// after the rain droplet is checked to have collided with RainCollider.Collider. If false is returned, the droplet will pass through the collider.
    /// </summary>
    /// <param name="component">A RainCollider, created previously via <see cref="MakeRainCollider"/></param>
    /// <param name="tryCollide">The callback.</param>
    public static void SetRainColliderTryCollide(Component component, Func<System.Numerics.Vector2, bool> tryCollide) {
        if (component is RainCollider collider) {
            collider.TryCollide = (r) => tryCollide(r.Position);
        }
    }
    
    /// <summary>
    /// Sets the OnMakeSplashes callback for the given RainCollider, which gets called when a droplet *actually* hits the collider (after all collision checks and callbacks).
    /// Only called if RainCollider.MakeSplashes is true and the rain droplet is in-camera.
    /// Can be used to create particles. The callback returns whether particles/some other visual was created which should replace the built-in default particles.
    /// </summary>
    /// <param name="component">A RainCollider, created previously via <see cref="MakeRainCollider"/></param>
    /// <param name="onHit">The callback, params are (particles, rain.Position)</param>
    public static void SetRainColliderOnMakeSplashes(Component component, Func<ParticleSystem, NumVector2, bool> onHit) {
        if (component is RainCollider collider) {
            collider.OnMakeSplashes = (ParticleSystem p, ref DynamicRainGenerator.Rain rain) => onHit(p, rain.Position);
        }
    }
    
    /// <param name="component">A RainCollider, created previously via <see cref="MakeRainCollider"/></param>
    /// <param name="onHit">The callback, params are (particles, rain.Position, rain.Speed, rain.Scale, rain.Rotation, rain.Color)</param>
    public static void SetRainColliderOnMakeSplashes(Component component, Func<ParticleSystem, NumVector2, NumVector2, NumVector2, float, Color, bool> onHit) {
        if (component is RainCollider collider) {
            collider.OnMakeSplashes = (ParticleSystem p, ref DynamicRainGenerator.Rain rain) => onHit(p, rain.Position, rain.Speed, rain.Scale.ToNumerics(), rain.Rotation, rain.Color);
        }
    }
}