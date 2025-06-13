using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asi.DataMigrationService.MessageQueue.Interfaces;
using Microsoft.Extensions.Logging;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>   A message routing. </summary>
    public class MessageRouting : IMessageRouting, IDisposable
    {
        private readonly IMessageQueueProvider _messageQueueProvider;
        private readonly ILogger<MessageRouting> _logger;
        private readonly MessageRoutingRules _messageRoutingRules;
        private readonly IList<IQueueHandler> _ownedQueueHandlers = new List<IQueueHandler>();

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="messageQueueProvider"> The message queue provider. </param>
        /// <param name="loggingService">       The logging service. </param>
        /// <param name="messageRoutingRules">  The message routing rules. </param>
        ///
        /// ### <param name="standardQueues">   The standard queues. </param>
        public MessageRouting(IMessageQueueProvider messageQueueProvider,ILogger<MessageRouting> logger, MessageRoutingRules messageRoutingRules)
        {
            _messageQueueProvider = messageQueueProvider;
            _logger = logger;
            _messageRoutingRules = messageRoutingRules;
            Start();
        }

        /// <summary>   Publishes the given queue message. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="queueMessage"> Message describing the queue. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public Task PublishAsync(IQueueMessage queueMessage)
        {
            if (queueMessage is null) throw new ArgumentNullException(nameof(queueMessage));
            if (queueMessage.Message is null) throw new ArgumentNullException(nameof(queueMessage));
            if (queueMessage.Context is null) throw new ArgumentNullException(nameof(queueMessage));

            var context = (MessageHandlerContext)queueMessage.Context;

            var endpoints = _messageRoutingRules.StandardRoutingRules.Where(p => p.IsMatch(queueMessage)).Select(p => p.EndpointName).Distinct().ToList();
            foreach (var endpoint in endpoints)
            {
                PublishAsync(endpoint, queueMessage);
            }
            if (endpoints.Count == 0)
                _logger.LogInformation($"Could not find a destination for message type: {queueMessage.Message.GetType().Name}");

            return Task.CompletedTask;
        }

        /// <summary>   Publishes the given queue message. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="queueName">    Name of the queue. </param>
        /// <param name="queueMessage"> Message describing the queue. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public Task PublishAsync(string queueName, IQueueMessage queueMessage)
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (queueMessage is null) throw new ArgumentNullException(nameof(queueMessage));

            _messageQueueProvider.PublishAsync(queueName, queueMessage);
            return Task.CompletedTask;
        }

        /// <summary>   Perform once-off startup processing. </summary>
        public void Start()
        {
        }

        /// <summary>   Stops this object. </summary>
        public void Stop()
        {
            foreach (var queueHandler in _ownedQueueHandlers)
            {
                queueHandler.Stop();
                queueHandler.Dispose();
            }
            _ownedQueueHandlers.Clear();
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool _disposedValue = false;

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
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

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}
