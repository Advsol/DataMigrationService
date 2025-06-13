using Asi.DataMigrationService.Core;
using Asi.Soa.Core.DataContracts;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   A send options. </summary>
    public class SendOptions
    {
        /// <summary>   Gets the identifier of the correlation. </summary>
        ///
        /// <value> The identifier of the correlation. </value>
        public string CorrelationId { get; } = ShortGuid.NewGuid();

        /// <summary>   Gets or sets a value indicating whether this object is priority. </summary>
        ///
        /// <value> True if this object is priority, false if not. </value>
        public bool IsPriority { get; set; }

        /// <summary>   Gets or sets the identity. </summary>
        ///
        /// <value> The identity. </value>
        public IServiceContext ServiceContext { get; set; }
    }
}