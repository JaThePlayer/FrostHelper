using FrostHelper.ModIntegration;
using FrostHelper.Triggers.Activator;
using MonoMod.ModInterop;
using System.Threading;

namespace FrostHelper.API;

/// <summary>
/// Exposes various events that can be subscribed to by mods.
/// All apis here accept a mod name, which should be the everest.yaml name of your mod.
/// All apis here return an IDisposable, which unsubscribes from the event when disposed. Make sure to do that on mod unload!
/// </summary>
[ModExportName("FrostHelper.Events")]
public static class EventsApi {
    /// <summary>
    /// Fires whenever any Activator activates any trigger.
    /// If an Activator activates several triggers at once, this event will be fired several times.
    /// </summary>
    /// <param name="modName">everest.yaml name of the mod subscribing to this event.</param>
    /// <param name="handler">Event handler, of signature (Trigger activator, Trigger target, Player?)</param>
    /// <returns>IDisposable, which unsubscribes from the event when disposed. Make sure to do that on mod unload!</returns>
    public static IDisposable OnActivatorActivate(string modName, Action<Trigger, Trigger, Player?> handler)
        => BaseActivator.OnActivatorActivateEvent.Subscribe(modName, handler);
}

internal sealed class ModEvent<T>(string eventName) where T : Delegate {
    private readonly object _lock = new();
    private readonly List<(string ModName, T Delegate)> _listeners = [];
    private T? _delegate;

    private string FormatDelegateForPrinting(T dele) {
        var method = dele.Method;
        return $"{method.DeclaringType?.FullName ?? ""}.{method.Name}";
    }
    
    public ModEventDisposer<T> Subscribe(string modName, T dele) {
        ArgumentNullException.ThrowIfNull(modName);
        
        if (!IntegrationUtils.TryGetModule(modName, out var mod)) {
            throw new Exception($"""
                                 Attempt to subscribe to event '{eventName}' from a non-existing mod '{modName}'.
                                 Please make sure you are using the correct everest.yaml mod name.
                                 Delegate: '{FormatDelegateForPrinting(dele)}'.
                                 """);
        }

        Logger.Info("FrostHelper.ModEvent", $"New listener for event '{eventName}' from mod '{modName}': {FormatDelegateForPrinting(dele)}");

        lock (_lock) {
            _listeners.Add((modName, dele));
            _delegate = (T)Delegate.Combine(_delegate, dele);
        }
        
        return new(this, dele);
    }

    public T? Get() {
        lock (_lock)
            return _delegate;
    }

    internal void Unsubscribe(T dele) {
        lock (_lock) {
            _listeners.RemoveAll(x => {
                if (x.Delegate != dele)
                    return false;
                
                Logger.Info("FrostHelper.ModEvent", $"Removed listener for event '{eventName}' from mod '{x.ModName}': {FormatDelegateForPrinting(dele)}");
                return true;
            });

            _delegate = (T?) Delegate.Remove(_delegate, dele);
        }
    }
}

internal class ModEventDisposer<T>(ModEvent<T> modEvent, T dele) : IDisposable where T : Delegate {
    private int _disposed;
    
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        
        modEvent.Unsubscribe(dele);
        GC.SuppressFinalize(this);
    }
}