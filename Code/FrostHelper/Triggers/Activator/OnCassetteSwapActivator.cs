using FrostHelper.Components;

// TODO: swap to everest's, might be missing OnSilentUpdate?
using CassetteListener = FrostHelper.Components.CassetteListener;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnCassetteSwapActivator")]
internal sealed class OnCassetteSwapActivator : BaseActivator {
    public OnCassetteSwapActivator(EntityData data, Vector2 offset) : base(data, offset) {
        var index = data.Int("targetIndex", -1);
        CassetteListener listener = new(index);

        Add(listener);

        listener.OnActivated = () => {
            var player = Scene?.Tracker.GetEntity<Player>();
            if (player is { } || ActivateAfterDeath)
                ActivateAll(player!);
        };

        listener.OnSilentUpdate = (correctIndex) => {
            var player = Scene?.Tracker.GetEntity<Player>();
            if (correctIndex && (player is { } || ActivateAfterDeath))
                ActivateAll(player!);
        };

        Active = Visible = false;
    }
}
