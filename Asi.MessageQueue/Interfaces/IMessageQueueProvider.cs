using System;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   Interface for message queue provider. </summary>
    public interface IMessageQueueProvider
    {
        /// <summary>   Publishes. </summary>
        ///
        /// <param name="queueDefinition">  The queue definition. </param>
        /// <param name="message">          The message. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task PublishAsync(QueueDefinition queueDefinition, IQueueMessage message);

        /// <summary>   Publishes. </summary>
        ///
        /// <param name="queueName">    Name of the queue. </param>
        /// <param name="message">      The message. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task PublishAsync(string queueName, IQueueMessage message);

        /// <summary>   Subscribes. </summary>
        ///
        /// <param name="queueDefinition">  The queue definition. </param>
        /// <param name="processor">        The processor. </param>
        ///
        /// <returns>   An ISubscription. </returns>
        ISubscription Subscribe(QueueDefinition queueDefinition, Func<IQueueMessage, Task> processor);
    }
}
