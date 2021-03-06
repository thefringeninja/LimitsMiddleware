﻿namespace LimitsMiddleware
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal class RateLimiter
    {
        private const long Infinite = 0;
        private readonly long _maximumBytesPerSecond;
        private long _byteCount;
        private long _start;
        private readonly InterlockedBoolean _resetting;

        private long CurrentMilliseconds
        {
            get { return Environment.TickCount; }
        }

        public RateLimiter(long maximumBytesPerSecond = Infinite)
        {
            if (maximumBytesPerSecond < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "maximumBytesPerSecond",
                    maximumBytesPerSecond,
                    "The maximum number of bytes per second can't be negative.");
            }

            _maximumBytesPerSecond = maximumBytesPerSecond;
            _start = CurrentMilliseconds;
            _byteCount = 0;
            _resetting = new InterlockedBoolean();
        }

        public async Task Throttle(int bufferSizeInBytes)
        {
            // Make sure the buffer isn't empty.
            if (_maximumBytesPerSecond <= 0 || bufferSizeInBytes <= 0)
            {
                return;
            }
            Interlocked.Add(ref _byteCount, bufferSizeInBytes);
            long elapsedMilliseconds = CurrentMilliseconds - _start;

            if (elapsedMilliseconds >= 0)
            {
                // Calculate the current bps.
                long bps = elapsedMilliseconds == 0 ? long.MaxValue : _byteCount / (elapsedMilliseconds * 1000L);

                // If the bps are more then the maximum bps, try to throttle.
                if (bps > _maximumBytesPerSecond)
                {
                    // Calculate the time to sleep.
                    long wakeElapsed = _byteCount / _maximumBytesPerSecond;
                    var toSleep = (wakeElapsed*1000L) - elapsedMilliseconds;

                    if (toSleep > 1)
                    {
                        try
                        {
                            // The time to sleep is more then a millisecond, so sleep.
                            await Task.Delay((int)toSleep);
                        }
                        catch (ThreadAbortException)
                        {
                            // Eatup ThreadAbortException.
                        }

                        // A sleep has been done, reset.
                        Reset();
                    }
                }
            }
        }

        private void Reset()
        {
            if (!_resetting.EnsureCalledOnce())
            {
                return;
            }
            
            long difference = CurrentMilliseconds - _start;

            // Only reset counters when a known history is available of more then 1 second.
            if (difference > 1000)
            {
                Interlocked.Exchange(ref _byteCount, 0);
            }

            _resetting.Set(false);
        }
    }
}