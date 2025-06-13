using System.Threading.Tasks;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   Interface for message routing. </summary>
    public interface IMessageRouting
    {
        /// <summary>   Publishes the given queue message. </summary>
        ///
        /// <param name="queueMessage"> Message describing the queue. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task PublishAsync(IQueueMessage queueMessage);

        /// <summary>   Publishes the given queue message. </summary>
        ///
        /// <param name="queueName">    Name of the queue. </param>
        /// <param name="queueMessage"> Message describing the queue. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task PublishAsync(string queueName, IQueueMessage queueMessage);
    }
}
