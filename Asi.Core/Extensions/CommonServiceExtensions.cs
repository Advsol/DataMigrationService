using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.Core.Extensions
{
    /// <summary>   Class CommonServiceExtensions. </summary>
    public static class CommonServiceExtensions
    {
        #region Public Static Methods

        /// <summary>
        /// An ICommonReadOnlyServiceAsync&lt;TDataContract&gt; extension method that exists asynchronous.
        /// </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <typeparam name="TDataContract">    Type of the data contract. </typeparam>
        /// <param name="service">  The service. </param>
        /// <param name="query">    The query. </param>
        ///
        /// <returns>   An asynchronous result that yields an IServiceResponse&lt;bool&gt; </returns>
        public static async Task<IServiceResponse<bool>> ExistsAsync<TDataContract>(this ICommonReadOnlyServiceAsync<TDataContract> service, IQuery<TDataContract> query)
            where TDataContract : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var response = await service.FindAsync(query).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return new ServiceResponse<bool>(response);
            return new ServiceResponse<bool> { Result = response.Result.Count > 0 };
        }

        /// <summary>
        /// An ICommonReadOnlyServiceAsync&lt;TDataContract&gt; extension method that exists asynchronous.
        /// </summary>
        ///
        /// <typeparam name="TDataContract">    Type of the data contract. </typeparam>
        /// <param name="service">  The service. </param>
        /// <param name="criteria"> The criteria. </param>
        ///
        /// <returns>   An asynchronous result that yields an IServiceResponse&lt;bool&gt; </returns>
        public static Task<IServiceResponse<bool>> ExistsAsync<TDataContract>(this ICommonReadOnlyServiceAsync<TDataContract> service, params CriteriaData[] criteria)
            where TDataContract : class
        {
            var query = new Query<TDataContract>();
            if (criteria != null)
                foreach (var criterion in criteria)
                    query.Filters.Add(criterion);
            return ExistsAsync(service, query);
        }

        /// <summary>
        /// An ICommonReadOnlyServiceAsync&lt;TDataContract&gt; extension method that exists asynchronous.
        /// </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="service">  The service. </param>
        /// <param name="query">    The query. </param>
        ///
        /// <returns>   An asynchronous result that yields an IServiceResponse&lt;bool&gt; </returns>
        public static async Task<IServiceResponse<bool>> ExistsAsync(this ICommonReadOnlyServiceAsync service, IQuery query)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var response = await service.FindAsync(query.Offset(0).Limit(1)).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return new ServiceResponse<bool>(response);
            return new ServiceResponse<bool> { Result = response.Result.Count > 0 };
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static async Task<IServiceResponse<IList<TDataContract>>> FindAllAsync<TDataContract>(this ICommonReadOnlyServiceAsync<TDataContract> service) where TDataContract : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var collection = new Collection<TDataContract>();
            var query = new Query<TDataContract>().Limit(500);
            do
            {
                var response = await service.FindAsync(query).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new ServiceResponse<IList<TDataContract>>(response);
                foreach (var result in response.Result) collection.Add(result);
                if (response.Result.NextOffset == 0) break;
                query.Offset = response.Result.NextOffset;
                query.Limit = response.Result.Limit;
            } while (true);

            return new ServiceResponse<IList<TDataContract>> { Result = collection };
        }

        /// <summary>   find all as an asynchronous operation. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <typeparam name="TDataContract">    The type of the t data contract. </typeparam>
        /// <param name="service">  The service. </param>
        /// <param name="query">    The query. </param>
        ///
        /// <returns>   Task&lt;IList&lt;TDataContract&gt;&gt;. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static async Task<IServiceResponse<IList<TDataContract>>> FindAllAsync<TDataContract>(this ICommonReadOnlyServiceAsync<TDataContract> service, IQuery<TDataContract> query)
            where TDataContract : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var collection = new Collection<TDataContract>();
            do
            {
                var response = await service.FindAsync(query).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new ServiceResponse<IList<TDataContract>>(response);
                foreach (var result in response.Result) collection.Add(result);
                if (response.Result.NextOffset == 0) break;
                query.Offset = response.Result.NextOffset;
                query.Limit = response.Result.Limit;
            } while (true);

            return new ServiceResponse<IList<TDataContract>> { Result = collection };
        }

        /// <summary>   Finds all asynchronous. </summary>
        ///
        /// <typeparam name="TDataContract">    The type of the t data contract. </typeparam>
        /// <param name="service">  The service. </param>
        /// <param name="criteria"> The criteria. </param>
        ///
        /// <returns>   Task&lt;IServiceResponse&lt;IList&lt;TDataContract&gt;&gt;&gt;. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static Task<IServiceResponse<IList<TDataContract>>> FindAllAsync<TDataContract>(this ICommonReadOnlyServiceAsync<TDataContract> service, params CriteriaData[] criteria)
            where TDataContract : class
        {
            var query = new Query<TDataContract>();
            if (criteria != null)
                foreach (var criterion in criteria)
                    query.Filters.Add(criterion);
            return FindAllAsync(service, query);
        }

        /// <summary>   find all as an asynchronous operation. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="service">  The service. </param>
        ///
        /// <returns>   Task&lt;IList&lt;TDataContract&gt;&gt;. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static async Task<IServiceResponse<IList>> FindAllAsync(this ICommonReadOnlyServiceAsync service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var collection = new List<object>();
            var query = new Query<object>().Limit(500);
            do
            {
                var response = await service.FindAsync(query).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new ServiceResponse<IList>(response);
                foreach (var result in response.Result) collection.Add(result);
                if (response.Result.NextOffset == 0) break;
                query.Offset = response.Result.NextOffset;
                query.Limit = response.Result.Limit;
            } while (true);

            return new ServiceResponse<IList> { Result = collection };
        }

        /// <summary>   find all as an asynchronous operation. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="service">  The service. </param>
        /// <param name="query">    The query. </param>
        ///
        /// <returns>   Task&lt;IList&lt;TDataContract&gt;&gt;. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static async Task<IServiceResponse<IList>> FindAllAsync(this ICommonReadOnlyServiceAsync service, IQuery query)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var collection = new List<object>();
            do
            {
                var response = await service.FindAsync(query).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new ServiceResponse<IList>(response);
                collection.AddRange(response.Result.Cast<object>());
                if (response.Result.NextOffset == 0) break;
                query.Offset = response.Result.NextOffset;
                query.Limit = response.Result.Limit;
            } while (true);

            return new ServiceResponse<IList> { Result = collection };
        }

        /// <summary>   Finds all asynchronous. </summary>
        ///
        /// <param name="service">  The service. </param>
        /// <param name="criteria"> The criteria. </param>
        ///
        /// <returns>   Task&lt;IServiceResponse&lt;IList&lt;TDataContract&gt;&gt;&gt;. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static Task<IServiceResponse<IList>> FindAllAsync(this ICommonReadOnlyServiceAsync service, params CriteriaData[] criteria)
        {
            var query = new Query<object>();
            if (criteria != null)
                foreach (var criterion in criteria)
                    query.Filters.Add(criterion);
            return FindAllAsync(service, query);
        }

        /// <summary>   Find single. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <typeparam name="TDataContract">    The type of the t data contract. </typeparam>
        /// <param name="service">  The service. </param>
        /// <param name="query">    The query. </param>
        ///
        /// <returns>   TDataContract. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static async Task<IServiceResponse<TDataContract>> FindSingleAsync<TDataContract>(this ICommonReadOnlyServiceAsync<TDataContract> service, IQuery<TDataContract> query)
            where TDataContract : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (query == null) throw new ArgumentNullException(nameof(query));
            var response = await service.FindAsync(query).ConfigureAwait(false);
            var response2 = new ServiceResponse<TDataContract>
            {
                Exception = response.Exception,
                Message = response.Message,
                ReasonPhrase = response.ReasonPhrase,
                StatusCode = response.StatusCode,
                ValidationResults = response.ValidationResults
            };
            if (response2.IsSuccessStatusCode)
                if (response.Result.Count > 0)
                    response2.Result = response.Result[0];
            return response2;
        }

        /// <summary>   Finds single asynchronous. </summary>
        ///
        /// <typeparam name="TDataContract">    The type of the t data contract. </typeparam>
        /// <param name="service">  The service. </param>
        /// <param name="criteria"> The criteria. </param>
        ///
        /// <returns>   Task&lt;IServiceResponse&lt;TDataContract&gt;&gt;. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static Task<IServiceResponse<TDataContract>> FindSingleAsync<TDataContract>(this ICommonReadOnlyServiceAsync<TDataContract> service, params CriteriaData[] criteria)
            where TDataContract : class
        {
            var query = new Query<TDataContract>();
            if (criteria != null)
                foreach (var criterion in criteria)
                    query.Filters.Add(criterion);
            return FindSingleAsync(service, query);
        }

        #endregion
    }
}