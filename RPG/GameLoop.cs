using System.Windows.Threading;

namespace RPG
{
    public sealed class GameLoop
    {
        private readonly DispatcherTimer _timer;
        private readonly List<Action<double>> _callbacks = new();
        private readonly Dictionary<string, ScheduledAction> _scheduledActions = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();
        private DateTime _lastTick;

        public bool IsRunning => _timer.IsEnabled;

        public GameLoop(TimeSpan interval)
        {
            _timer = new DispatcherTimer
            {
                Interval = interval
            };
            _timer.Tick += OnTick;
        }

        private sealed class ScheduledAction
        {
            public string Name { get; }
            public Action Callback { get; }
            public double IntervalSeconds { get; }
            public double Accumulator { get; set; }
            public bool Repeat { get; }

            public ScheduledAction(string name, Action callback, double intervalSeconds, bool repeat)
            {
                Name = name;
                Callback = callback;
                IntervalSeconds = intervalSeconds;
                Repeat = repeat;
                Accumulator = 0d;
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            double dt = (_lastTick == default) ? _timer.Interval.TotalSeconds : (now - _lastTick).TotalSeconds;
            _lastTick = now;

            Action<double>[] snapshot;
            ScheduledAction[] scheduledSnapshot;
            lock (_lock)
            {
                snapshot = _callbacks.ToArray();
                scheduledSnapshot = _scheduledActions.Values.ToArray();
            }

            foreach (var cb in snapshot)
            {
                try
                {
                    cb(dt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GameLoop callback exception: {ex}");
                }
            }

            // Trigger scheduled actions without blocking the main loop
            if (scheduledSnapshot.Length > 0)
            {
                var accumulatorUpdates = new List<(string name, double accumulator)>();
                var toRemove = new List<string>();

                foreach (var scheduled in scheduledSnapshot)
                {
                    double accumulator = scheduled.Accumulator + dt;

                    while (accumulator >= scheduled.IntervalSeconds)
                    {
                        try
                        {
                            scheduled.Callback();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"GameLoop scheduled callback exception: {ex}");
                        }

                        if (scheduled.Repeat)
                        {
                            accumulator -= scheduled.IntervalSeconds;
                        }
                        else
                        {
                            toRemove.Add(scheduled.Name);
                            accumulator = 0d;
                            break;
                        }
                    }

                    accumulatorUpdates.Add((scheduled.Name, accumulator));
                }

                lock (_lock)
                {
                    foreach (var (name, accumulator) in accumulatorUpdates)
                    {
                        if (_scheduledActions.TryGetValue(name, out var action))
                        {
                            action.Accumulator = accumulator;
                        }
                    }

                    if (toRemove.Count > 0)
                    {
                        foreach (var name in toRemove)
                        {
                            _scheduledActions.Remove(name);
                        }
                    }
                }
            }
        }

        public void Start()
        {
            _lastTick = default;
            if (!_timer.IsEnabled)
                _timer.Start();
        }

        public void Stop()
        {
            if (_timer.IsEnabled)
                _timer.Stop();
        }

        public void Register(Action<double> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            lock (_lock)
            {
                if (!_callbacks.Contains(callback))
                    _callbacks.Add(callback);
            }
        }

        public void Unregister(Action<double> callback)
        {
            lock (_lock)
            {
                _callbacks.Remove(callback);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _callbacks.Clear();
            }
        }

        public bool Schedule(string name, Action callback, double intervalSeconds, bool repeat = true, bool replace = true)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (intervalSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Interval must be positive.");

            var scheduled = new ScheduledAction(name, callback, intervalSeconds, repeat);
            lock (_lock)
            {
                if (_scheduledActions.ContainsKey(name) && !replace)
                {
                    return false;
                }

                _scheduledActions[name] = scheduled;
            }

            return true;
        }

        public bool CancelScheduled(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            lock (_lock)
            {
                return _scheduledActions.Remove(name);
            }
        }

        public void ClearScheduled()
        {
            lock (_lock)
            {
                _scheduledActions.Clear();
            }
        }
    }
}
