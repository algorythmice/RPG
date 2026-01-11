using System.Windows.Threading;

namespace RPG
{
    /// <summary>
    /// Simple game loop service that runs on the WPF Dispatcher thread.
    /// You can Register callbacks that receive deltaTime (seconds) on each tick.
    /// </summary>
    public sealed class GameLoop
    {
        private readonly DispatcherTimer _timer;
        private readonly List<Action<double>> _callbacks = new();
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

        private void OnTick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            double dt = (_lastTick == default) ? _timer.Interval.TotalSeconds : (now - _lastTick).TotalSeconds;
            _lastTick = now;

            Action<double>[] snapshot;
            lock (_lock)
            {
                snapshot = _callbacks.ToArray();
            }

            foreach (var cb in snapshot)
            {
                try
                {
                    cb(dt);
                }
                catch (Exception ex)
                {
                    // Swallow exceptions to not stop the loop; in a real project log this.
                    Console.WriteLine($"GameLoop callback exception: {ex}");
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
    }
}
