using System.Threading.Tasks;
using Asi.Core.Interfaces.Messaging;
using Asi.Soa.Core.DataContracts;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   Interface for message queue endpoint. </summary>
    public interface IMessageQueueEndpoint
    {
        /// <summary>   Send this message. </summary>
        ///
        /// <param name="message">  The message. </param>
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task SendAsync(IMessage message, SendOptions options);

        /// <summary>   Requests. </summary>
        ///
        /// <typeparam name="TResult">  Type of the service response. </typeparam>
        /// <param name="message">  The message. </param>
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   An asynchronous result that yields an IServiceResponse. </returns>
        Task<IServiceResponse> RequestAsync(IMessage message, SendOptions options);

        /// <summary>   Publish event. </summary>
        ///
        /// <param name="message">  The message. </param>
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task PublishEventAsync(IEvent message, SendOptions options);
    }

    /// <summary>   Interface for message queue endpoint control. </summary>
    public interface IMessageQueueEndpointControl
    {
        /// <summary>   Starts this object. </summary>
        void Start();
        /// <summary>   Stops this object. </summary>
        void Stop();
    }
}
