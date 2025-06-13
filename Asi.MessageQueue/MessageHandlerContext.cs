using Asi.Core.Interfaces.Messaging;
using Asi.Soa.Core.DataContracts;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using IMessageHandlerContext = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageHandlerContext;
using IMessageRouting = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageRouting;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>   A message handler context. </summary>
    [DataContract]
    public class MessageHandlerContext : IMessageHandlerContext
    {
        #region Constructors
        /// <summary>   Default constructor. </summary>
        public MessageHandlerContext() { }

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="tenantId">         Identifier for the tenant. </param>
        /// <param name="userName">         Identifier for the user. </param>
        /// <param name="correlationId">    Identifier for the correlation. </param>
        /// <param name="messageRouting">   The message routing. </param>
        public MessageHandlerContext(string tenantId, string userName, string correlationId, IMessageRouting messageRouting)
        {
            CorrelationId = correlationId ?? ShortGuid.NewGuid();
            MessageRouting = messageRouting;
            TenantId = tenantId;
            UserName = userName;
            MessageSentDateTimeUtc = DateTime.UtcNow;
        }

        /// <summary>   Constructor. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="context">  The context. </param>
        public MessageHandlerContext(IMessageHandlerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            TenantId = context.TenantId;
            UserName = context.UserName;
            CorrelationId = context.CorrelationId;
        }

        #endregion

        #region IMessageHandlerContext Members

        /// <inheritdoc/>
        [DataMember]
        public string CorrelationId { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public bool IsRequestReply { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public DateTime MessageSentDateTimeUtc { get; set; }

        public IMessageRouting MessageRouting { get; }

        /// <inheritdoc/>
        public Task PublishEventAsync(IEvent message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            if (MessageRouting is null) throw new ArgumentNullException(nameof(MessageRouting));

            var queueMessage = new QueueMessage(message, CloneForPassForward());
            return MessageRouting.PublishAsync(queueMessage);
        }

        /// <inheritdoc/>
        public Task ResponseAsync(IServiceResponse response)
        {
            if (response is null) throw new ArgumentNullException(nameof(response));
            if (IsRequestReply)
            {
                if (ReplyQueueName == null) throw new ArgumentNullException(nameof(ReplyQueueName));
                if (MessageRouting is null) throw new ArgumentNullException(nameof(MessageRouting));
                var replyMessage = new QueueMessage(new Reply(response), CloneForPassForward());
                return MessageRouting.PublishAsync(ReplyQueueName, replyMessage);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        [DataMember]
        public string TenantId { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public string UserName { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public string ReplyQueueName { get; set; }

        /// <summary>   Gets or sets a value indicating whether this object is priority. </summary>
        ///
        /// <value> True if this object is priority, false if not. </value>
        [DataMember]
        public bool IsPriority { get; set; }

        private IMessageHandlerContext CloneForPassForward()
        {
            var mhc = (IMessageHandlerContext)MemberwiseClone();
            mhc.IsPriority = false;
            return mhc;
        }
        #endregion

    }
}