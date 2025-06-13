using System;
using System.Threading.Tasks;
using Asi.DataMigrationService.MessageQueue.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>
    /// A class that will subscribe to a queue and dispatch messages to message handlers.
    /// </summary>
    public class QueueHandler : IQueueHandler
    {
        private readonly QueueDefinition _queueDefinition;
        private readonly IMessageQueueProvider _messageQueueProvider;
        private readonly ILogger<QueueHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        private ISubscription _subscription = null;

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="queueDefinition">      The name of the queue. </param>
        /// <param name="messageQueueProvider"> The message queue provider. </param>
        /// <param name="logger">               The logging service. </param>
        public QueueHandler(QueueDefinition queueDefinition, IMessageQueueProvider messageQueueProvider, ILogger<QueueHandler> logger, IServiceProvider serviceProvider)
        {
            _queueDefinition = queueDefinition;
            _messageQueueProvider = messageQueueProvider;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>   Gets the name of the queue. </summary>
        ///
        /// <value> The name of the queue. </value>
        public string QueueName => _queueDefinition.Name;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private Task ProcessMessage(IQueueMessage queueMessage)
        {
            try
            {
                return HandleMessage(queueMessage);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Queue handler exception. ");
            }
            return Task.CompletedTask;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private async Task HandleMessage(object message)
        {
            if (message is IQueueMessage queueMessage)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    await MessageDispatcher.DispatchMessageAsync(scope.ServiceProvider, queueMessage.Message, queueMessage.Context);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>   Starts this object. </summary>
        public void Start()
        {
            _subscription = _messageQueueProvider.Subscribe(_queueDefinition, (message) => ProcessMessage(message));
        }

        /// <summary>   Stops this object. </summary>
        public void Stop()
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
        }

        #region IDisposable Support
        /// <summary>   To detect redundant calls. </summary>
        private bool _disposedValue = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    Stop();
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
