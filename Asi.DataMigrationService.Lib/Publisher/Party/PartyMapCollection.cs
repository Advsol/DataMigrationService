using System;
using System.Collections.Concurrent;

namespace Asi.DataMigrationService.Lib.Publisher.Party
{
    /// <summary>   Class PartyMapCollection. </summary>
    public class PartyMapCollection : ConcurrentDictionary<string, PartyMap>
    {
        #region Methods

        /// <summary>   Default constructor. </summary>
        public PartyMapCollection() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        #endregion
    }
}
