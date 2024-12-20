﻿using FrostHelper.Colliders;
using FrostHelper.Entities.Boosters;
using FrostHelper.Helpers;
using FrostHelper.ModIntegration;
using FrostHelper.SessionExpressions;
using MonoMod.ModInterop;
using System.Diagnostics.CodeAnalysis;

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
    /// Supports Maddie's Helping Hand rainbow spinner controllers.
    /// </summary>
    public static Color GetRainbowColor(Vector2 position) {
        return GetRainbowColor(Engine.Scene, position);
    }

    /// <summary>
    /// Returns the color a rainbow spinner would have at the given position in a given scene.
    /// Supports Maddie's Helping Hand rainbow spinner controllers.
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
    /// Sets the current Lightning block colors for the current <see cref="LightningRenderer"/>
    /// </summary>
    public static void SetLightningColors(Color colorA, Color colorB) {
        LightningColorTrigger.ChangeLightningColor(colorA, colorB);
    }

    /// <summary>
    /// Sets the current Lightning block colors for the current <see cref="LightningRenderer"/>
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


    /// <summary>
    /// Sets the blend state for the given backdrop
    /// </summary>
    public static void SetBackdropBlendState(Backdrop backdrop, BlendState blendState) {
        CustomBackdropBlendModeHelper.SetBlendMode(backdrop, blendState);
    }

    /// <summary>
    /// Gets the blend mode for the given backdrop.
    /// If the backdrop is a parallax, it returns the <see cref="Parallax.BlendState"/> field.
    /// Otherwise, it tries to get the blend state set by <see cref="SetBackdropBlendState(Backdrop, BlendState)"/>
    /// If that function was never called on the given backdrop, null is returned.
    /// </summary>
    public static BlendState? GetBackdropBlendState(Backdrop backdrop) {
        return CustomBackdropBlendModeHelper.GetBlendMode(backdrop);
    }

    /// <summary>
    /// Returns an easer of the name specified by <paramref name="name"/>, defaulting to <paramref name="defaultValue"/> or <see cref="Ease.Linear"/> if <paramref name="defaultValue"/> is null.
    /// Supports using lua code for the easing, where the provided code will be transformed as follows: $"return function(p){(<paramref name="name"/>.Contains("return") ? "" : " return")} {<paramref name="name"/>} end";
    /// </summary>
    public static Ease.Easer GetEaser(string name, Ease.Easer? defaultValue = null) {
        return EaseHelper.GetEase(name, defaultValue);
    }

    /// <summary>
    /// Returns a tween mode of the name specified by <paramref name="name"/>, defaulting to <paramref name="defaultValue"/> if the string was invalid. Case-sensitive.
    /// </summary>
    public static Tween.TweenMode GetTweenMode(string mode, Tween.TweenMode defaultValue) {
        return EaseHelper.GetTweenMode(mode, defaultValue);
    }

    /// <summary>
    /// Tries to retrieve an effect/shader located at 'Effects/id', returning null if the effect can't be found.
    /// An in-game notification will be shown whenever this returns null.
    /// </summary>
    public static Effect? GetEffectOrNull(string id) {
        if (ShaderHelperIntegration.TryGetEffect(id) is { } eff)
            return eff;

        ShaderHelperIntegration.NotifyAboutMissingShader(id);

        return null;
    }

    /// <summary>
    /// Tries to retrieve an effect/shader located at 'Effects/id', returning null if the effect can't be found.
    /// If the second argument is true, an in-game notification will be shown whenever this returns null.
    /// </summary>
    public static Effect? GetEffectOrNull(string id, bool notifyOnMissing) {
        if (ShaderHelperIntegration.TryGetEffect(id) is { } eff)
            return eff;

        if (notifyOnMissing)
            ShaderHelperIntegration.NotifyAboutMissingShader(id);

        return null;
    }

    /// <summary>
    /// Applies FrostHelper-standard parameters for the given effect. Should be called each frame the effect is used.
    /// </summary>
    /// <param name="viewMatrix">The shader's ViewMatrix uniform will be set to this matrix. In most cases, this should be camera.Matrix</param>
    public static void ApplyStandardParameters(Effect effect, Matrix viewMatrix) {
        effect.ApplyStandardParameters(viewMatrix);
    }

    /// <summary>
    /// Creates an object which can evaluate a Session Expression.
    /// The returned object can be passed to <see cref="GetIntSessionExpressionValue"/>
    /// Refer to https://github.com/JaThePlayer/FrostHelper/wiki/Session-Expressions
    /// </summary>
    public static bool TryCreateSessionExpression(string str, [NotNullWhen(true)] out object? expression) {
        if (ConditionHelper.TryCreate(str, out var expr)) {
            expression = expr;
            return true;
        }

        expression = null;
        return false;
    }

    /// <summary>
    /// Returns the current value of a Session Expression.
    /// The object passed as the 1st argument needs to be created via <see cref="TryCreateSessionExpression"/>
    /// </summary>
    public static object GetSessionExpressionValue(object expression, Session session) {
        var expr = AssertExpression(expression);

        return expr.Get(session);
    }
    
    /// <summary>
    /// Returns the type that the given session expression will return, or typeof(object) if that's unknown.
    /// The object passed as the 1st argument needs to be created via <see cref="TryCreateSessionExpression"/>
    /// </summary>
    public static Type GetSessionExpressionReturnedType(object expression) {
        var expr = AssertExpression(expression);

        return expr.ReturnType ?? typeof(object);
    }
    
    /// <summary>
    /// Returns the current value of a Session Expression as an integer, coercing it if needed.
    /// The object passed as the 1st argument needs to be created via <see cref="TryCreateSessionExpression"/>
    /// </summary>
    public static int GetIntSessionExpressionValue(object expression, Session session) {
        var expr = AssertExpression(expression);

        return expr.GetInt(session);
    }
    
    /// <summary>
    /// Returns the current value of a Session Expression as a float, coercing it if needed.
    /// The object passed as the 1st argument needs to be created via <see cref="TryCreateSessionExpression"/>
    /// </summary>
    public static float GetFloatSessionExpressionValue(object expression, Session session) {
        var expr = AssertExpression(expression);

        return expr.GetFloat(session);
    }
    
    /// <summary>
    /// Returns the current value of a Session Expression as a boolean, coercing it if needed.
    /// The object passed as the 1st argument needs to be created via <see cref="TryCreateSessionExpression"/>
    /// </summary>
    public static bool GetBoolSessionExpressionValue(object expression, Session session) {
        var expr = AssertExpression(expression);

        return expr.Check(session);
    }

    /// <summary>
    /// Registers a simple Session Expression command, which will be accessible via $modName.cmdName in Session Expressions.
    /// </summary>
    /// <param name="modName">Name of the mod which registers this command. Will be used to prefix the command name.</param>
    /// <param name="cmdName">Name of the command</param>
    /// <param name="func">Function called each time the command needs to be evaluated</param>
    public static void RegisterSimpleSessionExpressionCommand(string modName, string cmdName, Func<Session, object> func) {
        SimpleCommands.RegisterSimpleCommand(modName, cmdName, func);
    }

    // NON-API!
    private static ConditionHelper.Condition AssertExpression(object expression) {
        if (expression is not ConditionHelper.Condition expr) {
            throw new ArgumentException($"Object '{expression}' is not of type {nameof(ConditionHelper.Condition)}!");
        }

        return expr;
    }
}
