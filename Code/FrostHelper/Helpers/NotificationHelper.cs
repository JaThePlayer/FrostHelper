namespace FrostHelper.Helpers;
public static class NotificationHelper {
    public static void Notify(string notif, LogLevel logLevel = LogLevel.Error) {
        Notify(new Notification(logLevel, notif));
    }

    public static void Notify(Notification notification) {
        var controller = ControllerHelper<NotificationController>.AddToSceneIfNeeded(FrostModule.GetCurrentLevel());

        controller.Push(notification);
    }


    public class NotificationController : Entity {
        public static List<Notification> Notifications = new();
        
        public void Push(Notification notification) {
            notification.Time = DateTime.Now;
            notification.Alpha += 0.25f * Notifications.Count;
            Notifications.Add(notification);
        }

        public NotificationController() : base() {
            Tag = Tags.HUD;
        }

        public override void Render() {
            base.Render();

            const float notifHeight = 100;
            const float padding = 15;

            var y = 1080 - notifHeight - padding;
            

            for (int i = Notifications.Count - 1; i >= 0; i--) {
                var notif = Notifications[i];
                notif.Alpha -= Engine.DeltaTime / 8f;
                Notifications[i] = notif;

                var realAlpha = Ease.ExpoIn(Math.Min(1f, notif.Alpha * 2.35f));

                var messageScale = Vector2.One / 3f * 2f;
                var notifWidth = (ActiveFont.Measure(notif.Message) * messageScale).X;
                var pos = new Vector2(1920 - notifWidth - padding, y);

                Draw.Rect(pos.X, pos.Y, notifWidth, notifHeight, Color.Gray * 0.75f * realAlpha);
                Draw.HollowRect(pos.X, pos.Y, notifWidth, notifHeight, Color.Gray * realAlpha);
                ActiveFont.Draw(notif.Message, pos, Vector2.Zero, messageScale, Color.White * realAlpha);
                ActiveFont.Draw(notif.Time.ToString(), pos + new Vector2(0f, notifHeight - 20f), Vector2.Zero, Vector2.One / 4f, Color.White * realAlpha);

                y -= notifHeight + padding;
            }

            Notifications.RemoveAll(n => n.Alpha <= 0f);
        }
    }

    public record struct Notification(LogLevel Level, string Message) {
        internal DateTime Time;
        internal float Alpha = 1f;
    }
}
