
using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   Interface for queue message. </summary>
    ///  <remarks>
    /// The top level message and context thet is passed over queues
    /// </remarks>
    public interface IQueueMessage
    {
        /// <summary>   Gets or sets the context. </summary>
        ///
        /// <value> The context. </value>
        IMessageHandlerContext Context { get; set; }

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>
        IMessage Message { get; set; }
    }
}
