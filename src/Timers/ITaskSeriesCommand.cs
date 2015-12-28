using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers
{
    internal interface ITaskSeriesCommand
    {
        Task<TaskSeriesCommandResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}
