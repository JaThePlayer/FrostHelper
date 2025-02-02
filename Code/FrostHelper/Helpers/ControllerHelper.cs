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
        newController.Add(new OnAwakeCallbackComponent(() => _justAddedControllers.Remove(newController)));
        _justAddedControllers.Add(newController);
        scene.Add(newController);

        return newController;
    }

    internal static T? FindFirst(Scene scene, Func<T, bool> filter) {
        foreach (T e in scene.Tracker.SafeGetEntities<T>()) {
            if (filter(e))
                return e;
        }
        foreach (T e in _justAddedControllers) {
            if (filter(e))
                return e;
        }

        return null;
    }
    
    internal static T? FindFirst<TState>(Scene scene, TState state, Func<T, TState, bool> filter) {
        foreach (T e in scene.Tracker.SafeGetEntities<T>()) {
            if (filter(e, state))
                return e;
        }
        foreach (T e in _justAddedControllers) {
            if (filter(e, state))
                return e;
        }

        return null;
    }
}

public class OnAwakeCallbackComponent(Action onAwake) : Component(false, false) {
    public override void EntityAwake() {
        base.EntityAwake();
        onAwake();
    }
}
