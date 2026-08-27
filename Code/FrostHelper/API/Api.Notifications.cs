using FrostHelper.Helpers;

namespace FrostHelper.API;

// [ModExportName("FrostHelper")] - defined in API.cs
public static partial class API {
    /// <summary>
    /// Creates an in-game notification that also gets logged to console.
    /// Added in 1.80.0
    /// </summary>
    public static void Notify(LogLevel level, string message) {
        NotificationHelper.Notify(message, level);
    }

    /// <summary>
    /// Registers a notification sink, which calls onNotification whenever a notification is created.
    /// Returning false from onNotification will prevent it from being displayed in-game.
    /// Dispose the returned <see cref="IDisposable"/> to remove the sink.
    /// 
    /// Added in 1.80.0.
    /// </summary>
    public static IDisposable RegisterNotificationSink(Func<LogLevel, string, bool> onNotification) {
        var sink = new ApiNotificationSink(NotificationHelper.NotificationSink, onNotification);
        NotificationHelper.NotificationSink = sink;
        return sink;
    }
}

file class ApiNotificationSink(INotificationSink previous, Func<LogLevel, string, bool> onNotification) : INotificationSink, IDisposable {
    public void Push(NotificationHelper.Notification notification) {
        if (onNotification(notification.Level, notification.Message)) {
            previous.Push(notification);
        }
    }

    public void Dispose() {
        NotificationHelper.NotificationSink = previous;
    }
}
