using System;


namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Defines the [GroupQueueTrigger] attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class GroupQueueTriggerAttribute : Attribute
    {
        private const int DEFAULT_SIZE = 32;
        /// <summary>
        /// Triggers an event with a group of messages
        /// </summary>
        /// <param name="queueName">Name of the Queue</param>
        public GroupQueueTriggerAttribute(string queueName)
        {
            GroupSize = DEFAULT_SIZE;
            QueueName = queueName;
            MinQueuePollingInterval = 0;
            MaxQueuePollingInterval = 0;
        }
        /// <summary>
        /// Triggers an event with a group of messages
        /// </summary>
        /// <param name="queueName">Name of the Queue</param>
        /// <param name="size">Maximun group size</param>
        public GroupQueueTriggerAttribute(string queueName,int size)
        {
            GroupSize = size;
            QueueName = queueName;
            MinQueuePollingInterval = 0;
            MaxQueuePollingInterval = 0;
        }

        /// <summary>
        /// Triggers an event with a group of messages
        /// </summary>
        /// <param name="queueName">Name of the Queue</param>
        /// <param name="size">Maximun group size</param>
        /// <param name="minInterval">Minimun queue polling interval (in minutes). Default is 100 milliseconds (Azure Storage QueuePollingIntervals.Minimum).</param>
        /// <param name="maxInterval">Maximun queue polling interval (in minutes). Default is 1 minute.</param>
        public GroupQueueTriggerAttribute(string queueName, int size, int minInterval, int maxInterval)
        {
            GroupSize = size;
            QueueName = queueName;
            MinQueuePollingInterval = minInterval;
            MaxQueuePollingInterval = maxInterval;
        }
        /// <summary>
        /// Triggers an event with a group of messages
        /// </summary>
        /// <param name="queueName">Name of the Queue</param>
        /// <param name="minInterval">Minimun queue polling interval (in minutes). Default is 100 milliseconds (Azure Storage QueuePollingIntervals.Minimum).</param>
        /// <param name="maxInterval">Maximun queue polling interval (in minutes). Default is 1 minute.</param>
        public GroupQueueTriggerAttribute(string queueName, int minInterval, int maxInterval)
        {
            GroupSize = DEFAULT_SIZE;
            QueueName = queueName;
            MinQueuePollingInterval = minInterval;
            MaxQueuePollingInterval = maxInterval;
        }

        /// <summary>
        /// Max group by size
        /// </summary
        public int GroupSize{ get; private set; }

        /// <summary>
        /// Gets the name of the queue to which to bind.
        /// </summary>
        public string QueueName { get; private set; }

        /// <summary>
        /// Minimun queue polling interval (in minutes). Default is 100 milliseconds (Azure Storage QueuePollingIntervals.Minimum)
        /// </summary>
        public int MinQueuePollingInterval { get; private set; }

        /// <summary>
        /// Maximun queue polling interval (in minutes). Default is 1 minute.
        /// </summary>
        public int MaxQueuePollingInterval { get; private set; }
    }
}
