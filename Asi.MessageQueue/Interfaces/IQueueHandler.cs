using System;

namespace Asi.DataMigrationService.MessageQueue.Interfaces
{
    /// <summary>   Interface for queue handler. </summary>
    public interface IQueueHandler : IDisposable
    {
        /// <summary>   Gets the name of the queue. </summary>
        ///
        /// <value> The name of the queue. </value>
        string QueueName { get; }
        /// <summary>   Starts this object. </summary>
        void Start();
        /// <summary>   Stops this object. </summary>
        void Stop();
    }
}
