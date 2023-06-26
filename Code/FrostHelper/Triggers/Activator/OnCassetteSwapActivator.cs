using FrostHelper.Components;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnCassetteSwapActivator")]
internal class OnCassetteSwapActivator : BaseActivator {
    private CassetteListener Listener;

    public OnCassetteSwapActivator(EntityData data, Vector2 offset) : base(data, offset) {
        var index = data.Int("targetIndex", -1);

        Add(Listener = new CassetteListener(index));

        Listener.OnActivated = () => {
            if (Scene?.Tracker.GetEntity<Player>() is { } player)
                ActivateAll(player);
        };

        Active = Visible = false;
    }
}
