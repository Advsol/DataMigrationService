using System;
using System.Net.Http.Formatting;
using Newtonsoft.Json;

namespace Asi.DataMigrationService.Core
{
    public static class GlobalSettings
    {
        public static TimeSpan MaximumReplyWaitTime = new TimeSpan(0, 5, 0);

        /// <summary>
        /// The serializer settings
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
        /// <summary>
        /// The clean serializer settings
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly JsonSerializerSettings CleanJsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None
        };
        /// <summary>
        /// The formatter
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly MediaTypeFormatter JsonMediaTypeFormatter = new JsonMediaTypeFormatter { SerializerSettings = JsonSerializerSettings, };
        /// <summary>
        /// The formatters
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public static readonly MediaTypeFormatter[] MediaTypeFormatters = { JsonMediaTypeFormatter };

        /// <summary>
        /// Indicates if SSL is required
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static bool RequireSSL => false;
    }
}
