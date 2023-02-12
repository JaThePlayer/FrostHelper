using Celeste.Mod.Helpers;
using FrostHelper.Helpers;

namespace FrostHelper;

public static class TypeHelper {
    private static Dictionary<string, Type>? entityNameToType = null;

    [OnLoad]
    public static void Load() {
        On.Celeste.Mod.Everest.Loader.LoadModAssembly += Loader_LoadModAssembly;
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Mod.Everest.Loader.LoadModAssembly -= Loader_LoadModAssembly;
    }

    // Clear the cache if a mod is loaded/hot reloaded
    private static void Loader_LoadModAssembly(On.Celeste.Mod.Everest.Loader.orig_LoadModAssembly orig, EverestModuleMetadata meta, Assembly asm) {
        orig(meta, asm);
        entityNameToType = null!;
    }

    public static Type EntityNameToType(string entityName) {
        var type = EntityNameToTypeSafe(entityName);
        if (type is { }) {
            return type;
        }
        NotificationHelper.Notify($"Unknown Entity Name: {entityName}");
        return typeof(TypeHelper);

        //return EntityNameToTypeSafe(entityName) ?? throw new Exception($"Unknown entity name: {entityName}.");
    }

    public static Type? EntityNameToTypeSafe(string entityName) {
        if (entityNameToType is null) {
            CreateCache();
        }

        if (entityNameToType!.TryGetValue(entityName, out var ret))
            return ret;

        // see if this is a C# type name
        if (FakeAssembly.GetFakeEntryAssembly().GetType(entityName, false, true) is { } csType) {
            entityNameToType[entityName] = csType;
            return csType;
        }

        return null;
    }

    private static string? TryFindTypeToEntityName(Dictionary<string, Type>? dict, Type targetType) {
        if (dict is null)
            return null;

        foreach (var item in dict) {
            if (ReferenceEquals(item.Value, targetType))
                return item.Key;
        }

        return null;
    }

    public static string? TypeToEntityName(Type entityType) {
        return TryFindTypeToEntityName(entityNameToType, entityType);
    }

    public static string? TypeNameToEntityName(string typeName) {
        var type = EntityNameToTypeSafe(typeName);
        return type is null ? null : TypeToEntityName(type);
    }

    private static void CreateCache() {
        entityNameToType = new(StringComparer.Ordinal) {
            ["checkpoint"] = typeof(Checkpoint),
            ["jumpThru"] = typeof(JumpthruPlatform),
            ["refill"] = typeof(Refill),
            ["infiniteStar"] = typeof(FlyFeather),
            ["strawberry"] = typeof(Strawberry),
            ["memorialTextController"] = typeof(Strawberry),
            ["goldenBerry"] = typeof(Strawberry),
            ["summitgem"] = typeof(SummitGem),
            ["blackGem"] = typeof(HeartGem),
            ["dreamHeartGem"] = typeof(DreamHeartGem),
            ["spring"] = typeof(Spring),
            ["wallSpringLeft"] = typeof(Spring),
            ["wallSpringRight"] = typeof(Spring),
            ["fallingBlock"] = typeof(FallingBlock),
            ["zipMover"] = typeof(ZipMover),
            ["crumbleBlock"] = typeof(CrumblePlatform),
            ["dreamBlock"] = typeof(DreamBlock),
            ["touchSwitch"] = typeof(TouchSwitch),
            ["switchGate"] = typeof(SwitchGate),
            ["negaBlock"] = typeof(NegaBlock),
            ["key"] = typeof(Key),
            ["lockBlock"] = typeof(LockBlock),
            ["movingPlatform"] = typeof(MovingPlatform),
            ["rotatingPlatforms"] = typeof(RotatingPlatform),
            ["blockField"] = typeof(BlockField),
            ["cloud"] = typeof(Cloud),
            ["booster"] = typeof(Booster),
            ["moveBlock"] = typeof(MoveBlock),
            ["light"] = typeof(PropLight),
            ["switchBlock"] = typeof(SwapBlock),
            ["swapBlock"] = typeof(SwapBlock),
            ["dashSwitchH"] = typeof(DashSwitch),
            ["dashSwitchV"] = typeof(DashSwitch),
            ["templeGate"] = typeof(TempleGate),
            ["torch"] = typeof(Torch),
            ["templeCrackedBlock"] = typeof(TempleCrackedBlock),
            ["seekerBarrier"] = typeof(SeekerBarrier),
            ["theoCrystal"] = typeof(TheoCrystal),
            ["glider"] = typeof(Glider),
            ["theoCrystalPedestal"] = typeof(TheoCrystalPedestal),
            ["badelineBoost"] = typeof(BadelineBoost),
            ["cassette"] = typeof(Cassette),
            ["cassetteBlock"] = typeof(CassetteBlock),
            ["wallBooster"] = typeof(WallBooster),
            ["bounceBlock"] = typeof(BounceBlock),
            ["coreModeToggle"] = typeof(CoreModeToggle),
            ["iceBlock"] = typeof(IceBlock),
            ["fireBarrier"] = typeof(FireBarrier),
            ["eyebomb"] = typeof(Puffer),
            ["flingBird"] = typeof(FlingBird),
            ["flingBirdIntro"] = typeof(FlingBirdIntro),
            ["birdPath"] = typeof(BirdPath),
            ["lightningBlock"] = typeof(LightningBreakerBox),
            ["spikesUp"] = typeof(Spikes),
            ["spikesDown"] = typeof(Spikes),
            ["spikesLeft"] = typeof(Spikes),
            ["spikesRight"] = typeof(Spikes),
            ["triggerSpikesUp"] = typeof(TriggerSpikes),
            ["triggerSpikesDown"] = typeof(TriggerSpikes),
            ["triggerSpikesRight"] = typeof(TriggerSpikes),
            ["triggerSpikesLeft"] = typeof(TriggerSpikes),
            ["darkChaser"] = typeof(BadelineOldsite),
            ["rotateSpinner"] = typeof(BladeRotateSpinner),
            ["trackSpinner"] = typeof(TrackSpinner),
            ["spinner"] = typeof(CrystalStaticSpinner),
            ["sinkingPlatform"] = typeof(SinkingPlatform),
            ["friendlyGhost"] = typeof(AngryOshiro),
            ["seeker"] = typeof(Seeker),
            ["seekerStatue"] = typeof(SeekerStatue),
            ["slider"] = typeof(Slider),
            ["templeBigEyeball"] = typeof(TempleBigEyeball),
            ["crushBlock"] = typeof(CrushBlock),
            ["bigSpinner"] = typeof(Bumper),
            ["starJumpBlock"] = typeof(StarJumpBlock),
            ["floatySpaceBlock"] = typeof(FloatySpaceBlock),
            ["glassBlock"] = typeof(GlassBlock),
            ["goldenBlock"] = typeof(GoldenBlock),
            ["fireBall"] = typeof(FireBall),
            ["risingLava"] = typeof(RisingLava),
            ["sandwichLava"] = typeof(SandwichLava),
            ["killbox"] = typeof(Killbox),
            ["fakeHeart"] = typeof(FakeHeart),
            ["lightning"] = typeof(Lightning),
            ["finalBoss"] = typeof(FinalBoss),
            ["finalBossFallingBlock"] = typeof(FallingBlock),
            ["finalBossMovingBlock"] = typeof(FinalBossMovingBlock),
            ["fakeWall"] = typeof(FakeWall),
            ["fakeBlock"] = typeof(FakeWall),
            ["dashBlock"] = typeof(DashBlock),
            ["invisibleBarrier"] = typeof(InvisibleBarrier),
            ["exitBlock"] = typeof(ExitBlock),
            ["conditionBlock"] = typeof(ExitBlock),
            ["coverupWall"] = typeof(CoverupWall),
            ["crumbleWallOnRumble"] = typeof(CrumbleWallOnRumble),
            ["ridgeGate"] = typeof(RidgeGate),
            ["tentacles"] = typeof(Tentacles),
            ["starClimbController"] = typeof(StarClimbGraphicsController),
            ["playerSeeker"] = typeof(PlayerSeeker),
            ["chaserBarrier"] = typeof(ChaserBarrier),
            ["introCrusher"] = typeof(IntroCrusher),
            ["bridge"] = typeof(Bridge),
            ["bridgeFixed"] = typeof(BridgeFixed),
            ["bird"] = typeof(BirdNPC),
            ["introCar"] = typeof(IntroCar),
            ["memorial"] = typeof(Memorial),
            ["wire"] = typeof(Wire),
            ["cobweb"] = typeof(Cobweb),
            ["lamp"] = typeof(Lamp),
            ["hanginglamp"] = typeof(HangingLamp),
            ["hahaha"] = typeof(Hahaha),
            ["bonfire"] = typeof(Bonfire),
            ["payphone"] = typeof(Payphone),
            ["colorSwitch"] = typeof(ClutterSwitch),
            ["clutterDoor"] = typeof(ClutterDoor),
            ["dreammirror"] = typeof(DreamMirror),
            ["resortmirror"] = typeof(ResortMirror),
            ["towerviewer"] = typeof(Lookout),
            ["picoconsole"] = typeof(PicoConsole),
            ["wavedashmachine"] = typeof(WaveDashTutorialMachine),
            ["yellowBlocks"] = typeof(ClutterBlockBase),
            ["redBlocks"] = typeof(ClutterBlockBase),
            ["greenBlocks"] = typeof(ClutterBlockBase),
            ["oshirodoor"] = typeof(MrOshiroDoor),
            ["templeMirrorPortal"] = typeof(TempleMirrorPortal),
            ["reflectionHeartStatue"] = typeof(ReflectionHeartStatue),
            ["resortRoofEnding"] = typeof(ResortRoofEnding),
            ["gondola"] = typeof(Gondola),
            ["birdForsakenCityGem"] = typeof(ForsakenCitySatellite),
            ["whiteblock"] = typeof(WhiteBlock),
            ["plateau"] = typeof(Plateau),
            ["soundSource"] = typeof(SoundSourceEntity),
            ["templeMirror"] = typeof(TempleMirror),
            ["templeEye"] = typeof(TempleEye),
            ["clutterCabinet"] = typeof(ClutterCabinet),
            ["floatingDebris"] = typeof(FloatingDebris),
            ["foregroundDebris"] = typeof(ForegroundDebris),
            ["moonCreature"] = typeof(MoonCreature),
            ["lightbeam"] = typeof(LightBeam),
            ["door"] = typeof(Door),
            ["trapdoor"] = typeof(Trapdoor),
            ["resortLantern"] = typeof(ResortLantern),
            ["water"] = typeof(Water),
            ["waterfall"] = typeof(WaterFall),
            ["bigWaterfall"] = typeof(BigWaterfall),
            ["clothesline"] = typeof(Clothesline),
            ["cliffflag"] = typeof(CliffFlags),
            ["cliffside_flag"] = typeof(CliffsideWindFlag),
            ["flutterbird"] = typeof(FlutterBird),
            ["SoundTest3d"] = typeof(_3dSoundTest),
            ["SummitBackgroundManager"] = typeof(AscendManager),
            ["summitGemManager"] = typeof(SummitGem),
            ["heartGemDoor"] = typeof(HeartGemDoor),
            ["summitcheckpoint"] = typeof(SummitCheckpoint),
            ["summitcloud"] = typeof(SummitCloud),
            ["coreMessage"] = typeof(CoreMessage),
            ["playbackTutorial"] = typeof(PlayerPlayback),
            ["playbackBillboard"] = typeof(PlaybackBillboard),
            ["cutsceneNode"] = typeof(CutsceneNode),
            ["kevins_pc"] = typeof(KevinsPC),
            ["powerSourceNumber"] = typeof(PowerSourceNumber),
            ["npc"] = typeof(NPC),
            ["eventTrigger"] = typeof(EventTrigger),
            ["musicFadeTrigger"] = typeof(MusicFadeTrigger),
            ["musicTrigger"] = typeof(MusicTrigger),
            ["altMusicTrigger"] = typeof(AltMusicTrigger),
            ["cameraOffsetTrigger"] = typeof(CameraOffsetTrigger),
            ["lightFadeTrigger"] = typeof(LightFadeTrigger),
            ["bloomFadeTrigger"] = typeof(BloomFadeTrigger),
            ["cameraTargetTrigger"] = typeof(CameraTargetTrigger),
            ["cameraAdvanceTargetTrigger"] = typeof(CameraAdvanceTargetTrigger),
            ["respawnTargetTrigger"] = typeof(RespawnTargetTrigger),
            ["changeRespawnTrigger"] = typeof(ChangeRespawnTrigger),
            ["windTrigger"] = typeof(WindTrigger),
            ["windAttackTrigger"] = typeof(WindAttackTrigger),
            ["minitextboxTrigger"] = typeof(MiniTextboxTrigger),
            ["oshiroTrigger"] = typeof(OshiroTrigger),
            ["interactTrigger"] = typeof(InteractTrigger),
            ["checkpointBlockerTrigger"] = typeof(CheckpointBlockerTrigger),
            ["lookoutBlocker"] = typeof(LookoutBlocker),
            ["stopBoostTrigger"] = typeof(StopBoostTrigger),
            ["noRefillTrigger"] = typeof(NoRefillTrigger),
            ["ambienceParamTrigger"] = typeof(AmbienceParamTrigger),
            ["creditsTrigger"] = typeof(CreditsTrigger),
            ["goldenBerryCollectTrigger"] = typeof(GoldBerryCollectTrigger),
            ["moonGlitchBackgroundTrigger"] = typeof(MoonGlitchBackgroundTrigger),
            ["blackholeStrength"] = typeof(BlackholeStrengthTrigger),
            ["rumbleTrigger"] = typeof(RumbleTrigger),
            ["birdPathTrigger"] = typeof(BirdPathTrigger),
            ["spawnFacingTrigger"] = typeof(SpawnFacingTrigger),
            ["detachFollowersTrigger"] = typeof(DetachStrawberryTrigger),

            // FROM MODS:
            ["FrostHelper/CustomDreamBlock"] = typeof(CustomDreamBlockV2),
        };

        foreach (var type in FakeAssembly.GetFakeEntryAssembly().GetTypesSafe()) {
            checkType(type);
        }

        static void checkType(Type type) {
            foreach (CustomEntityAttribute customEntityAttribute in type.GetCustomAttributes<CustomEntityAttribute>()) {
                foreach (string idFull in customEntityAttribute.IDs) {
                    string id;
                    string[] split = idFull.Split('=');

                    if (split.Length == 1 || split.Length == 2) {
                        id = split[0];
                    } else {
                        // invalid
                        continue;
                    }

                    string IDTrim = id.Trim();
                    if (entityNameToType!.TryGetValue(IDTrim, out var duplicateType)) {
                        Logger.Log(LogLevel.Error, "FrostHelper.TypeHelper", $"Found duplicate entity ID {IDTrim} - {type.FullName} vs {duplicateType.FullName}");
                    }
                    entityNameToType[IDTrim] = type;
                }
            }
        }
    }
}
