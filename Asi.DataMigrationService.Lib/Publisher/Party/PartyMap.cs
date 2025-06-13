using Asi.DataMigrationService.Lib.Publisher.DataSource;

namespace Asi.DataMigrationService.Lib.Publisher.Party
{
    /// <summary>   Map of parties. </summary>
    public class PartyMap
    {
        #region Constructors

        /// <summary>   Default constructor. </summary>
        public PartyMap()
        {
            //RelatedAddress = new Collection<WorksheetRowReference>();
        }

        #endregion

        #region Properties

        /// <summary>   Gets or sets the depth. </summary>
        ///
        /// <value> The depth. </value>
        public int Depth { get; set; }

        /// <summary>   Gets or sets the identifier. </summary>
        ///
        /// <value> The identifier. </value>
        public string SourceId { get; set; }

        /// <summary>   Gets or sets a value indicating whether this object is organization. </summary>
        ///
        /// <value> True if this object is organization, false if not. </value>
        public bool IsOrganization { get; set; }

        /// <summary>   Gets or sets the identifier of the party. </summary>
        ///
        /// <value> The identifier of the party. </value>
        public string PartyId { get; set; }

        /// <summary>   Gets or sets the identifier of the related. </summary>
        ///
        /// <value> The identifier of the related. </value>
        public string RelatedSourceId { get; set; }

        /// <summary>   Gets or sets a value indicating whether this object is club member. </summary>
        ///
        /// <value> True if this object is club member, false if not. </value>
        public bool IsClubMember { get; set; }

        /// <summary>   Gets or sets the data source row reference. </summary>
        ///
        /// <value> The data source row reference. </value>
        public ImportRowReference DataSourceRowReference { get; set; }
        #endregion
    }
}
