﻿using System;
using System.Collections;
using System.Linq;
using DesertImage.External;
using DesertImage.Pools;
using DesertImage.Timers;
using External;

namespace DesertImage.Managers
{
    public interface IManagerTimers : IAwake
    {
        ITimer PlayAction(object sender, Action action, float delay = 1f, bool ignoreTimescale = false,
            Action<ITimer> timerCallback = null);

        ITimer PlayAction(object sender, TimerEntry[] entries, float delay = 1f, bool ignoreTimescale = false,
            Action<ITimer> timerCallback = null);


        ITimer PlayAction(object sender, Action<ITimer> action, float delay = 1f, bool ignoreTimescale = false,
            Action<ITimer> timerCallback = null);

        void CancelAll();
        void CancelAll(object sender);
        void Cancel(object sender, ushort id);
    }

    public class ManagerTimers : IManagerTimers
    {
        private readonly Pool<Timer> _pool = new Pool<Timer>(() => new Timer());
        private readonly Pool<Timer> _sequencePool;

        private readonly CustomDictionary<int, CustomList<ITimer>> _timers =
            new CustomDictionary<int, CustomList<ITimer>>();

        public ManagerTimers()
        {
            _sequencePool = new Pool<Timer>(() => new TimerSequence(_pool));
        }

        public void OnAwake()
        {
            _pool.Register(35);
        }

        public ITimer PlayAction(object sender, Action action, float delay = 1f, bool ignoreTimescale = false,
            Action<ITimer> timerCallback = null)
        {
            var timer = _pool.GetInstance();

            timerCallback?.Invoke(timer);

            if (sender != null)
            {
                timer.OnFinish += OnTimerFinish;

                if (_timers.TryGetValue(sender.GetHashCode(), out var timers))
                {
                    timers.Add(timer);
                }
                else
                {
                    _timers.Add(sender.GetHashCode(), new CustomList<ITimer> { timer });
                }
            }

            timer.Play(action, delay, ignoreTimescale);

            return timer;
        }

        public ITimer PlayAction(object sender, TimerEntry[] entries, float delay = 1, bool ignoreTimescale = false,
            Action<ITimer> timerCallback = null)
        {
            var timerSequence = _sequencePool.GetInstance() as ITimerSequence;

            timerCallback?.Invoke(timerSequence);

            if (sender != null)
            {
                timerSequence.OnFinish += OnTimerFinish;

                if (_timers.TryGetValue(sender.GetHashCode(), out var timers))
                {
                    timers.Add(timerSequence);
                }
                else
                {
                    _timers.Add(sender.GetHashCode(), new CustomList<ITimer> { timerSequence });
                }
            }

            timerSequence.Play(entries, delay, ignoreTimescale);

            return timerSequence;
        }

        public ITimer PlayAction(object sender, Action<ITimer> action, float delay = 1f, bool ignoreTimescale = false,
            Action<ITimer> timerCallback = null)
        {
            var timer = _pool.GetInstance();

            timerCallback?.Invoke(timer);

            timer.Play(action, delay, ignoreTimescale);

            return timer;
        }

        public void CancelAll()
        {
            for (var i = 0; i < _timers.Count; i++)
            {
                var (_, timers) = _timers[i];

                foreach (var timer in timers)
                {
                    timer.OnFinish -= OnTimerFinish;

                    timer.ReturnToPool();
                }
            }

            _timers.Clear();
        }

        public void CancelAll(object sender)
        {
            var hashCode = sender.GetHashCode();

            if (!_timers.TryGetValue(sender.GetHashCode(), out var timers)) return;

            for (var i = 0; i < timers.Count; i++)
            {
                var timer = timers[i];

                timer.OnFinish -= OnTimerFinish;

                timer.ReturnToPool();
            }

            _timers.Remove(hashCode);
        }

        public void Cancel(object sender, ushort id)
        {
            if (!_timers.TryGetValue(sender.GetHashCode(), out var timers)) return;

            var timer = timers.FirstOrDefault(x => x.Id == id);

            if (timer == null) return;

            timer.OnFinish -= OnTimerFinish;

            timer.ReturnToPool();
            timers.Remove(timer);
        }

        public void ReturnInstance(Timer instance)
        {
            _pool.ReturnInstance(instance);
        }

        private void OnTimerFinish(ITimer timer)
        {
        }
    }
}