using FrostHelper.Helpers;
using System.Runtime.CompilerServices;

namespace FrostHelper.Tests.Utils;

public sealed class FrostHelperSetupFixture : IDisposable
{
    public FrostHelperSetupFixture() {
        var frostHelperModule = new FrostModule { 
            Metadata = new EverestModuleMetadata
            {
                Name = "FrostHelper",
                VersionString = "1.0.0.0-unit-test"
            }
        };
        frostHelperModule.Metadata.RegisterMod();
        
        // crashes due to SaveData IO, which we do not want
        //frostHelperModule.Register();
        
        // Make sure FakeAssembly knows about FrostHelper, as we cannot Register()
        Everest._Modules.Add(frostHelperModule);
        
        NotificationHelper.NotificationSink = new DefaultTestNotificationSink();
    }

    public void Dispose()
    {
    }


    private class DefaultTestNotificationSink : INotificationSink {
        public void Push(NotificationHelper.Notification notification) {
            Assert.Fail($"An unhandled notification happened:\n{notification.Message}");
        }
    }
}

public sealed class CelesteSetupFixture : IDisposable
{
    public CelesteSetupFixture() {
        var celesteModule = new NullModule(new EverestModuleMetadata
        {
            Name = "Celeste",
            VersionString = "1.4.0.0-unit-test"
        });
        celesteModule.Register();
        celesteModule.Metadata.RegisterMod();
        
        Tracker.Initialize();

        SaveData.Instance = new SaveData {
            Areas = [ new AreaStats {
                Modes = [
                    new AreaModeStats(),
                    new AreaModeStats(),
                    new AreaModeStats()
                ]
            }],
            LevelSets = [],
        };

        Engine.Instance = (Celeste.Celeste)RuntimeHelpers.GetUninitializedObject(typeof(Celeste.Celeste));
    }

    public void Dispose()
    {
    }

}

[CollectionDefinition("FrostHelper")]
public class FrostHelperCollection : ICollectionFixture<FrostHelperSetupFixture>, ICollectionFixture<CelesteSetupFixture>
{
}
