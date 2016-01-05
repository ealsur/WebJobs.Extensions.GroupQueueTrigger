using System;

namespace Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers
{
    internal static class RandomExtensions
    {
        public static double Next(this Random random, double minValue, double maxValue)
        {
            if (random == null)
                throw new ArgumentNullException("random");
            return (maxValue - minValue) * random.NextDouble() + minValue;
        }
    }
}
