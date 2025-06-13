using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Asi.DataMigrationService.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>   A string extension method that null trim. </summary>
        ///
        /// <param name="value">    The value to act on. </param>
        ///
        /// <returns>   A string. </returns>
        public static string NullTrim(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>   A string extension method that empty trim. </summary>
        ///
        /// <param name="value">    The value to act on. </param>
        ///
        /// <returns>   A string. </returns>
        public static string EmptyTrim(this string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        /// <summary>   A string extension method that truncates. </summary>
        ///
        /// <param name="value">        The value to act on. </param>
        /// <param name="maxLength">    The maximum length of the. </param>
        ///
        /// <returns>   A string. </returns>
        public static string Truncate(this string value, int maxLength)
        {
            return string.IsNullOrEmpty(value) ? value : value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static byte[] Compress(this string value)
        {
            using var output = new MemoryStream();
            using var gzip = new DeflateStream(output, CompressionMode.Compress);
            using var writer = new StreamWriter(gzip, Encoding.UTF8);
            writer.Write(value);
            return output.ToArray();
        }

        public static string Decompress(this byte[] value)
        {
            using var inputStream = new MemoryStream(value);
            using var gzip = new DeflateStream(inputStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        /// <summary>
        /// To unique identifier.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>Guid.</returns>
        public static Guid ToGuid(this object value)
        {
            if (value == null) return Guid.Empty;
            if (value is Guid guid1) return guid1;
            if (value is string stringValue)
                if (Guid.TryParse(stringValue, out var guid))
                    return guid;

            return Guid.Empty;
        }
        /// <summary>
        /// To nullable unique identifier.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>System.Nullable&lt;Guid&gt;.</returns>
        public static Guid? ToNullableGuid(this object value)
        {
            if (value == null) return null;
            if (value is Guid guid1) return guid1;
            if (value is string stringValue)
                if (Guid.TryParse(stringValue, out var guid))
                    return guid;

            return null;
        }

        /// <summary>   A string extension method that equals ordinal ignore case. </summary>
        ///
        /// <param name="value">        The value to act on. </param>
        /// <param name="compareValue"> The compare value. </param>
        ///
        /// <returns>   True if equals ordinal ignore case, false if not. </returns>
        public static bool EqualsOrdinalIgnoreCase(this string value, string compareValue)
        {
            return value.Equals(compareValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>   A string extension method that starts with ordinal ignore case. </summary>
        ///
        /// <param name="value">        The value to act on. </param>
        /// <param name="compareValue"> The compare value. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        public static bool StartsWithOrdinalIgnoreCase(this string value, string compareValue)
        {
            return value.StartsWith(compareValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// An IEnumerable&lt;string&gt; extension method that query if 'values' contains ordinal ignore
        /// case.
        /// </summary>
        ///
        /// <param name="values">       The values to act on. </param>
        /// <param name="compareValue"> The compare value. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        public static bool ContainsOrdinalIgnoreCase(this IEnumerable<string> values, string compareValue)
        {
            return values.Contains(compareValue, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>   Enumerates all values in this collection. </summary>
        ///
        /// <param name="col">  The col to act on. </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process all values in this collection.
        /// </returns>
        public static IEnumerable<string> AllValues (this NameValueCollection col)
        {
            return col.Cast<string>().Select(p => col[p]);
        }
    }
}
