using System.Threading.Tasks;
using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   Interface for handle messages. </summary>
    public interface IHandleMessages
    {
    }

    /// <summary>   Interface for message handlers. </summary>
    ///
    /// <typeparam name="T">    Generic type parameter. </typeparam>
    public interface IHandleMessages<T> : IHandleMessages where T : IMessage
    {
        #region Public Instance Methods

        /// <summary>   Handles the asynchronous described by command. </summary>
        ///
        /// <param name="message">  The command. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task HandleAsync(T message, IMessageHandlerContext context);

        #endregion
    }
}