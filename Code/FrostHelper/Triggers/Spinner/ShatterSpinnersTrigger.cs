namespace FrostHelper.Triggers.Spinner;

[CustomEntity("FrostHelper/ShatterSpinnersTrigger")]
internal sealed class ShatterSpinnersTrigger(EntityData data, Vector2 offset) : SpinnerTrigger(data, offset) {
    protected override void ChangeSpinner(Session session, CustomSpinner spinner, bool fromExternalSource) {
        spinner.Destroy();
    }
}