using System;
using System.Reflection;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.MessageQueue.Interfaces;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>   A message routing rule. </summary>
    public class MessageRoutingRule
    {
        /// <summary>   The namespace. </summary>
        private string _namespace;

        /// <summary>   Gets or sets the type of the message. </summary>
        ///
        /// <value> The type of the message. </value>
        public Type MessageType { get; set; }

        /// <summary>   Gets or sets the assembly. </summary>
        ///
        /// <value> The assembly. </value>
        public Assembly Assembly { get; set; }

        /// <summary>   Gets or sets the namespace. </summary>
        ///
        /// <value> The namespace. </value>
        public string Namespace { get => _namespace; set => _namespace = value.NullTrim(); }

        /// <summary>   Gets or sets the name of the endpoint. </summary>
        ///
        /// <value> The name of the endpoint. </value>
        public string EndpointName { get; set; }

        /// <summary>   Gets or sets the priority. </summary>
        ///
        /// <value> The priority. </value>
        
        public bool IsPriorityMessage { get; set; }

        /// <summary>   Gets the rule priority. </summary>
        ///
        /// <value> The rule priority. </value>
        public int RulePriority
        {
            get
            {
                var priority = 0;
                if (!MessageType.IsInterface)
                    priority = 10;
                else if (!string.IsNullOrEmpty(Namespace))
                    priority = 5;
                else if (Assembly != null)
                    priority = 3;
                return priority;
            }
        }

        /// <summary>   Query if 'message' is match. </summary>
        ///
        /// <param name="queueMessage"> The message. </param>
        ///
        /// <returns>   True if match, false if not. </returns>
        public bool IsMatch(IQueueMessage queueMessage)
        {
            if (queueMessage.Context.IsPriority != IsPriorityMessage) return false;
            var message = queueMessage.Message;
            var messageType = message.GetType();
            if (MessageType != null && MessageType.IsAssignableFrom(messageType)) return true;
            if (Assembly != null && messageType.Assembly == Assembly)
                if (Namespace == null || Namespace.EqualsOrdinalIgnoreCase(messageType.Namespace)) return true;
            return Namespace != null && Namespace.EqualsOrdinalIgnoreCase(messageType.Namespace);
        }
    }
}
