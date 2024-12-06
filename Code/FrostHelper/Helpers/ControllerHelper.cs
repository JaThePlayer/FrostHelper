namespace FrostHelper;

public static class ControllerHelper<T> where T : Entity {
    private static T? _justAddedController;
    private static List<T> _justAddedControllers = new();
    
    public static T AddToSceneIfNeeded(Scene scene) {
        var tracked = scene.Tracker.SafeGetEntity<T>();
        if (tracked is null && _justAddedController is null) {
            scene.Add(_justAddedController = Activator.CreateInstance<T>());

            _justAddedController.Add(new OnAwakeCallbackComponent(static () => _justAddedController = null));

            return _justAddedController;
        }

        return tracked ?? _justAddedController!;
    }
    
    internal static T AddToSceneIfNeeded(Scene scene, Func<T, bool> filter, Func<T> factory) {
        foreach (T e in scene.Tracker.SafeGetEntities<T>()) {
            if (filter(e))
                return e;
        }
        foreach (T e in _justAddedControllers) {
            if (filter(e))
                return e;
        }

        var newController = factory();
        newController.Add(new OnAwakeCallbackComponent(static () => _justAddedControllers.Clear()));
        _justAddedControllers.Add(newController);
        scene.Add(newController);

        return newController;
    }
}

public class OnAwakeCallbackComponent(Action onAwake) : Component(false, false) {
    public override void EntityAwake() {
        base.EntityAwake();
        onAwake();
    }
}
