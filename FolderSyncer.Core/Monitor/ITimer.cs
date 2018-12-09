using System;
using System.Timers;

namespace FolderSyncer.Core.Monitor
{
    public interface ITimer
    {
        double Interval { get; }
        event ElapsedEventHandler Elapsed;
        void Start();
        void Stop();
    }
}