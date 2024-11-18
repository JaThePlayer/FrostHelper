using System.Globalization;
using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;
public static class NotificationHelper {
    // note: used internally by lua badeline boss
    public static void NotifyInfo(string notif) {
        Notify(new Notification(LogLevel.Info, notif));
    }
    
    public static void Notify(string notif, LogLevel logLevel) {
        Notify(new Notification(logLevel, notif));
    }
    
    public static void Notify(string notif, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string source = "", [CallerLineNumber] int lineNumber = 0) {
        Notify(new Notification(logLevel, notif) {
            Source = $" | {source[source.LastIndexOf("FrostHelper", StringComparison.Ordinal)..]}:{lineNumber}"
        });
    }

    public static void Notify(Notification notification) {
        var controller = ControllerHelper<NotificationController>.AddToSceneIfNeeded(FrostModule.TryGetCurrentLevel() ?? Engine.Scene);

        controller.Push(notification);
    }


    public class NotificationController : Entity {
        public static List<Notification> Notifications = new();
        
        public void Push(Notification notification) {
            notification.Time = DateTime.Now;
            notification.Alpha += 0.25f * Notifications.Count;
            Notifications.Add(notification);
            Logger.Log(notification.Level, "FrostHelper/Notifications", notification.Message);
        }

        public NotificationController() : base() {
            Tag = Tags.HUD;
        }

        public override void Render() {
            base.Render();

            const float minMotifHeight = 100;
            const float padding = 15;

            var y = 1080 - padding;
            

            for (int i = Notifications.Count - 1; i >= 0; i--) {
                var notif = Notifications[i];
                notif.Alpha -= Engine.DeltaTime / 8f;
                Notifications[i] = notif;

                var realAlpha = Ease.ExpoInOut(Math.Min(1f, notif.Alpha * 2.35f));

                var messageScale = Vector2.One / 3f * 2f;
                var notifSize = (ActiveFont.Measure(notif.Message) * messageScale);
                var notifWidth = notifSize.X + padding * 2;
                var notifHeight = float.Max(notifSize.Y + 30, minMotifHeight);
                var pos = new Vector2(1920 - notifWidth - padding, y - notifHeight);

                var color = ColorHelper.GetColor("262626");
                Draw.Rect(pos.X, pos.Y, notifWidth, notifHeight, color * 0.75f * realAlpha);
                Draw.HollowRect(pos.X, pos.Y, notifWidth, notifHeight, color * realAlpha);
                ActiveFont.Draw(notif.Message, pos + new Vector2(padding, 0), Vector2.Zero, messageScale, Color.White * realAlpha);

                var timePos = pos + new Vector2(padding, notifHeight - 20f);
                var timeStr = notif.Time.ToString(CultureInfo.CurrentCulture);
                var timeScale = Vector2.One / 4f;
                ActiveFont.Draw(timeStr, timePos, Vector2.Zero, timeScale, Color.White * realAlpha);
                if (notif.Source is { } src) {
                    var srcPos = timePos + new Vector2(ActiveFont.Measure(timeStr).X * timeScale.X, 0f);
                    ActiveFont.Draw(src, srcPos, Vector2.Zero, timeScale, Color.White * realAlpha);
                }

                y -= notifHeight + padding;
            }

            Notifications.RemoveAll(n => n.Alpha <= 0f);
        }
    }

    public record struct Notification(LogLevel Level, string Message) {
        internal DateTime Time;
        internal float Alpha = 1f;
        internal string? Source;
    }
}
