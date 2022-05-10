namespace FrostHelper;

public static class ControllerHelper<T> where T : Entity, new() {
    private static T? _justAddedController;
    public static T AddToSceneIfNeeded(Scene scene) {
        var tracked = scene.Tracker.GetEntity<T>();
        if (tracked is null && _justAddedController is null) {
            scene.Add(_justAddedController = new T());

            _justAddedController.Add(new OnAwakeCallbackComponent(() => _justAddedController = null));

            return _justAddedController;
        }

        return tracked ?? _justAddedController!;
    }

     
}

public class OnAwakeCallbackComponent : Component {
    Action OnAwake;

    public OnAwakeCallbackComponent(Action onAwake) : base(false, false) {
        OnAwake = onAwake;
    }

    public override void EntityAwake() {
        base.EntityAwake();
        OnAwake();
    }
}
