using System;
using System.Threading.Tasks;
using Asi.Core.Interfaces.Messaging;
using Asi.Soa.Core.DataContracts;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   Interface for message handler context. </summary>
    public interface IMessageHandlerContext
    {
        #region Properties

        /// <summary>   Gets the identifier of the correlation. </summary>
        ///
        /// <value> The identifier of the correlation. </value>
        string CorrelationId { get; }

        /// <summary>   Gets a value indicating whether this object is request reply. </summary>
        ///
        /// <value> True if this object is request reply, false if not. </value>
        bool IsRequestReply { get; }

        /// <summary>   Gets the lifetime scope. </summary>
        ///
        /// <value> The lifetime scope. </value>
        DateTime MessageSentDateTimeUtc { get; }

        /// <summary>   Gets the identifier of the tenant. </summary>
        ///
        /// <value> The identifier of the tenant. </value>
        string TenantId { get; }

        /// <summary>   Gets the identifier of the user. </summary>
        ///
        /// <value> The identifier of the user. </value>
        string UserName { get; }

        #endregion

        #region Public Instance Methods

        /// <summary>   Publishes the given message. </summary>
        ///
        /// <param name="message">  The message. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task PublishEventAsync(IEvent message);

        /// <summary>   Replies the given response. </summary>
        ///
        /// <param name="response"> The response. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task ResponseAsync(IServiceResponse response);

        /// <summary>   Gets or sets a value indicating whether this object is priority. </summary>
        ///
        /// <value> True if this object is priority, false if not. </value>
        bool IsPriority { get; set; }

        #endregion
    }
}