using System.Collections.Generic;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   A queue definition. </summary>
    public class QueueDefinition
    {
        private int _maxDegreeOfParallelism;

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="name">             The name. </param>
        /// <param name="isDurable">        True if this object is durable, false if not. </param>
        public QueueDefinition(string name, bool isDurable, IEnumerable<string> bindings = null)
        {
            Name = name;
            IsDurable = isDurable;
            Bindings = bindings != null ? new List<string>(bindings) : new List<string>();
        }

        /// <summary>   Gets the name. </summary>
        ///
        /// <value> The name. </value>
        public string Name { get; }

        /// <summary>   Gets a value indicating whether this object is durable. </summary>
        ///
        /// <value> True if this object is durable, false if not. </value>
        public bool IsDurable { get; }

        /// <summary>   Gets the bindings. </summary>
        ///
        /// <value> The bindings. </value>
        public IList<string> Bindings { get; }

        /// <summary>   Gets the queue expire time in milliseconds. </summary>
        ///
        /// <value> The queue expire time. </value>
        public long QueueExpireTime => IsDurable ? 640800000 : 0;

        /// <summary>   Gets or sets the maximum degree of parallelism per instance. </summary>
        ///
        /// <value> The maximum degree of parallelism. </value>
        public int MaxDegreeOfParallelism
        {
            get => _maxDegreeOfParallelism > 0 ? _maxDegreeOfParallelism : 1;
            set => _maxDegreeOfParallelism = value;
        }
    }
}
