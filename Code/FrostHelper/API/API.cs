using FrostHelper.Colliders;
using FrostHelper.Entities.Boosters;
using FrostHelper.Helpers;
using MonoMod.ModInterop;

namespace FrostHelper.API;

[ModExportName("FrostHelper")]
public static class API {
    public static int Version => 1;

    public static void SetCustomBoostState(Player player, GenericCustomBooster booster) {
        player.SetAttached(booster);
        player.StateMachine.State = GenericCustomBooster.CustomBoostState;
    }

    public static bool IsInCustomBoostState(Player player) {
        return player.StateMachine.State == GenericCustomBooster.CustomBoostState;
    }

    public static int GetCustomFeatherStateId() => CustomFeather.CustomFeatherState;
    public static int GetCustomBoostStateId() => GenericCustomBooster.CustomBoostState;
    public static int GetCustomRedBoostStateId() => GenericCustomBooster.CustomRedBoostState;
    public static int GetHeldDashStateId() => HeldRefill.HeldDashState;

    /// <summary>
    /// Converts an entity name to a Type.
    /// Returns a dummy type if the name doesn't correspond to any entity.
    /// </summary>
    public static Type EntityNameToType(string entityName) {
        return TypeHelper.EntityNameToType(entityName);
    }

    /// <summary>
    /// Converts an entity name to a Type.
    /// Returns null if the name doesn't correspond to any entity.
    /// </summary>
    public static Type? EntityNameToTypeOrNull(string entityName) {
        return TypeHelper.EntityNameToTypeSafe(entityName);
    }

    /// <summary>
    /// Returns an array of types from a comma-separated string of types.
    /// These types could either be c# type names, OR entity ID's.
    /// In case of an empty string, an empty array is returned.
    /// 
    /// Example input: jumpthru,FrostHelper/SpringLeft,FrostHelper.DirectionalPuffer
    /// </summary>
    public static Type[] GetTypes(string typeString) 
        => FrostModule.GetTypes(typeString);

    /// <inheritdoc cref="GetTypes(string)"/>
    public static List<Type> GetTypesAsList(string typeString)
        => FrostModule.GetTypesAsList(typeString);

    /// <inheritdoc cref="GetTypes(string)"/>
    public static HashSet<Type> GetTypesAsHashSet(string typeString)
        => FrostModule.GetTypesAsHashSet(typeString);

    public static string? EntityNameFromType(Type entityType) => TypeHelper.TypeToEntityName(entityType);
    public static string? EntityNameFromTypeName(string entityTypeName) => TypeHelper.TypeNameToEntityName(entityTypeName);
    public static IEnumerable<string?> EntityNamesFromTypeNames(IEnumerable<string> entityTypeNames) =>
        entityTypeNames.Select(name => TypeHelper.TypeNameToEntityName(name)).Where(s => s is not null);
    /// <summary>
    /// Returns an array of types from an <see cref="IEnumerable{T}"/> of entity types.
    /// These types could either be c# type names, OR entity ID's.
    /// In case of an empty string, an empty array is returned.
    /// 
    /// Example input: jumpthru,FrostHelper/SpringLeft,FrostHelper.DirectionalPuffer
    /// </summary>
    public static Type[] GetTypes(IEnumerable<string> types) {
        return types.Select(type => EntityNameToType(type)).ToArray();
    }

    /// <inheritdoc cref="GetTypes(IEnumerable{string})"/>
    public static List<Type> GetTypesAsList(IEnumerable<string> types) {
        return types.Select(type => EntityNameToType(type)).ToList();
    }

    /// <summary>
    /// Converts a string representation of a color into an XNA color struct.
    /// Possible formats:
    /// RRGGBBAA,
    /// RRGGBB,
    /// Xna Color Name (case insensitive)
    /// 
    /// </summary>
    public static Color GetColor(string colorString) {
        return ColorHelper.GetColor(colorString);
    }

    /// <summary>
    /// Returns the color a rainbow spinner would have at the given position.
    /// Supports Max's Helping Hand rainbow spinner controllers.
    /// </summary>
    public static Color GetRainbowColor(Vector2 position) {
        return GetRainbowColor(Engine.Scene, position);
    }

    /// <summary>
    /// Returns the color a rainbow spinner would have at the given position in a given scene.
    /// Supports Max's Helping Hand rainbow spinner controllers.
    /// </summary>
    public static Color GetRainbowColor(Scene scene, Vector2 position) {
        return ColorHelper.GetHue(scene, position);
    }

    /// <summary>
    /// Gets the attach group of a given Entity
    /// </summary>
    public static int GetAttachGroup(Entity entity) {
        return entity switch {
            CustomSpinner sp => sp.AttachGroup,
            _ => entity.Get<GroupedStaticMover>()?.Group ?? -1,
        };
    }

    /// <summary>
    /// Converts a StaticMover into a GroupedStaticMover, returning it
    /// </summary>
    public static Component ToGroupedStaticMover(StaticMover staticMover, int attachGroup) => ToGroupedStaticMover(staticMover, attachGroup, true);
    public static Component ToGroupedStaticMover(StaticMover staticMover, int attachGroup, bool canBeLeader) {
        return new GroupedStaticMover(attachGroup, canBeLeader) {
            JumpThruChecker = staticMover.JumpThruChecker,
            OnDestroy = staticMover.OnDestroy,
            OnDisable = staticMover.OnDisable,
            OnEnable = staticMover.OnEnable,
            Active = staticMover.Active,
            OnMove = staticMover.OnMove,
            OnShake = staticMover.OnShake,
        }.SetOnAttach(staticMover.OnAttach);
    }

    /// <summary>
    /// Destroys a CustomSpinner or vanilla spinner
    /// </summary>
    public static void DestroySpinner(Entity spinner, bool boss) {
        switch (spinner) {
            case CustomSpinner sp:
                sp.Destroy(boss); 
                break;
            case CrystalStaticSpinner sp:
                sp.Destroy(boss);
                break;
        };
    }

    /// <summary>
    /// Sets the tint of a given custom spinner.
    /// Note: while this function accepts any entity, it will only change entities of type CustomSpinner
    /// </summary>
    public static void SetCustomSpinnerColor(Entity spinner, Color color) {
        if (spinner is CustomSpinner cs) {
            cs.SetColor(color);
        }
    }

    /// <summary>
    /// Sets the border color of a given custom spinner.
    /// Note: while this function accepts any entity, it will only change entities of type CustomSpinner
    /// </summary>
    public static void SetCustomSpinnerBorderColor(Entity spinner, Color color) {
        if (spinner is CustomSpinner cs) {
            cs.SetBorderColor(color);
        }
    }

    public static Collider CreateShapeCollider(Vector2[] points) {
         return new ShapeHitbox(points);
    }

    /// <summary>
    /// Updates a specified point of a ShapeCollider. The argument is a Collider only to avoid needing a hard reference to Frost Helper, this function only accepts a ShapeCollider and will throw an <see cref="ArgumentException"/> otherwise.
    /// </summary>
    public static void UpdateShapeColliderPoint(Collider shapeCollider, int pointIndex, Vector2 point) {
        if (shapeCollider is ShapeHitbox shape) {
            shape.Points[pointIndex] = point;
        } else {
            throw new ArgumentException($"The first argument to {nameof(UpdateShapeColliderPoint)} must be a {nameof(ShapeHitbox)}!");
        }
    }

    /// <summary>
    /// Gets the current Lightning block colors
    /// </summary>
    public static void GetLightningColors(out Color colorA, out Color colorB, out Color fillColor, out float fillColorMultiplier) {
        LightningColorTrigger.GetColors(out colorA, out colorB, out fillColor, out fillColorMultiplier);
    }

    public static void GetLightningFillColor(out Color fillColor, out float fillColorMultiplier) {
        fillColor = ColorHelper.GetColor(LightningColorTrigger.GetFillColorString());
        fillColorMultiplier = LightningColorTrigger.GetLightningFillColorMultiplier();
    }

    /// <summary>
    /// Sets the current Lightning block colors for the current <see cref="LightingRenderer"/>
    /// </summary>
    public static void SetLightningColors(Color colorA, Color colorB) {
        LightningColorTrigger.ChangeLightningColor(colorA, colorB);
    }

    /// <summary>
    /// Sets the current Lightning block colors for the current <see cref="LightingRenderer"/>
    /// </summary>
    public static void SetLightningColors(Color[] colors) {
        LightningColorTrigger.ChangeLightningColor(colors);
    }

    /// <summary>
    /// Sets the current Lightning block colors for a given <paramref name="renderer"/>
    /// </summary>
    public static void SetLightningColors(LightningRenderer? renderer, Color colorA, Color colorB) {
        LightningColorTrigger.ChangeLightningColor(renderer, colorA, colorB);
    }

    /// <summary>
    /// Sets the current Lightning block colors for a given <paramref name="renderer"/>
    /// </summary>
    public static void SetLightningColors(LightningRenderer? renderer, Color[] colors) {
        LightningColorTrigger.ChangeLightningColor(renderer, colors);
    }

    public static Color GetBloomColor() => BloomColorChange.Color;
    public static void SetBloomColor(Color color) => BloomColorChange.Color = color;


    /// <summary>
    /// Checks whether the given spring is a <see cref="CustomSpring"/> with Orientation set to <see cref="CustomOrientations.Ceiling"/>
    /// </summary>
    public static bool IsCeilingSpring(Spring spring) => spring is CustomSpring { Orientation: CustomSpring.CustomOrientations.Ceiling };

    /// <summary>
    /// Gets the speed multiplier for a given spring. For vanilla springs, this will return <see cref="Vector2.One"/>
    /// </summary>
    public static Vector2 GetSpringSpeedMultiplier(Spring spring) => spring switch {
        CustomSpring spr => spr.speedMult,
        _ => Vector2.One
    };
}
