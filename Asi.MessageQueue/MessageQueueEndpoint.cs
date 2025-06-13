using Asi.Core.Interfaces.Messaging;
using Asi.DataMigrationService.Core;
using Asi.Soa.Core.DataContracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IMessageHandlerContext = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageHandlerContext;
using IMessageQueueEndpoint = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageQueueEndpoint;
using IMessageQueueEndpointControl = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageQueueEndpointControl;
using IMessageRouting = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageRouting;
using IQueueHandler = Asi.DataMigrationService.MessageQueue.Interfaces.IQueueHandler;
using QueueDefinition = Asi.DataMigrationService.MessageQueue.Interfaces.QueueDefinition;
using SendOptions = Asi.DataMigrationService.MessageQueue.Interfaces.SendOptions;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>   A message queue endpoint. </summary>
    ///
    /// <remarks>
    /// An endpoint supports the following operations:
    /// <para>Send and Request methods.</para>
    /// <para>An optional command queue handler.</para>
    /// <para>An Event queue handler</para>
    /// </remarks>
    public class MessageQueueEndpoint : IMessageQueueEndpoint, IDisposable, Interfaces.IHandleMessages<Reply>, IMessageQueueEndpointControl
    {
        private readonly QueueDefinition _instanceQueue;
        private readonly IEnumerable<QueueDefinition> _endpointQueues;
        private readonly Func<QueueDefinition, IQueueHandler> _queueHandler;
        private readonly IMessageRouting _messageRouting;
        private readonly ILogger<MessageQueueEndpoint> _logger;
        private readonly CancellationTokenSource _masterCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _masterCancellationToken;
        private readonly ConcurrentDictionary<string, BlockingCollection<IServiceResponse>> _requests = new ConcurrentDictionary<string, BlockingCollection<IServiceResponse>>();
        private readonly IList<IQueueHandler> _ownedQueueHandlers = new List<IQueueHandler>();
        private bool _disposedValue = false;

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="instanceQueue">    The name of the endpoint. </param>
        /// <param name="endpointQueues">   The endpoint queues. </param>
        /// <param name="queueHandler">     The queue handler. </param>
        /// <param name="messageRouting">   The message routing. </param>
        /// <param name="logger">           The logger. </param>
        public MessageQueueEndpoint(QueueDefinition instanceQueue, IEnumerable<QueueDefinition> endpointQueues, Func<QueueDefinition, IQueueHandler> queueHandler, IMessageRouting messageRouting, ILogger<MessageQueueEndpoint> logger)
        {
            _instanceQueue = instanceQueue;
            _endpointQueues = endpointQueues;
            _queueHandler = queueHandler;
            _messageRouting = messageRouting;
            _logger = logger;
            _masterCancellationToken = _masterCancellationTokenSource.Token;
        }

        /// <summary>   Requests. </summary>
        ///
        /// <typeparam name="IResult">  Type of the result. </typeparam>
        /// <param name="message">  The message. </param>
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   An asynchronous result that yields an IResult. </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task<IServiceResponse> RequestAsync(IMessage message, SendOptions options) 
        {
            var context = new MessageHandlerContext(options.ServiceContext.TenantId, options.ServiceContext.Identity.Name, options.CorrelationId, _messageRouting)
            {
                IsRequestReply = true,
                ReplyQueueName = _instanceQueue.Name,
                IsPriority = options.IsPriority
            };
            var queueMessage = new QueueMessage(message, context);

            var correlationId = queueMessage.Context.CorrelationId;
            var bag = new BlockingCollection<IServiceResponse>();
            _requests.TryAdd(correlationId, bag);
            IServiceResponse result = null;
            try
            {
                var timer = new CancellationTokenSource(GlobalSettings.MaximumReplyWaitTime);
                var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(timer.Token, _masterCancellationToken);
                await _messageRouting.PublishAsync(queueMessage);
                result = bag.Take(tokenSource.Token);
            }
            catch (OperationCanceledException)
            { }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Request processing: failed to queue or process request.");
            }
            finally
            {
                if (_requests.TryRemove(correlationId, out bag))
                    bag.Dispose();
            }

            return result;
        }

        /// <summary>   Send this message. </summary>
        ///
        /// <param name="message">  The message. </param>
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public Task SendAsync(IMessage message, SendOptions options)
        {
            var context = new MessageHandlerContext(options.ServiceContext.TenantId, options.ServiceContext.Identity.Name, options.CorrelationId, _messageRouting);
            var queueMessage = new QueueMessage(message, context);
            return _messageRouting.PublishAsync(queueMessage);
        }

        /// <summary>   Perform once-off startup processing. </summary>
        public void Start()
        {
            if (_instanceQueue != null)
                _ownedQueueHandlers.Add(_queueHandler.Invoke(_instanceQueue));
            if(_endpointQueues != null)
            {
                foreach (var item in _endpointQueues)
                {
                    _ownedQueueHandlers.Add(_queueHandler.Invoke(item));
                }
            }
            foreach (var queueHandler in _ownedQueueHandlers)
            {
                queueHandler.Start();
            }
        }

        /// <summary>   Stops this object. </summary>
        public void Stop()
        {
            if (_masterCancellationTokenSource != null)
                _masterCancellationTokenSource.Cancel();
            foreach (var queueHandler in _ownedQueueHandlers)
            {
                queueHandler.Stop();
                queueHandler.Dispose();
            }
            _ownedQueueHandlers.Clear();
        }

        /// <summary>   Publish event. </summary>
        ///
        /// <param name="message">  The message. </param>
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public Task PublishEventAsync(IEvent message, SendOptions options)
        {
            return SendAsync(message, options);
        }

        /// <summary>   Handles the asynchronous described by command. </summary>
        ///
        /// <param name="message">  The command. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public Task HandleAsync(Reply message, IMessageHandlerContext context)
        {
            if (_requests.TryGetValue(context.CorrelationId, out var bag))
                bag.Add((IServiceResponse)message.ReplyMessage);
            return Task.CompletedTask;
        }

        #region IDisposable Support
        /// <summary>   True to disposed value. </summary>
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
                {
                    Stop();
                    _masterCancellationTokenSource.Cancel();
                    _masterCancellationTokenSource.Dispose();
                }
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
