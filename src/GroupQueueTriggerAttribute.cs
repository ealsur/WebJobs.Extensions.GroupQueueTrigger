using System;


namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Defines the [GroupQueueTrigger] attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class GroupQueueTriggerAttribute : Attribute
    {
        public GroupQueueTriggerAttribute(string queueName)
        {
            GroupSize = 32;
            QueueName = queueName;
        }
        public GroupQueueTriggerAttribute(string queueName,int size)
        {
            GroupSize = size;
            QueueName = queueName;
        }

        /// <summary>
        /// Max group by size
        /// </summary
        public int GroupSize{ get; private set; }

        /// <summary>
        /// Gets the name of the queue to which to bind.
        /// </summary>
        public string QueueName { get; private set; }
    }
}
