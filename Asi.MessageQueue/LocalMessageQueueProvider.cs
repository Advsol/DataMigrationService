using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Asi.DataMigrationService.MessageQueue.Interfaces;
using Microsoft.Extensions.Logging;

namespace Asi.DataMigrationService.MessageQueue
{
    public class LocalMessageQueueProvider : IMessageQueueProvider
    {
        internal class QueueInfo
        {
            public ActionBlock<Func<Task>> ActionBlock { get; set; }
            public Func<IQueueMessage, Task> Processor { get; set; }
        }

        /// <summary>   The redis connection. </summary>
        private readonly ILogger<LocalMessageQueueProvider> _logger;

        private readonly ConcurrentDictionary<string, QueueInfo> _queues = new ConcurrentDictionary<string, QueueInfo>();

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="logger">           The logging service. </param>
        public LocalMessageQueueProvider(ILogger<LocalMessageQueueProvider> logger)
        {
            _logger = logger;
        }

        /// <summary>   Publishes. </summary>
        ///
        /// <param name="queueDefinition">  The queue definition. </param>
        /// <param name="message">          The message. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public Task PublishAsync(QueueDefinition queueDefinition, IQueueMessage message)
        {
            return PublishAsync(queueDefinition.Name, message);
        }

        /// <summary>   Publishes. </summary>
        ///
        /// <param name="queueName">    Name of the queue. </param>
        /// <param name="message">      The message. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public async Task PublishAsync(string queueName, IQueueMessage message)
        {
            if (_queues.TryGetValue(queueName, out var queue))
            {
                await queue.ActionBlock.SendAsync(() => ProcessMessage(message, queue.Processor));
                return;
            }
            throw new ArgumentException($"Queue not defined.", nameof(queueName));
        }
        private async Task ProcessMessage(IQueueMessage message, Func<IQueueMessage, Task> processor)
        {
            try
            {
                await processor.Invoke(message);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Failed to process message type: {message.GetType()}", exception);
            }
        }

        /// <summary>   Subscribes. </summary>
        ///
        /// <param name="queueDefinition">  The queue definition. </param>
        /// <param name="processor">        The processor. </param>
        ///
        /// <returns>   An ISubscription. </returns>
        public ISubscription Subscribe(QueueDefinition queueDefinition, Func<IQueueMessage, Task> processor)
        {
            var subscription = new LocalSubscription(queueDefinition, processor, _logger, _queues);
            subscription.Start();
            return subscription;
        }
    }
    /// <summary>   The redis subscription. </summary>
    public class LocalSubscription : ISubscription
    {
        private readonly Func<IQueueMessage, Task> _processor;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, LocalMessageQueueProvider.QueueInfo> _queues;
        private bool _isStarted;
        private readonly object _lock = new object();
        private bool _disposedValue = false;

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="queueDefinition">  The queue definition. </param>
        /// <param name="processor">        The processor. </param>
        /// <param name="logger">           The logger. </param>
        /// <param name="queues">           The queues. </param>
        internal LocalSubscription(QueueDefinition queueDefinition, Func<IQueueMessage, Task> processor, ILogger logger, ConcurrentDictionary<string, LocalMessageQueueProvider.QueueInfo> queues)
        {
            _queueDefinition = queueDefinition;
            _processor = processor;
            _logger = logger;
            _queues = queues;
        }

        /// <summary>   Starts this object. </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (!_isStarted)
                {
                    AddQueueHandler(_queueDefinition.Name);
                    if (_queueDefinition.Bindings != null)
                    {
                        foreach (var item in _queueDefinition.Bindings)
                        {
                            AddQueueHandler(item);
                        }
                    }

                    _isStarted = true;
                }
            }
        }

        private void AddQueueHandler(string queueName)
        {
            var actionBlock = new ActionBlock<Func<Task>>(async action =>
            {
                await action.Invoke();
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _queueDefinition.MaxDegreeOfParallelism,
                BoundedCapacity = 100
            });
            _queues.TryAdd(queueName, new LocalMessageQueueProvider.QueueInfo { ActionBlock = actionBlock, Processor = _processor });
        }

        /// <summary>   Stops this object. </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (_isStarted)
                {
                    _isStarted = false;
                }
            }
        }
        #region IDisposable Support
        private readonly QueueDefinition _queueDefinition;

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Stop();
                    foreach (var item in _queues.Values)
                    {
                        item.ActionBlock.Complete();
                    }
                }

                _disposedValue = true;
            }
        }
        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}