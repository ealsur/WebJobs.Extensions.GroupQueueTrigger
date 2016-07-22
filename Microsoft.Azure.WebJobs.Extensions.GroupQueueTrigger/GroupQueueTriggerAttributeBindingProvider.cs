using Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger.Timers;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger
{
    /// <summary>
    /// Binding provider for [GroupQueueTrigger]
    /// </summary>
    internal class GroupQueueTriggerAttributeBindingProvider :  ITriggerBindingProvider
    {
        private readonly string _storageConnectionString;
        /// <summary>
        /// [GroupQueueTrigger] constructor
        /// </summary>
        /// <param name="storageConnectionString">Azure Storage Account connection string</param>
        public GroupQueueTriggerAttributeBindingProvider(string storageConnectionString)
        {
            _storageConnectionString = storageConnectionString;
        }
        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            //Tries to parse the context parameters and see if it belongs to this [GroupQueueTrigger] binder
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<GroupQueueTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            //It does, so we create the necessary acceses and internal variables and bind it
            WindowsAzure.Storage.Queue.CloudQueue queue;
            WindowsAzure.Storage.Queue.CloudQueue poisonQueue;
            try {
                var account = WindowsAzure.Storage.CloudStorageAccount.Parse(_storageConnectionString);
                var client = account.CreateCloudQueueClient();
                queue = client.GetQueueReference(NormalizeAndValidate(attribute.QueueName));
                queue.CreateIfNotExists();
                poisonQueue = client.GetQueueReference(NormalizeAndValidate(attribute.QueueName)+"-poison");
            }
            catch(Exception ex)
            {
                throw new Exception(string.Format("Cannot connect to Storage Queue {0}: {1}",attribute.QueueName, ex.Message));
            }
            return Task.FromResult<ITriggerBinding>(new GroupQueueTriggerBinding(parameter, queue, poisonQueue, attribute.GroupSize, attribute.MinQueuePollingInterval, attribute.MaxQueuePollingInterval));
        }

        /// <summary>
        /// Normalizes queue access names
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        private static string NormalizeAndValidate(string queueName)
        {
            return queueName.ToLowerInvariant();
        }

        /// <summary>
        /// [GroupQueueTrigger] Binding logic
        /// </summary>
        private class GroupQueueTriggerBinding : ITriggerBinding
        {
            private readonly ParameterInfo _parameter;
            private readonly int _groupSize;
            private readonly int _intervalMin;
            private readonly int _intervalMax;
            private readonly IBindingDataProvider _bindingDataProvider;
            private readonly WindowsAzure.Storage.Queue.CloudQueue _queue;
            private readonly WindowsAzure.Storage.Queue.CloudQueue _poisonQueue;
            public GroupQueueTriggerBinding(ParameterInfo parameter, WindowsAzure.Storage.Queue.CloudQueue queue, WindowsAzure.Storage.Queue.CloudQueue poisonQueue, int groupSize, int intervalMin, int intervalMax)
            {
                //Support for List<POCO> types only
                _bindingDataProvider = BindingDataProvider.FromType(parameter.ParameterType);
                _parameter = parameter;
                _queue = queue;
                _poisonQueue = poisonQueue;
                _groupSize = groupSize;
                _intervalMin = intervalMin;
                _intervalMax = intervalMax;
            }
            
            /// <summary>
            /// Binding contract based on POCO user type
            /// </summary>
            public IReadOnlyDictionary<string, Type> BindingDataContract
            {
                get { return _bindingDataProvider.Contract; }
            }

            /// <summary>
            /// Type of value that the Trigger receives from the Executor
            /// </summary>
            public Type TriggerValueType
            {
                get { return typeof(List<WindowsAzure.Storage.Queue.CloudQueueMessage>); }
            }
            
            /// <summary>
            /// Function called when the Executor fires the triggering event
            /// </summary>
            /// <param name="value"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
            {
                //Receives a list of CloudQueueMessages
                List<WindowsAzure.Storage.Queue.CloudQueueMessage> output = (List<WindowsAzure.Storage.Queue.CloudQueueMessage>)null;
                try
                {
                    output = (List<WindowsAzure.Storage.Queue.CloudQueueMessage>)value;
                }
                catch(Exception ex)
                {
                    throw new InvalidOperationException("Unable to convert trigger to CloudQueueMessage:"+ex.Message);
                }
                
                var invokeString = string.Format("[{0}]", string.Join(",", output.Select(x => x.AsString).ToArray()));
                if (_parameter.ParameterType.Equals(typeof(List<String>)))
                {
                    invokeString = string.Format("[{0}]", string.Join(",", output.Select(x => string.Format("\"{0}\"",x.AsString)).ToArray()));
                }
                //We read the AsString value, since it contains the JSON Serialized POCO object
                var deserializedObject = JsonConvert.DeserializeObject(invokeString, _parameter.ParameterType);
                //Deserialize the JSON POCO array
                var valueProvider = new GroupQueueValueBinder(_parameter.ParameterType, deserializedObject, invokeString);
                var bindingData = _bindingDataProvider.GetBindingData(valueProvider.GetValue());
                //Trigger the User Function with the data
                return new TriggerData(valueProvider, bindingData);
                
            }
            
            /// <summary>
            /// Creates the Listener that polls the Queue
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
            {
                return Task.FromResult<IListener>(new GroupQueueListener(context.Executor, _parameter,_queue,_poisonQueue, _groupSize, _intervalMin, _intervalMax));
            }

            /// <summary>
            /// Shows display information on the dashboard
            /// </summary>
            /// <returns></returns>
            public ParameterDescriptor ToParameterDescriptor()
            {
                return new GroupQueueTriggerParameterDescriptor
                {
                    Name = _parameter.Name,
                    Type="GroupQueueTrigger",
                    QueueName = _queue.Name
                };
            }
            
            /// <summary>
            /// Descriptor that creates the message for the dashboard in case of a Trigger event
            /// </summary>
            private class GroupQueueTriggerParameterDescriptor : TriggerParameterDescriptor
            {
                public string QueueName { get; set; }
                public override string GetTriggerReason(IDictionary<string, string> arguments)
                {
                    return string.Format("New message on queue {0} at {1}",QueueName, DateTime.UtcNow.ToString("o"));
                }
            }

            /// <summary>
            /// Wraps the POCO string and identifies the value that was deserialized on the BindAsync trigger
            /// </summary>
            private class GroupQueueValueBinder : IValueProvider
            {
                private readonly Type _type;
                private readonly object _value;
                private readonly string _invokeString;

                public GroupQueueValueBinder(Type type, object value, string invokeString)
                {
                    _type = type;
                    _value = value;
                    _invokeString = invokeString;
                }

                public Type Type
                {
                    get
                    {
                        return _type;
                    }
                }

                public object GetValue()
                {
                    return _value;
                }

                public string ToInvokeString()
                {
                    return _invokeString;
                }
            }
            
            /// <summary>
            /// Listener that polls the Queue. It uses a Timer with a Randomized Exponential backoff strategy to poll and get messages.
            /// </summary>
            private class GroupQueueListener : ITaskSeriesCommand, IListener
            {
                /// <summary>
                /// List of tasks created when the triggers fire. Populated by the ExecuteAsync from the Executor
                /// </summary>
                private readonly List<Task> _processing = new List<Task>();
                private readonly TaskSeriesTimer _timer;
                private ITriggeredFunctionExecutor _executor;
                private readonly int _groupSize;
                private readonly WindowsAzure.Storage.Queue.CloudQueue _queue;
                private readonly WindowsAzure.Storage.Queue.CloudQueue _poisonQueue;
                private readonly ParameterInfo _triggerParameter;
                private bool _disposed = true;
                private readonly uint _newBatchThreshold;
                private bool _foundMessageSinceLastDelay;
                private readonly object _stopWaitingTaskSourceLock = new object();
                private TaskCompletionSource<object> _stopWaitingTaskSource;
                private RandomizedExponentialBackoffStrategy _delayStrategy;
                private readonly int _maxDequeueCount = 100;
                public GroupQueueListener(ITriggeredFunctionExecutor executor, ParameterInfo triggerParameter, WindowsAzure.Storage.Queue.CloudQueue queue, WindowsAzure.Storage.Queue.CloudQueue poisonQueue, int groupSize, int intervalMin, int intervalMax)
                {
                    _executor = executor;
                    if (groupSize <= 0)
                        throw new ArgumentOutOfRangeException("groupSize");
                    _groupSize = groupSize;
                    this._newBatchThreshold = (uint)this._groupSize / 2U;
                    _triggerParameter = triggerParameter;
                    _queue = queue;
                    _poisonQueue = poisonQueue;
                    _delayStrategy = new RandomizedExponentialBackoffStrategy(intervalMin==0?QueuePollingIntervals.Minimum:TimeSpan.FromMinutes(intervalMin), intervalMax==0?QueuePollingIntervals.DefaultMaximum:TimeSpan.FromMinutes(intervalMax));
                    this._timer = new TaskSeriesTimer((ITaskSeriesCommand)this, Task.Delay(0));
                }
                /// <summary>
                /// Starts polling
                /// </summary>
                /// <param name="cancellationToken"></param>
                /// <returns></returns>
                public Task StartAsync(CancellationToken cancellationToken)
                {
                    this._timer.Start();
                    return Task.FromResult(true);
                }

                /// <summary>
                /// Stops polling and cancels all processing Tasks
                /// </summary>
                /// <param name="cancellationToken"></param>
                /// <returns></returns>
                public async Task StopAsync(CancellationToken cancellationToken)
                {
                    this._timer.Cancel();
                    await Task.WhenAll((IEnumerable<Task>)this._processing);
                    await this._timer.StopAsync(cancellationToken);
                }

                public void Dispose()
                {
                    if (this._disposed)
                        return;
                    this._timer.Dispose();
                    this._disposed = true;
                }

                public void Cancel()
                {
                    this._timer.Cancel();
                }

                #region Internal Results
                private TaskSeriesCommandResult CreateSucceededResult()
                {
                    return new TaskSeriesCommandResult(this.WaitForNewBatchThreshold());
                }
                private TaskSeriesCommandResult CreateBackoffResult()
                {
                    return new TaskSeriesCommandResult(this.CreateDelayWithNotificationTask());
                }
                private async Task WaitForNewBatchThreshold()
                {
                    while ((long)this._processing.Count > (long)this._newBatchThreshold)
                    {
                        Task processed = await Task.WhenAny((IEnumerable<Task>)this._processing);
                        this._processing.Remove(processed);
                    }
                }
                private Task CreateDelayWithNotificationTask()
                {
                    Task task = Task.Delay(this._delayStrategy.GetNextDelay(this._foundMessageSinceLastDelay));
                    this._foundMessageSinceLastDelay = false;
                    return (Task)Task.WhenAny((Task)this._stopWaitingTaskSource.Task, task);
                } 
                #endregion

                /// <summary>
                /// Queue Polling. Fires everytime the Timer says so.
                /// </summary>
                /// <param name="cancellationToken"></param>
                /// <returns></returns>
                public async Task<TaskSeriesCommandResult> ExecuteAsync(CancellationToken cancellationToken)
                {
                    lock (this._stopWaitingTaskSourceLock)
                    {
                        if (this._stopWaitingTaskSource != null)
                            this._stopWaitingTaskSource.TrySetResult((object)null);
                        this._stopWaitingTaskSource = new TaskCompletionSource<object>();
                    }

                    TaskSeriesCommandResult seriesCommandResult;
                    
                    TimeSpan visibilityTimeout = TimeSpan.FromMinutes(10.0);
                    IEnumerable<WindowsAzure.Storage.Queue.CloudQueueMessage> batch;
                    try
                    {
                        batch = await this._queue.GetMessagesAsync(this._groupSize, new TimeSpan?(visibilityTimeout), (QueueRequestOptions)null, (OperationContext)null, cancellationToken);                        
                    }
                    catch (StorageException ex)
                    {
                        throw;
                    }
                    if (batch == null || !batch.Any())
                    {
                        seriesCommandResult = this.CreateBackoffResult();
                    }
                    else
                    {
                        this._processing.Add(this.ProcessMessageAsync(batch.ToList(), visibilityTimeout, cancellationToken));
                        this._foundMessageSinceLastDelay = true;
                        seriesCommandResult = this.CreateSucceededResult();
                    }
                    
                    return seriesCommandResult;
                }

                /// <summary>
                /// Sends the detected messages to the Executor and creates the next timer event depending on Success
                /// </summary>
                /// <param name="messages"></param>
                /// <param name="visibilityTimeout"></param>
                /// <param name="cancellationToken"></param>
                /// <returns></returns>
                private async Task ProcessMessageAsync(List<WindowsAzure.Storage.Queue.CloudQueueMessage> messages, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
                {
                    try
                    {
                        bool succeeded;
                        
                        succeeded = (await this._executor.TryExecuteAsync(new TriggeredFunctionData() { TriggerValue=messages }, cancellationToken)).Succeeded;

                        if (succeeded)
                        {
                            foreach (var message in messages)
                            {
                                await this._queue.DeleteMessageAsync(message, cancellationToken);
                            }
                        }
                        else if (this._poisonQueue != null)
                        {
                            foreach (var message in messages)
                            {
                                if (message.DequeueCount >= this._maxDequeueCount)
                                {
                                    await this.CopyToPoisonQueueAsync(message, cancellationToken);
                                    await this._queue.DeleteMessageAsync(message, cancellationToken);
                                }
                            }                            
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }

                /// <summary>
                /// Creates a copy on the poison queue
                /// </summary>
                /// <param name="message"></param>
                /// <param name="cancellationToken"></param>
                /// <returns></returns>
                private async Task CopyToPoisonQueueAsync(WindowsAzure.Storage.Queue.CloudQueueMessage message, CancellationToken cancellationToken)
                {
                    await this._poisonQueue.CreateIfNotExistsAsync();
                    await this._poisonQueue.AddMessageAsync(message, cancellationToken);
                }
            }
        }
    }
}


