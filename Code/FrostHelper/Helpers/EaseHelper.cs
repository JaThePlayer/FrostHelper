namespace FrostHelper;

public static class EaseHelper {
    /// <returns>An easer of the name specified by <paramref name="name"/>, defaulting to <paramref name="defaultValue"/> or <see cref="Ease.Linear"/> if <paramref name="defaultValue"/> is null </returns>
    public static Ease.Easer GetEase(string name, Ease.Easer? defaultValue = null) {
        foreach (var propertyInfo in easeProps) {
            if (name.Equals(propertyInfo.Name, StringComparison.OrdinalIgnoreCase)) {
                return (Ease.Easer) propertyInfo.GetValue(default(Ease));
            }
        }
        return defaultValue ?? Ease.Linear;
    }

    /// <summary>Calls <see cref="GetEaser(string, Ease.Easer)"/> on the <paramref name="data"/>'s attribute <paramref name="key"/>, using <paramref name="defaultValue"/> if it's not a valid easing type/is empty</summary>
    public static Ease.Easer Easing(this EntityData data, string key, Ease.Easer defaultValue) {
        return GetEase(data.Attr(key), defaultValue);
    }

    // TODO: move this somewhere else lol
    public static Tween.TweenMode TweenMode(this EntityData data, string key, Tween.TweenMode defaultValue) {
        var attr = data.Attr(key);

        return attr switch {
            nameof(Tween.TweenMode.Persist) => Tween.TweenMode.Persist,
            nameof(Tween.TweenMode.Oneshot) => Tween.TweenMode.Oneshot,
            nameof(Tween.TweenMode.Looping) => Tween.TweenMode.Looping,
            nameof(Tween.TweenMode.YoyoOneshot) => Tween.TweenMode.YoyoOneshot,
            nameof(Tween.TweenMode.YoyoLooping) => Tween.TweenMode.YoyoLooping,
            _ => defaultValue
        };
    }

    private static readonly FieldInfo[] easeProps = typeof(Ease).GetFields(BindingFlags.Static | BindingFlags.Public);
}
