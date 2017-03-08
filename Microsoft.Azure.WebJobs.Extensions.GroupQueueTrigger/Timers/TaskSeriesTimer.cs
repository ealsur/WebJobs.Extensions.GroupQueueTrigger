using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers
{
    internal sealed class TaskSeriesTimer :  IDisposable
    {
        private readonly ITaskSeriesCommand _command;
        private readonly Task _initialWait;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _started;
        private bool _stopped;
        private Task _run;
        private bool _disposed;

        public TaskSeriesTimer(ITaskSeriesCommand command, Task initialWait)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            if (initialWait == null)
                throw new ArgumentNullException("initialWait");
            this._command = command;
            this._initialWait = initialWait;
            this._cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            this.ThrowIfDisposed();
            if (this._started)
                throw new InvalidOperationException("The timer has already been started; it cannot be restarted.");
            this._run = this.RunAsync(this._cancellationTokenSource.Token);
            this._started = true;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.ThrowIfDisposed();
            if (!this._started)
                throw new InvalidOperationException("The timer has not yet been started.");
            if (this._stopped)
                throw new InvalidOperationException("The timer has already been stopped.");
            this._cancellationTokenSource.Cancel();
            return this.StopAsyncCore(cancellationToken);
        }

        private async Task StopAsyncCore(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
            TaskCompletionSource<object> cancellationTaskSource = new TaskCompletionSource<object>();
            using (cancellationToken.Register((Action)(() => cancellationTaskSource.SetCanceled())))
            {
                Task task = await Task.WhenAny(this._run, (Task)cancellationTaskSource.Task);
            }
            this._stopped = true;
        }

        public void Cancel()
        {
            this.ThrowIfDisposed();
            this._cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (this._disposed)
                return;
            this._cancellationTokenSource.Cancel();
            this._disposed = true;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Yield();
                Task wait = this._initialWait;
                while (!cancellationToken.IsCancellationRequested)
                {
                    TaskCompletionSource<object> cancellationTaskSource = new TaskCompletionSource<object>();
                    using (cancellationToken.Register((Action)(() => cancellationTaskSource.SetCanceled())))
                    {
                        try
                        {
                            Task task = await Task.WhenAny(wait, (Task)cancellationTaskSource.Task);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            TaskSeriesCommandResult result = await this._command.ExecuteAsync(cancellationToken);
                            wait = result.Wait;
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    else
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
                throw new ObjectDisposedException((string)null);
        }
    }
}
