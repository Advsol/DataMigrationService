using Asi.Core.Interfaces.Messaging;
using IMessageHandlerContext = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageHandlerContext;
using IQueueMessage = Asi.DataMigrationService.MessageQueue.Interfaces.IQueueMessage;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>   A queue message. </summary>
    ///
    /// <remarks>   The top level message and context that is passed over queues. </remarks>
    public class QueueMessage : IQueueMessage
    {
        /// <summary>   Constructor. </summary>
        ///
        /// <param name="message">  The message. </param>
        /// <param name="context">  The context. </param>
        public QueueMessage(IMessage message, IMessageHandlerContext context)
        {
            Message = message;
            Context = context;
        }

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>
        public IMessage Message { get; set; }

        /// <summary>   Gets or sets the context. </summary>
        ///
        /// <value> The context. </value>
        public IMessageHandlerContext Context { get; set; }
    }
}
