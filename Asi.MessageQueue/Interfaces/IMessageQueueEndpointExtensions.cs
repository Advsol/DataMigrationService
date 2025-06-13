using System.Threading.Tasks;
using Asi.Core.Interfaces.Messaging;
using Asi.Soa.Core.DataContracts;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   A message queue endpoint extensions. </summary>
    public static class IMessageQueueEndpointExtensions
    {
        /// <summary>   An IMessageQueueEndpoint extension method that send this message. </summary>
        ///
        /// <param name="messageQueueEndpoint"> The messageQueueEndpoint to act on. </param>
        /// <param name="message">              The message. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public static Task SendAsync(this IMessageQueueEndpoint messageQueueEndpoint, IMessage message)
        {
            return messageQueueEndpoint.SendAsync(message, new SendOptions());
        }

        /// <summary>   An IMessageQueueEndpoint extension method that requests. </summary>
        ///
        /// <typeparam name="TResult">  Type of the result. </typeparam>
        /// <param name="messageQueueEndpoint"> The messageQueueEndpoint to act on. </param>
        /// <param name="message">              The message. </param>
        ///
        /// <returns>   An asynchronous result that yields a TResult. </returns>
        public static Task<IServiceResponse> RequestAsync<TResult>(this IMessageQueueEndpoint messageQueueEndpoint, IMessage message) where TResult : class
        {
            return messageQueueEndpoint.RequestAsync(message, new SendOptions());
        }

        /// <summary>   An IMessageQueueEndpoint extension method that publish event. </summary>
        ///
        /// <param name="messageQueueEndpoint"> The messageQueueEndpoint to act on. </param>
        /// <param name="message">              The message. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public static Task PublishEventAsync(this IMessageQueueEndpoint messageQueueEndpoint, IEvent message)
        {
            return messageQueueEndpoint.PublishEventAsync(message, new SendOptions());
        }
    }
}
