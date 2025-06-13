using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class MemoryProcessMessageLogger
    {
        public IList<PublishMessage> Messages { get; } = new List<PublishMessage>();

        public Task LogMessageAsync(PublishMessage processingError)
        {
            Messages.Add(processingError);
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return Messages.Count > 0 ? string.Join(", ", Messages.Select(p => p.ToString())) : "No Errors";
        }

        public bool HasErrors => Messages.Count > 0;
    }
}
