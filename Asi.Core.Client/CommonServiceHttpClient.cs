using Asi.Core.Interfaces;
using Asi.DataMigrationService.Core.Extensions;
using Asi.Soa.Core.Attributes;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Asi.DataMigrationService.Core.Client
{
    /// <summary>
    /// Interface ICommonServiceHttpClient
    /// </summary>
    /// <seealso cref="Asi.Soa.Core.ServiceContracts.ICommonServiceAsync" />
    public interface ICommonServiceHttpClient : ICommonServiceAsync, ICommonService, IDisposable
    {
    }
    /// <summary>
    /// Class CommonServiceHttpClient.
    /// </summary>
    /// <seealso cref="Asi.Soa.Core.ServiceContracts.ICommonServiceAsync" />
    /// <seealso cref="System.IDisposable" />
    public class CommonServiceHttpClient : ICommonServiceHttpClient, IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// Gets or sets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected SecureHttpClient HttpClient { get; set; }

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonServiceHttpClient{TDataContract}" /> class.
        /// </summary>
        ///
        /// <param name="secureHttpClient"> The secure HTTP client. </param>
        /// <param name="entityTypeName">   Name of the entity type. </param>
        public CommonServiceHttpClient(SecureHttpClient secureHttpClient, string entityTypeName)
        {
            HttpClient = secureHttpClient;
            EntityTypeName = entityTypeName;
            AuthorizationContext = new CommonServiceWebClientAuthorizationContext(this);
            BaseUri = secureHttpClient.BaseUri;
        }

        #endregion

        #region ICommonServiceAsync Members

        /// <summary>
        /// Gets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequestContext RequestContext { get; } = new RequestContextData();

        /// <summary>
        /// Gets or sets the base URI.
        /// </summary>
        /// <value>The base URI.</value>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets the user credentials.
        /// </summary>
        /// <value>The user credentials.</value>
        protected IUserCredentials UserCredentials { get; set; }

        /// <summary>
        /// Gets or sets the name of the controller.
        /// </summary>
        /// <value>The name of the controller.</value>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// Adds the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>System.Object.</returns>
        public IServiceResponse Add(object dataContract)
        {
            return Task.Run(() => AddAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Adds the entity specified by the data contract.
        /// </summary>GlobalSettings.JsonSerializerSettings
        /// <param name="dataContract">The data contract.</param>
        /// <returns>System.Object.</returns>
        public async Task<IServiceResponse> AddAsync(object dataContract)
        {
            try
            {
                var response = await HttpClient.PostAsync(GetUri(), dataContract, GlobalSettings.JsonMediaTypeFormatter);
                var result = response.ToServiceResponse();
                if (result.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), GlobalSettings.JsonSerializerSettings);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Validations the error check.
        /// </summary>
        /// <param name="result">The result.</param>
        protected static void ValidationErrorCheck(IServiceResponse result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.StatusCode == StatusCode.ValidationError)
            {
                result.ValidationResults = JsonConvert.DeserializeObject<IValidationResults>(result.Message, GlobalSettings.JsonSerializerSettings);
                result.Result = null;
                result.Message = result.ValidationResults.Summary;
            }
        }

        /// <summary>
        /// Adds the specified data contracts.
        /// </summary>
        /// <param name="dataContracts">The data contracts.</param>
        /// <returns>IList.</returns>
        public IServiceResponse<IList> AddMultiple(IEnumerable dataContracts)
        {
            return Task.Run(() => AddMultipleAsync(dataContracts)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Adds the specified data contracts.
        /// </summary>
        /// <param name="dataContracts">The data contracts.</param>
        /// <returns>IList.</returns>
        public async Task<IServiceResponse<IList>> AddMultipleAsync(IEnumerable dataContracts)
        {
            if (dataContracts == null) throw new ArgumentNullException(nameof(dataContracts));
            try
            {
                var response = await HttpClient.PostAsync(GetUri("/_addMultiple"), dataContracts, GlobalSettings.JsonMediaTypeFormatter);
                var result = new ServiceResponse<IList>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<IList>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<IList>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Deletes the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        public IServiceResponse Delete(object dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            return Task.Run(() => DeleteAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        public async Task<IServiceResponse> DeleteAsync(object dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            var id = EntityAttribute.GetId(dataContract);
            if (id == null)
                return new ServiceResponse(StatusCode.BadRequest) { Message = "Requested entity has no Id." };
            try
            {
                var response = await HttpClient.DeleteAsync(GetUriForId(id));
                var result = response.ToServiceResponse();
                if (result.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), GlobalSettings.JsonSerializerSettings);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Call the persistence strategy of a particular entity in order
        /// to perform an extended operation.
        /// </summary>
        /// <param name="request">
        /// The data to be used to identify the entity that should
        /// be invoked, as well as any information pertinent to the
        /// desired processing.
        /// </param>
        /// <returns>Variable return results.</returns>
        public IServiceResponse Execute(ExecuteRequestBase request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return Task.Run(() => ExecuteAsync(request)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes the specified method name. Called by the generated proxy methods
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>System.Object.</returns>
        public object Execute(string methodName, params object[] arguments)
        {
            return Execute(new GenericExecuteRequest(EntityTypeName, methodName, arguments)).ErrorCheck().Result;
        }

        /// <summary>
        /// Call the persistence strategy of a particular entity in order
        /// to perform an extended operation.
        /// </summary>
        /// <param name="request">
        /// The data to be used to identify the entity that should
        /// be invoked, as well as any information pertinent to the
        /// desired processing.
        /// </param>
        /// <returns>Variable return results.</returns>
        public async Task<IServiceResponse> ExecuteAsync(ExecuteRequestBase request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            try
            {
                var response = await HttpClient.PostAsync(GetUri("/_execute"), request, GlobalSettings.JsonMediaTypeFormatter);
                var result = response.ToServiceResponse();
                if (result.IsSuccessStatusCode)
                {
                    var str = await response.Content.ReadAsStringAsync();
                    result.Result = JsonConvert.DeserializeObject(str, GlobalSettings.JsonSerializerSettings);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Executes the asynchronous.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>System.Object.</returns>
        public Task<IServiceResponse> ExecuteAsync(string methodName, params object[] arguments)
        {
            return ExecuteAsync(new GenericExecuteRequest(EntityTypeName, methodName, arguments));
        }

        /// <summary>   Executes asynchronous operation from CommonServiceHttpClientRegistrationSource generated method call proxies.</summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="methodName">   Name of the method. </param>
        /// <param name="arguments">    The arguments. </param>
        ///
        /// <returns>   An asynchronous result that yields the execute 2. </returns>
        public async Task<IServiceResponse<T>> Execute2Async<T>(string methodName, params object[] arguments)
        {
            var request = new GenericExecuteRequest(EntityTypeName, methodName, arguments);
            if (request == null) throw new ArgumentNullException(nameof(request));
            try
            {
                var response = await HttpClient.PostAsync(GetUri("/_execute"), request, GlobalSettings.JsonMediaTypeFormatter);
                var result = response.ToServiceResponse<T>();
                if (result.IsSuccessStatusCode)
                {
                    var str = await response.Content.ReadAsStringAsync();
                    result = (IServiceResponse<T>)JsonConvert.DeserializeObject(str, GlobalSettings.JsonSerializerSettings); ;
                    return result;
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                    return new ServiceResponse<T>(result);
                }
            }
            catch (Exception e)
            {
                return new ServiceResponse<T>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Finds with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>IPagedResult{`0}.</returns>
        public IServiceResponsePagedResult Find(string options)
        {
            return Task.Run(() => FindAsync(options)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finds with OData options and IPagesResult.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{IPagedResult{`0}}.</returns>
        public async Task<IServiceResponsePagedResult> FindAsync(string options)
        {
            try
            {
                var response = await HttpClient.GetAsync(options != null ? GetUri("?" + options) : GetUri());
                var result = new ServiceResponsePagedResult(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<IPagedResult>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponsePagedResult(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Determines whether [has permission asynchronous] [the specified action].
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>Task&lt;ServiceResponse&gt;.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public async Task<IServiceResponse<bool>> HasPermissionAsync(string action, object id, object entity)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            try
            {
                var uri = GetUri("/_haspermission/" + action);
                if (id != null) uri = new Uri(uri, "?Id=" + IdToEncodedString(id));
                var response = await HttpClient.PostAsync(uri, entity, GlobalSettings.JsonMediaTypeFormatter);
                var result = new ServiceResponse<bool>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync(), GlobalSettings.JsonSerializerSettings);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<bool>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Finds by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IPagedResult.</returns>
        public IServiceResponsePagedResult Find(IQuery query)
        {
            return Task.Run(() => FindAsync(query)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finds by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IServiceResponsePagedResult&gt;.</returns>
        public Task<IServiceResponsePagedResult> FindAsync(IQuery query)
        {
            string opts = null;
            if (query != null)
            {
                var queryOpts = query.ToQueryString();
                if (!string.IsNullOrWhiteSpace(queryOpts)) opts = queryOpts;
            }
            return FindAsync(opts);
        }

        /// <summary>
        /// Finds by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>`0.</returns>
        public IServiceResponse FindById(object id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return Task.Run(() => FindByIdAsync(id)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finds by id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;ServiceResponse&gt;.</returns>
        public async Task<IServiceResponse> FindByIdAsync(object id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            try
            {
                var response = await HttpClient.GetAsync(GetUriForId(id));
                var result = response.ToServiceResponse();
                if (result.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), GlobalSettings.JsonSerializerSettings);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Finds the change log by identifier.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>IPagedResult{ChangeLogData}.</returns>
        public IServiceResponsePagedResult<ChangeLogData> FindChangeLogById(int offset, int limit, object id)
        {
            return Task.Run(() => FindChangeLogByIdAsync(offset, limit, id)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finds the change log by identifier.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>IPagedResult{ChangeLogData}.</returns>
        public async Task<IServiceResponsePagedResult<ChangeLogData>> FindChangeLogByIdAsync(int offset, int limit, object id)
        {
            try
            {
                var response = await HttpClient.GetAsync(GetUri("/" + IdToEncodedString(id) + "/changelog?offset=" + offset + "&limit=" + limit));
                var result = new ServiceResponsePagedResult<ChangeLogData>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<IPagedResult<ChangeLogData>>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponsePagedResult<ChangeLogData>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <returns>EntityDefinitionData.</returns>
        public IServiceResponse<EntityDefinitionData> GetMetadata()
        {
            return Task.Run(() => GetMetadataAsync()).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <returns>EntityDefinitionData.</returns>
        public async Task<IServiceResponse<EntityDefinitionData>> GetMetadataAsync()
        {
            try
            {
                var response = await HttpClient.GetAsync(new Uri(BaseUri, "api/metadata/" + EntityTypeName));
                var result = new ServiceResponse<EntityDefinitionData>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<EntityDefinitionData>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<EntityDefinitionData>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Updates the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>System.Object.</returns>
        public IServiceResponse Update(object dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            return Task.Run(() => UpdateAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>System.Object.</returns>
        public async Task<IServiceResponse> UpdateAsync(object dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            var id = EntityAttribute.GetId(dataContract);

            try
            {
                var response = await HttpClient.PutAsync(GetUriForId(id), dataContract, GlobalSettings.JsonMediaTypeFormatter);
                var result = response.ToServiceResponse();
                if (result.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), GlobalSettings.JsonSerializerSettings);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Validates the specified data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>ValidateResultsData{`0}.</returns>
        public IServiceResponse Validate(object dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            return Task.Run(() => ValidateAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates the specified data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>ValidateResultsData{`0}.</returns>
        public async Task<IServiceResponse> ValidateAsync(object dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));

            try
            {
                var response = await HttpClient.PostAsync(GetUri("/_validate"), dataContract, GlobalSettings.JsonMediaTypeFormatter);
                var result = response.ToServiceResponse();
                if (result.IsSuccessStatusCode)
                {
                    var validateResults = await response.Content.ReadAsAsync<IValidateResults>(GlobalSettings.MediaTypeFormatters);
                    result.ValidationResults = validateResults.ValidationResults;
                    result.Result = validateResults.Entity;
                    result.Message = validateResults.ValidationResults.Summary;
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Gets the authorization context.
        /// </summary>
        /// <value>The authorization context.</value>
        public IAuthorizationContext AuthorizationContext { get; }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        /// unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (HttpClient != null)
                {
                    HttpClient.Dispose();
                    HttpClient = null;
                }
            }
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <returns>Uri.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
        public Uri GetUri()
        {
            return new Uri(BaseUri, "api/" + EntityTypeName);
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <param name="relative">The relative url.</param>
        /// <returns>Uri.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
        public Uri GetUri(string relative)
        {
            return new Uri(BaseUri, "api/" + EntityTypeName + relative);
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <param name="id">The relative url.</param>
        /// <returns>Uri.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
        public Uri GetUriForId(object id)
        {
            var stringId = new Id(id).ToString();
            var encodedId = IdToEncodedString(id);
            return stringId.Equals(encodedId, StringComparison.Ordinal)
                ? new Uri(BaseUri, "api/" + EntityTypeName + "/" + stringId)
                : new Uri(BaseUri, "api/" + EntityTypeName + "?EntityId=" + encodedId);
        }

        /// <summary>
        /// Identifiers to encoded string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>System.String.</returns>
        protected static string IdToEncodedString(object id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return HttpUtility.UrlEncode(new Id(id).ToString());
        }
    }

    /// <summary>   Class CommonServiceHttpClient. </summary>
    ///
    /// <typeparam name="TDataContract">    The type of the t data contract. </typeparam>
    ///
    /// <seealso cref="CommonServiceHttpClient"/>
    /// <seealso cref="Asi.Soa.Core.ServiceContracts.ICommonServiceAsync{TDataContract}"/>
    public class CommonServiceHttpClient<TDataContract> : CommonServiceHttpClient, ICommonServiceSyncAsync<TDataContract> where TDataContract : class
    {
        #region ICommonServiceAsync<TDataContract> Members

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonServiceHttpClient" /> class.
        /// </summary>
        ///
        /// <param name="secureHttpClient"> The user credentials. </param>
        public CommonServiceHttpClient(SecureHttpClient secureHttpClient)
            : base(secureHttpClient, GetEntityTypeName())
        {
        }

        private static string GetEntityTypeName()
        {
            var type = typeof(TDataContract);
            var name = EntityAttribute.GetEntityTypeName(type);
            if (string.IsNullOrWhiteSpace(name)) name = type.Name;
            return name.EndsWith("Data", StringComparison.Ordinal) ? name.Substring(0, name.Length - 4) : name;
        }

        /// <summary>
        /// Adds the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>TDataContract.</returns>
        public IServiceResponse<TDataContract> Add(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            return Task.Run(() => AddAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Adds the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>TDataContract.</returns>
        public async Task<IServiceResponse<TDataContract>> AddAsync(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            try
            {
                var response = await HttpClient.PostAsync(GetUri(), dataContract, GlobalSettings.JsonMediaTypeFormatter);
                var result = new ServiceResponse<TDataContract>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<TDataContract>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<TDataContract>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Deletes the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        [InvalidateCache(Item = "#dataContract")]
        public IServiceResponse Delete(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            return Task.Run(() => DeleteAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        public async Task<IServiceResponse> DeleteAsync(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            try
            {
                var id = EntityAttribute.GetId(dataContract);
                var response = await HttpClient.DeleteAsync(GetUriForId(id));
                var result = new ServiceResponse<TDataContract>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject<TDataContract>(await response.Content.ReadAsStringAsync(), GlobalSettings.JsonSerializerSettings);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Finds the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>IPagedResult{`0}.</returns>
        public new IServiceResponsePagedResult<TDataContract> Find(string options)
        {
            return Task.Run(() => FindAsync(options)).GetAwaiter().GetResult();
        }

        /// <summary>   Searches for the asynchronous. </summary>
        ///
        /// <param name="options">  The options. </param>
        ///
        /// <returns>   Task{IPagedResult{`0}}. </returns>
        ///
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        private new async Task<IServiceResponsePagedResult<TDataContract>> FindAsync(string options)
        {
            try
            {
                var response = await HttpClient.GetAsync(options != null ? GetUri("?" + options) : GetUri());

                var result = new ServiceResponsePagedResult<TDataContract>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<IPagedResult<TDataContract>>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponsePagedResult<TDataContract>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        private async Task<IServiceResponsePagedResult<TDataContract>> FindByQueryAsync(IQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            try
            {
                var x = JsonConvert.SerializeObject(query, GlobalSettings.JsonSerializerSettings);
                var y = new StringContent(x, UnicodeEncoding.UTF8, "application/json");

                //HttpResponseMessage response = await HttpClient.PostAsync(GetUri("/_query"), query, GlobalSettings.JsonMediaTypeFormatter);
                var response = await HttpClient.PostAsync(GetUri("/_query"), y);

                var result = new ServiceResponsePagedResult<TDataContract>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<IPagedResult<TDataContract>>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponsePagedResult<TDataContract>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Finds by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>PagedResult{`0}.</returns>
        public IServiceResponsePagedResult<TDataContract> Find(IQuery<TDataContract> query)
        {
            return Task.Run(() => FindAsync(query)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finds by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>PagedResult{`0}.</returns>
        public Task<IServiceResponsePagedResult<TDataContract>> FindAsync(IQuery<TDataContract> query)
        {
            string opts = null;
            if (query != null)
            {
                if (query.Condition != null)
                {
                    return FindByQueryAsync(query);
                }
                var queryOpts = query.ToQueryString();
                if (!string.IsNullOrWhiteSpace(queryOpts)) opts = queryOpts;
            }
            return FindAsync(opts);
        }

        /// <summary>
        /// Finds by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>`0.</returns>
        public new IServiceResponse<TDataContract> FindById(object id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return Task.Run(() => FindByIdAsync(id)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finds by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>`0.</returns>
        public new async Task<IServiceResponse<TDataContract>> FindByIdAsync(object id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            try
            {
                var response = await HttpClient.GetAsync(GetUriForId(id));
                var result = new ServiceResponse<TDataContract>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<TDataContract>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<TDataContract>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Updates the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>TDataContract.</returns>
        public IServiceResponse<TDataContract> Update(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            return Task.Run(() => UpdateAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the entity specified by the data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>TDataContract.</returns>
        public async Task<IServiceResponse<TDataContract>> UpdateAsync(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            try
            {
                var id = EntityAttribute.GetId(dataContract);
                var response = await HttpClient.PutAsync(GetUriForId(id), dataContract, GlobalSettings.JsonMediaTypeFormatter);
                var result = new ServiceResponse<TDataContract>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<TDataContract>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<TDataContract>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>
        /// Validates the specified data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>ValidateResultsData{`0}.</returns>
        public IServiceResponse<TDataContract> Validate(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            return Task.Run(() => ValidateAsync(dataContract)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates the specified data contract.
        /// </summary>
        /// <param name="dataContract">The data contract.</param>
        /// <returns>ValidateResultsData{`0}.</returns>
        public async Task<IServiceResponse<TDataContract>> ValidateAsync(TDataContract dataContract)
        {
            if (dataContract == null) throw new ArgumentNullException(nameof(dataContract));
            try
            {
                var response = await HttpClient.PostAsync(GetUri("/_validate"), dataContract, GlobalSettings.JsonMediaTypeFormatter);
                var result = new ServiceResponse<TDataContract>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    var validateResults = await response.Content.ReadAsAsync<IValidateResults>(GlobalSettings.MediaTypeFormatters);
                    result.ValidationResults = validateResults.ValidationResults;
                    result.Result = validateResults.Entity as TDataContract;
                    result.Message = validateResults.ValidationResults.Summary;
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<TDataContract>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>   Bulk insert asynchronous. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="dataContracts">    The data contracts. </param>
        ///
        /// <returns>   An asynchronous result that yields the bulk insert. </returns>
        public async Task<IServiceResponse<IList<object>>> BulkInsertAsync(IEnumerable<TDataContract> dataContracts)
        {
            if (dataContracts == null) throw new ArgumentNullException(nameof(dataContracts));
            try
            {
                var response = await HttpClient.PostAsync(GetUri("/_bulkInsert"), dataContracts, GlobalSettings.JsonMediaTypeFormatter);
                var result = new ServiceResponse<IList<object>>(response.ToServiceResponse());
                if (result.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsAsync<IList<object>>(GlobalSettings.MediaTypeFormatters);
                }
                else
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse<IList<object>>(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        /// <summary>   Bulk update asynchronous. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="dataContracts">    The data contracts. </param>
        ///
        /// <returns>   An asynchronous result that yields the bulk update. </returns>
        public async Task<IServiceResponse> BulkUpdateAsync(IEnumerable<TDataContract> dataContracts)
        {
            if (dataContracts == null) throw new ArgumentNullException(nameof(dataContracts));
            try
            {
                var response = await HttpClient.PostAsync(GetUri("/_bulkUpdate"), dataContracts, GlobalSettings.JsonMediaTypeFormatter);
                var result = new ServiceResponse(response.ToServiceResponse());
                if (!result.IsSuccessStatusCode)
                {
                    result.Message = await response.Content.ReadAsStringAsync();
                    ValidationErrorCheck(result);
                }
                return result;
            }
            catch (Exception e)
            {
                return new ServiceResponse(StatusCode.ServiceError) { Message = e.Message, Exception = e };
            }
        }

        #endregion
    }
    /// <summary>
    /// Class Authorization.
    /// </summary>
    public class CommonServiceWebClientAuthorizationContext : IAuthorizationContext
    {
        private readonly CommonServiceHttpClient _service;

        internal CommonServiceWebClientAuthorizationContext(CommonServiceHttpClient service)
        {
            _service = service;
        }

        #region IAuthorizationContext Members

        /// <summary>
        /// Determines whether this instance can read.
        /// </summary>
        /// <returns><c>true</c> if this instance can read; otherwise, <c>false</c>.</returns>
        public bool CanRead()
        {
            return IsAuthorized(AuthorizationAction.Read);
        }

        /// <summary>
        /// Determines whether this instance [can read for identifier] the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns><c>true</c> if this instance [can read for identifier] the specified identifier; otherwise, <c>false</c>.</returns>
        public bool CanReadForId(object id)
        {
            return IsAuthorizedForId(AuthorizationAction.Read, id);
        }

        /// <summary>
        /// Determines whether this instance [can read for object] the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if this instance [can read for object] the specified value; otherwise, <c>false</c>.</returns>
        public bool CanReadForObject(object value)
        {
            return IsAuthorizedForObject(AuthorizationAction.Read, value);
        }

        /// <summary>
        /// Determines whether this instance can create.
        /// </summary>
        /// <returns><c>true</c> if this instance can create; otherwise, <c>false</c>.</returns>
        public bool CanCreate()
        {
            return IsAuthorized(AuthorizationAction.Create);
        }

        /// <summary>
        /// Determines whether this instance can create the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if this instance can create the specified value; otherwise, <c>false</c>.</returns>
        public bool CanCreate(object value)
        {
            return IsAuthorizedForObject(AuthorizationAction.Create, value);
        }

        /// <summary>
        /// Determines whether this instance can update.
        /// </summary>
        /// <returns><c>true</c> if this instance can update; otherwise, <c>false</c>.</returns>
        public bool CanUpdate()
        {
            return IsAuthorized(AuthorizationAction.Update);
        }

        /// <summary>
        /// Determines whether this instance can update the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if this instance can update the specified value; otherwise, <c>false</c>.</returns>
        public bool CanUpdate(object value)
        {
            return IsAuthorizedForObject(AuthorizationAction.Update, value);
        }

        /// <summary>
        /// Determines whether this instance [can update for identifier] the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns><c>true</c> if this instance [can update for identifier] the specified identifier; otherwise, <c>false</c>.</returns>
        public bool CanUpdateForId(object id)
        {
            return IsAuthorizedForId(AuthorizationAction.Update, id);
        }

        /// <summary>
        /// Determines whether this instance can delete.
        /// </summary>
        /// <returns><c>true</c> if this instance can delete; otherwise, <c>false</c>.</returns>
        public bool CanDelete()
        {
            return IsAuthorized(AuthorizationAction.Delete);
        }

        /// <summary>
        /// Determines whether this instance can delete the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if this instance can delete the specified value; otherwise, <c>false</c>.</returns>
        public bool CanDelete(object value)
        {
            return IsAuthorizedForObject(AuthorizationAction.Delete, value);
        }

        /// <summary>
        /// Determines whether this instance [can delete for identifier] the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns><c>true</c> if this instance [can delete for identifier] the specified identifier; otherwise, <c>false</c>.</returns>
        public bool CanDeleteForId(object id)
        {
            return IsAuthorizedForId(AuthorizationAction.Delete, id);
        }

        /// <summary>
        /// Determines whether this instance can execute.
        /// </summary>
        /// <returns><c>true</c> if this instance can execute; otherwise, <c>false</c>.</returns>
        public bool CanExecute()
        {
            return IsAuthorized(AuthorizationAction.Execute);
        }

        /// <summary>
        /// Determines whether this instance [can read field] the specified field name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns><c>true</c> if this instance [can read field] the specified field name; otherwise, <c>false</c>.</returns>
        public bool CanReadField(string fieldName)
        {
            return true;
        }

        /// <summary>
        /// Determines whether this instance [can update field] the specified field name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns><c>true</c> if this instance [can update field] the specified field name; otherwise, <c>false</c>.</returns>
        public bool CanUpdateField(string fieldName)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the specified action is authorized.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns><c>true</c> if the specified action is authorized; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">action</exception>
        public bool IsAuthorized(string action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return IsAuthorizedForId(action, null);
        }

        /// <summary>
        /// Determines whether [is authorized for identifier] [the specified action].
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="id">The identifier.</param>
        /// <returns><c>true</c> if [is authorized for identifier] [the specified action]; otherwise, <c>false</c>.</returns>
        public bool IsAuthorizedForId(string action, object id)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var response = _service.HasPermissionAsync(action, id, null).Result;
            return response.IsSuccessStatusCode ? response.Result : false;
        }

        /// <summary>
        /// Determines whether [is authorized for object] [the specified action].
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if [is authorized for object] [the specified action]; otherwise, <c>false</c>.</returns>
        public bool IsAuthorizedForObject(string action, object value)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (value == null) throw new ArgumentNullException(nameof(value));
            var response = _service.HasPermissionAsync(action, null, value).Result;
            return response.IsSuccessStatusCode ? response.Result : false;
        }

        public Task<PermissionResultCollection> GetPermissions(PermissionDataCollection permissions)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
