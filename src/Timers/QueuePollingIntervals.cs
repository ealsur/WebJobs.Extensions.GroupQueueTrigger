using System;

namespace Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers
{
    internal static class QueuePollingIntervals
    {
        public static readonly TimeSpan Minimum = TimeSpan.FromSeconds(2.0);
        public static readonly TimeSpan DefaultMaximum = TimeSpan.FromMinutes(1.0);
    }
}
