using MonoMod.ModInterop;

namespace FrostHelper.ModIntegration;

[ModImportName("SpeedrunTool.SaveLoad")]
public class SpeedrunToolIntegration {
    public static bool LoadIfNeeded()
    {
        if (Loaded)
            return true;

        typeof(SpeedrunToolIntegration).ModInterop();

        Loaded = true;

        return true;
    }

    private static bool Loaded { get; set; }
    
    //public static void AddReturnSameObjectProcessor(Func<Type, bool> predicate) 
    public static Action<Func<Type, bool>>? AddReturnSameObjectProcessor;
    
    //AddCustomDeepCloneProcessor(Func<object, object> processor)
    public static Action<Func<object, object>>? AddCustomDeepCloneProcessor;
}

/// <summary>
/// Types marked with this interface will be returned by reference when loading a savestate, instead of being cloned.
/// </summary>
internal interface ISavestatePersisted;