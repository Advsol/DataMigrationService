using System.Collections.Generic;
using System.Linq;
using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>   A message routing rules. </summary>
    public class MessageRoutingRules
    {
        private IList<MessageRoutingRule> _routingRules = null;

        /// <summary>   Gets the standard routing rules. </summary>
        ///
        /// <value> The standard routing rules. </value>
        public IList<MessageRoutingRule> StandardRoutingRules
        {
            get
            {
                if (_routingRules == null)
                {
                    _routingRules = new List<MessageRoutingRule> {
                        new MessageRoutingRule { MessageType = typeof(ICommand), EndpointName = "CommandMain_Priority", IsPriorityMessage = true },
                        new MessageRoutingRule { MessageType = typeof(ICommand), EndpointName = "CommandMain" },
                        new MessageRoutingRule { MessageType = typeof(IEvent), EndpointName = "Event"}
                    }.OrderByDescending(p => p.RulePriority).ToList();
                }
                return _routingRules;
            }
        }
    }
}
