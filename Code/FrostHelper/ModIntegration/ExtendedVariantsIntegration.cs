using MonoMod.ModInterop;

namespace FrostHelper.ModIntegration;

// ReSharper disable InconsistentNaming 
// ReSharper disable UnassignedField.Global
#pragma warning disable CS0649 // Field is never assigned to
#pragma warning disable CA2211

// From Mapping Utils
[ModImportName("ExtendedVariantMode")]
public static class ExtVariantsAPI
{
    public static bool LoadIfNeeded()
    {
        if (Loaded)
            return true;

        typeof(ExtVariantsAPI).ModInterop();

        Loaded = true;

        return GetCurrentVariantValue is {};
    }

    private static bool Loaded { get; set; }

    public static bool Available => LoadIfNeeded() && GetCurrentVariantValue is { }; 

    // object GetCurrentVariantValue(string variantString)
    public static Func<string, object>? GetCurrentVariantValue;
    
    //TriggerVariant(string variantString, object newValue, bool revertOnDeath)
    public static Action<string, object, bool>? TriggerVariant;

    //void SetJumpCount(int jumpCount)
    public static Action<int>? SetJumpCount;
    
    //void CapJumpCount(int jumpCount)
    public static Action<int>? CapJumpCount;
    
    //int GetJumpCount()
    public static Func<int>? GetJumpCount;
    
    public static object? GetVariantValue(Variant variant)
    {
        LoadIfNeeded();

        return GetCurrentVariantValue?.Invoke(variant.ToString());
    }

    public static float? GetVariantFloat(Variant variant) => GetVariantValue(variant) switch
    {
        null => null,
        var obj => Convert.ToSingle(obj),
    };
    
    public static int? GetVariantInt(Variant variant) => GetVariantValue(variant) switch
    {
        null => null,
        var obj => Convert.ToInt32(obj),
    };
    
    public static int GetVariantInt(Variant variant, int def) => GetVariantValue(variant) switch
    {
        null => def,
        var obj => Convert.ToInt32(obj),
    };

    public static void SetVariant(Variant variant, object newValue, bool revertOnDeath)
    {
        LoadIfNeeded();

        TriggerVariant?.Invoke(variant.ToString(), newValue, revertOnDeath);
    }
    
    public enum Variant {
        Gravity, FallSpeed, JumpHeight, WallBouncingSpeed, DisableWallJumping, DisableClimbJumping, JumpCount, RefillJumpsOnDashRefill, DashSpeed, DashLength,
        HyperdashSpeed, ExplodeLaunchSpeed, DashCount, HeldDash, DontRefillDashOnGround, SpeedX, Friction, AirFriction, BadelineChasersEverywhere, ChaserCount,
        AffectExistingChasers, BadelineBossesEverywhere, BadelineAttackPattern, ChangePatternsOfExistingBosses, FirstBadelineSpawnRandom, LegacyDashSpeedBehavior,
        BadelineBossCount, BadelineBossNodeCount, BadelineLag, DelayBetweenBadelines, OshiroEverywhere, OshiroCount, ReverseOshiroCount, DisableOshiroSlowdown,
        WindEverywhere, SnowballsEverywhere, SnowballDelay, AddSeekers, DisableSeekerSlowdown, TheoCrystalsEverywhere, AllowThrowingTheoOffscreen, AllowLeavingTheoBehind,
        Stamina, UpsideDown, DisableNeutralJumping, RegularHiccups, HiccupStrength, RoomLighting, RoomBloom, GlitchEffect, EverythingIsUnderwater, ForceDuckOnGround,
        InvertDashes, InvertGrab, AllStrawberriesAreGoldens, GameSpeed, ColorGrading, JellyfishEverywhere, RisingLavaEverywhere, RisingLavaSpeed, InvertHorizontalControls,
        BounceEverywhere, SuperdashSteeringSpeed, ScreenShakeIntensity, AnxietyEffect, BlurLevel, ZoomLevel, DashDirection, BackgroundBrightness, DisableMadelineSpotlight,
        ForegroundEffectOpacity, MadelineIsSilhouette, DashTrailAllTheTime, DisableClimbingUpOrDown, SwimmingSpeed, BoostMultiplier, FriendlyBadelineFollower,
        DisableRefillsOnScreenTransition, RestoreDashesOnRespawn, DisableSuperBoosts, DisplayDashCount, MadelineHasPonytail, MadelineBackpackMode, InvertVerticalControls,
        DontRefillStaminaOnGround, EveryJumpIsUltra, CoyoteTime, BackgroundBlurLevel, NoFreezeFrames, PreserveExtraDashesUnderwater, AlwaysInvisible, DisplaySpeedometer,
        WallSlidingSpeed, DisableJumpingOutOfWater, DisableDashCooldown, DisableKeysSpotlight, JungleSpidersEverywhere, CornerCorrection, PickupDuration,
        MinimumDelayBeforeThrowing, DelayBeforeRegrabbing, DashTimerMultiplier, JumpDuration, HorizontalSpringBounceDuration, HorizontalWallJumpDuration,
        ResetJumpCountOnGround, UltraSpeedMultiplier, JumpCooldown, SpinnerColor, WallJumpDistance, WallBounceDistance, DashRestriction, CorrectedMirrorMode,
        // vanilla variants
        AirDashes, DashAssist, VanillaGameSpeed, Hiccups, InfiniteStamina, Invincible, InvisibleMotion, LowFriction, MirrorMode, NoGrabbing, PlayAsBadeline,
        SuperDashing, ThreeSixtyDashing
    }
}

#pragma warning restore CS0649
