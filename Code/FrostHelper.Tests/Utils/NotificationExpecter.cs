using FrostHelper.Helpers;

namespace FrostHelper.Tests;

public sealed class NotificationExpecter : IDisposable {
    private readonly Action<NotificationHelper.Notification>? _onNotification;
    private readonly INotificationSink _orig;
    private int _remaining;

    public NotificationExpecter(int expectedAmt, Action<NotificationHelper.Notification>? onNotification = null) {
        _onNotification = onNotification;
        _orig = NotificationHelper.NotificationSink;
        NotificationHelper.NotificationSink = new Sink(this);
        _remaining = expectedAmt;
    }
    
    public void Dispose() {
        NotificationHelper.NotificationSink = _orig;
        Assert.Equal(0, _remaining);
    }

    private class Sink(NotificationExpecter expecter) : INotificationSink {
        public void Push(NotificationHelper.Notification notification) {
            expecter._remaining--;
            expecter._onNotification?.Invoke(notification);
        }
    }
}