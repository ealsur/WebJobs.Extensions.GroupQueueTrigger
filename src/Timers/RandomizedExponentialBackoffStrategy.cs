using System;

namespace Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers
{
    internal class RandomizedExponentialBackoffStrategy
    {
        public const double RandomizationFactor = 0.2;
        private readonly TimeSpan _minimumInterval;
        private readonly TimeSpan _maximumInterval;
        private readonly TimeSpan _deltaBackoff;
        private TimeSpan _currentInterval;
        private uint _backoffExponent;
        private Random _random;

        public RandomizedExponentialBackoffStrategy(TimeSpan minimumInterval, TimeSpan maximumInterval)
          : this(minimumInterval, maximumInterval, minimumInterval)
        {
        }

        public RandomizedExponentialBackoffStrategy(TimeSpan minimumInterval, TimeSpan maximumInterval, TimeSpan deltaBackoff)
        {
            if (minimumInterval.Ticks < 0L)
                throw new ArgumentOutOfRangeException("minimumInterval", "The TimeSpan must not be negative.");
            if (maximumInterval.Ticks < 0L)
                throw new ArgumentOutOfRangeException("maximumInterval", "The TimeSpan must not be negative.");
            if (minimumInterval.Ticks > maximumInterval.Ticks)
                throw new ArgumentException("The minimumInterval must not be greater than the maximumInterval.", "minimumInterval");
            this._minimumInterval = minimumInterval;
            this._maximumInterval = maximumInterval;
            this._deltaBackoff = deltaBackoff;
        }

        public TimeSpan GetNextDelay(bool executionSucceeded)
        {
            if (executionSucceeded)
            {
                this._currentInterval = this._minimumInterval;
                this._backoffExponent = 1U;
            }
            else if (this._currentInterval != this._maximumInterval)
            {
                TimeSpan timeSpan = this._minimumInterval;
                if (this._backoffExponent > 0U)
                {
                    if (this._random == null)
                        this._random = new Random();
                    double num = RandomExtensions.Next(this._random, 0.8, 1.2) * Math.Pow(2.0, (double)(this._backoffExponent - 1U)) * this._deltaBackoff.TotalMilliseconds;
                    timeSpan += TimeSpan.FromMilliseconds(num);
                }
                if (timeSpan < this._maximumInterval)
                {
                    this._currentInterval = timeSpan;
                    ++this._backoffExponent;
                }
                else
                    this._currentInterval = this._maximumInterval;
            }
            return this._currentInterval;
        }
    }
}
