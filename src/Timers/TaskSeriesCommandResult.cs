using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers
{
    internal struct TaskSeriesCommandResult
    {
        private readonly Task _wait;

        /// <summary>
        /// Wait for this task to complete before calling <see cref="M:Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers.ITaskSeriesCommand.ExecuteAsync(System.Threading.CancellationToken)"/> again.
        /// 
        /// </summary>
        public Task Wait
        {
            get
            {
                return this._wait;
            }
        }

        public TaskSeriesCommandResult(Task wait)
        {
            this._wait = wait;
        }
    }
}
