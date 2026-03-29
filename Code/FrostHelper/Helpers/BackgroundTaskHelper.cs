using System.Threading;
using System.Threading.Tasks;

namespace FrostHelper.Helpers;

public static class BackgroundTaskHelper {
    private sealed class TimerInfo {
        public PeriodicTimer Timer;
        public Action OnElapsed;
        public CancellationTokenSource CancellationToken;
    }

    private static readonly Dictionary<TimeSpan, TimerInfo> Timers = new();

    /// <summary>
    /// Registers a background task which will call <paramref name="action"/> every <paramref name="interval"/>.
    /// </summary>
    public static BackgroundTaskInfo RegisterOnInterval(TimeSpan interval, Action action) {
        var info = new BackgroundTaskInfo() {
            Interval = interval,
            OnElapsed = action
        };

        lock (Timers) {
            if (Timers.TryGetValue(interval, out var existing)) {
                lock (existing.OnElapsed) {
                    existing.OnElapsed += action;
                }
                return info;
            }
        }
        var timer = new PeriodicTimer(interval);
        CancellationTokenSource tokenSource = new();

        lock (Timers)
            Timers.Add(interval, new() {
                Timer = timer,
                CancellationToken = tokenSource,
                OnElapsed = action
            });

        Task.Run(async () => {
            while (await timer.WaitForNextTickAsync(tokenSource.Token)) {
                Action onElapsed;
                lock (Timers) {
                    onElapsed = Timers[interval].OnElapsed;
                }

                onElapsed();
            }
        }, tokenSource.Token);


        return info;
    }

    public static void Deregister(TimeSpan interval, Action action) {
        lock (Timers) {
            if (!Timers.TryGetValue(interval, out var existing)) {
                return;
            }

            var newOnElapsed = existing.OnElapsed - action;
            if (newOnElapsed is not null) {
                existing.OnElapsed = newOnElapsed;
                return;
            }

            // there are no more actions to be executed by the timer, let's cancel the task
            existing.CancellationToken.Cancel();
            Timers.Remove(interval);
        }
    }
}

public class BackgroundTaskInfo {
    public TimeSpan Interval { get; init; }
    public Action OnElapsed { get; init; }

    public BackgroundTaskInfo() { }

    public void Deregister() {
        BackgroundTaskHelper.Deregister(Interval, OnElapsed);
    }
}