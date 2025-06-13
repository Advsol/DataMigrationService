using System.Diagnostics.CodeAnalysis;
using Asi.Soa.Core.DataContracts;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Asi.DataMigrationService.Lib.Data
{
    /// <summary>   A short unique identifier value generator. </summary>
    public class ShortGuidValueGenerator : ValueGenerator
    {
        /// <summary>
        /// <para>
        ///                 Gets a value indicating whether the values generated are temporary (i.e they
        ///                 should be replaced by database generated values when the entity is saved) or
        ///                 are permanent (i.e. the generated values should be saved to the database).
        ///             </para>
        /// <para>
        ///                 An example of temporary value generation is generating negative numbers for
        ///                 an integer primary key that are then replaced by positive numbers generated
        ///                 by the database when the entity is saved. An example of permanent value
        ///                 generation are client-generated values for a <see cref="T:System.Guid" />
        ///                 primary key which are saved to the database.
        ///             </para>
        /// </summary>
        ///
        /// <value> True if generates temporary values, false if not. </value>
        public override bool GeneratesTemporaryValues => false;

        /// <summary>
        /// Template method to be overridden by implementations to perform value generation.
        /// </summary>
        ///
        /// <param name="entry">    The entry. </param>
        ///
        /// <returns>   The generated value. </returns>
        protected override object NextValue([NotNull] EntityEntry entry)
        {
            return ShortGuid.NewGuid().ToString();
        }
    }
}
