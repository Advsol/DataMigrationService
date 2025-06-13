using System.Diagnostics;
using System.Threading;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class GroupSuccess
    {
        public int ErrorCount = 0;

        public int SuccessCount = 0;

        public GroupSuccess()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
        }

        public string ElapsedTime => Stopwatch.Elapsed.ToString(@"d\.hh\:mm\:ss");
        public Stopwatch Stopwatch { get; }

        public void IncrementErrorCount(int count = 1)
        {
            Interlocked.Add(ref ErrorCount, count);
        }

        public void IncrementSuccessCount(int count = 1)
        {
            Interlocked.Add(ref SuccessCount, count);
        }
    }
}