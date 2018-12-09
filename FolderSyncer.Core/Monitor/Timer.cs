using System;
using System.Timers;
using Microsoft.Extensions.Configuration;

namespace FolderSyncer.Core.Monitor
{
    /// <summary>
    /// Testable wrapper around System.Timers.Timer;
    /// </summary>
    public class Timer : ITimer
    {
        private readonly System.Timers.Timer _timer;

        public Timer(IConfiguration configuration)
        {
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }

            if (!int.TryParse(configuration["snapshot_interval"], out var timeout))
            {
                timeout = 5000;
            }

            _timer = new System.Timers.Timer
            {
                Interval = timeout,
                AutoReset = true,
            };
        }

        public double Interval => _timer.Interval;

        public event ElapsedEventHandler Elapsed
        {
            add => _timer.Elapsed += value;
            remove => _timer.Elapsed -= value;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}