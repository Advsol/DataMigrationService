using Asi.Core.Interfaces;
using IdentityModel.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.FormattableString;

namespace Asi.DataMigrationService.Core.Client
{
    public interface ISecureHttpClientFactory
    {
        SecureHttpClient Create(Uri baseUri, IUserCredentials userCredentials);
        SecureHttpClient Create(Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers);
        SecureHttpClient Create(Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers, ServiceApiOption serviceApiOption);
    }

    public class SecureHttpClientFactory : ISecureHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SecureHttpClient> _logger;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public SecureHttpClientFactory(IHttpClientFactory httpClientFactory, ILogger<SecureHttpClient> logger, IDataProtectionProvider dataProtectionProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dataProtectionProvider = dataProtectionProvider;
        }

        public SecureHttpClient Create(Uri baseUri, IUserCredentials userCredentials)
        {
            return new SecureHttpClient(baseUri, userCredentials, null, ServiceApiOption.None, _httpClientFactory, _logger, _dataProtectionProvider);
        }

        public SecureHttpClient Create(Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers)
        {
            return new SecureHttpClient(baseUri, userCredentials, headers, ServiceApiOption.None, _httpClientFactory, _logger, _dataProtectionProvider);
        }

        public SecureHttpClient Create(Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers, ServiceApiOption serviceApiOption)
        {
            return new SecureHttpClient(baseUri, userCredentials, headers, serviceApiOption, _httpClientFactory, _logger, _dataProtectionProvider);
        }
    }

    /// <summary>
    /// Used to specify options when invoked by ServiceApiProxyController
    /// </summary>
    public enum ServiceApiOption
    {
        /// <summary>
        /// Default
        /// </summary>
        None = 0,
        /// <summary>
        /// Don't attempt to get a Token.  Headers are passed through.
        /// </summary>
        PassThroughOnly = 1,
        /// <summary>
        /// RequestVerificationToken is used to authenticate APi request.
        /// </summary>
        UsesRequestVerificationToken = 2
    }
    /// <summary>
    /// Class SecureHttpClient.
    /// </summary>
    public class SecureHttpClient : IDisposable
    {
        private readonly ServiceApiOption? _serviceApiOption;

        // A list of headers we are not going to send through to the REST service
        // We need to review the HTTP_X_FORWARDED_FOR exclude, it caused a gift entry bug, but i think we should be sending it through.
        private readonly string[] _ignoreHeaders = { "requestverificationtoken", "cookie", "Referer", "upgrade-insecure-requests", "Host", "HTTP_X_FORWARDED_FOR" };

        // Headers received with the original request
        private readonly HttpRequestHeaders _passThrougHeaders;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SecureHttpClient> _logger;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        /// <summary>
        /// The HTTP client
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private HttpClient HttpClient
        {
            get
            {
                var httpClient = _httpClientFactory.CreateClient("SecureHttpClient");
                httpClient.Timeout = new TimeSpan(0, 5, 0);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                return httpClient;
            }
        }

        private bool PassThroughOnly
        {
            get { return _serviceApiOption != null && _serviceApiOption.Value.Equals(ServiceApiOption.PassThroughOnly); }
        }

        private bool UsesRequestVerificationToken
        {
            get { return _serviceApiOption != null && _serviceApiOption.Value.Equals(ServiceApiOption.UsesRequestVerificationToken); }
        }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        /// <value>The credentials.</value>
        public IUserCredentials UserCredentials { get; }

        public Uri BaseUri { get; }

        /// <summary>   Initializes a new instance of the <see cref="SecureHttpClient" /> class. </summary>
        ///
        /// <param name="baseUri">                  The base URI. </param>
        /// <param name="userCredentials">          The credentials. </param>
        /// <param name="headers">                  pass though headers. </param>
        /// <param name="serviceApiOption">         . </param>
        /// <param name="httpClientFactory">        The HTTP client factory. </param>
        /// <param name="logger">                   The logger. </param>
        /// <param name="dataProtectionProvider">   The data protection provider. </param>
        public SecureHttpClient(Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers, ServiceApiOption serviceApiOption,
            IHttpClientFactory httpClientFactory, ILogger<SecureHttpClient> logger, IDataProtectionProvider dataProtectionProvider)
        {
            BaseUri = baseUri;
            UserCredentials = userCredentials;
            _passThrougHeaders = headers;
            _serviceApiOption = serviceApiOption;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dataProtectionProvider = dataProtectionProvider;
        }

        /// <summary>
        /// get as an asynchronous operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        public Task<HttpResponseMessage> GetAsync(Uri uri)
        {
            return SecureExecute(() => SendRequestAsync(HttpMethod.Get, uri));
        }

        /// <summary>
        /// post as an asynchronous operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="httpContent">Content of the HTTP.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        public Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent httpContent)
        {
            return SecureExecute(() => SendRequestAsync(HttpMethod.Post, uri, httpContent));
        }

        /// <summary>
        /// post as an asynchronous operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="dataContract">The data contract.</param>
        /// <param name="formatter">The formatter.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public Task<HttpResponseMessage> PostAsync(Uri uri, object dataContract, MediaTypeFormatter formatter)
        {
            return SecureExecute(() => SendRequestAsync(HttpMethod.Post, uri, new ObjectContent<object>(dataContract, formatter, (MediaTypeHeaderValue)null)));
        }

        /// <summary>
        /// delete as an asynchronous operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        public Task<HttpResponseMessage> DeleteAsync(Uri uri)
        {
            return SecureExecute(() => SendRequestAsync(HttpMethod.Delete, uri));
        }

        /// <summary>
        /// put as an asynchronous operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="dataContract">The data contract.</param>
        /// <param name="formatter">The formatter.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public Task<HttpResponseMessage> PutAsync(Uri uri, object dataContract, MediaTypeFormatter formatter)
        {
            return SecureExecute(() => SendRequestAsync(HttpMethod.Put, uri, new ObjectContent<object>(dataContract, formatter, (MediaTypeHeaderValue)null)));
        }

        /// <summary>   put as an asynchronous operation. </summary>
        ///
        /// <param name="uri">          The URI. </param>
        /// <param name="httpContent">  Content of the HTTP. </param>
        ///
        /// <returns>   Task&lt;HttpResponseMessage&gt;. </returns>
        public Task<HttpResponseMessage> PutAsync(Uri uri, HttpContent httpContent)
        {
            return SecureExecute(() => SendRequestAsync(HttpMethod.Put, uri, httpContent));
        }

        /// <summary>   Patch asynchronous. </summary>
        ///
        /// <param name="uri">          The URI. </param>
        /// <param name="httpContent">  Content of the HTTP. </param>
        ///
        /// <returns>   An asynchronous result that yields the patch. </returns>
        public Task<HttpResponseMessage> PatchAsync(Uri uri, HttpContent httpContent)
        {
            return SecureExecute(() => SendRequestAsync(new HttpMethod("PATCH"), uri, httpContent));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, Uri uri, HttpContent content = null)
        {
            if (BaseUri != null) uri = new Uri(BaseUri, uri);
            var request = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = method
            };

            if (content != null) request.Content = content;

            if (UserCredentials?.Token != null) request.Headers.Add("Authorization", Invariant($"Bearer {UserCredentials.Token}"));
            AddOriginalHeadersToRequest(request);
            return HttpClient.SendAsync(request);
        }

        /// <summary>   Secures the execute. </summary>
        ///
        /// <remarks>
        /// Ensure we have a token or get one, then execute.
        /// 
        /// Using a delegate approach for getAsync has the beneficial side effect that the get can be
        /// repeated if there is authorization issue on first try.
        /// </remarks>
        ///
        /// <param name="operation">    The get asynchronous. </param>
        ///
        /// <returns>   Task&lt;HttpResponseMessage&gt;. </returns>
        private async Task<HttpResponseMessage> SecureExecute(Func<Task<HttpResponseMessage>> operation)
        {
            // ensure we have a token or get one, then execute
            try
            {
                HttpResponseMessage response;
                if (PassThroughOnly)
                {
                    response = await operation.Invoke();
                }
                else
                {
                    if (UserCredentials.Token == null)
                    {
                        response = await EnsureSecure();
                        if (!response.IsSuccessStatusCode) return response;
                    }
                    response = await operation.Invoke();
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // refresh and retry
                        response = await EnsureSecure(true);
                        if (response.IsSuccessStatusCode)
                        {
                            response = await operation.Invoke();
                        }
                    }
                }
                return response;
            }
            catch (Exception exception)
            {
                var message = string.Format(CultureInfo.CurrentCulture,
                    "Error in SecureHttpClient\r\nTimestamp: {0}\r\nAn exception of type '{1}' occurred\r\nType: {1}\r\nMessage: {2}\r\nSource: {3}\r\nStack Trace: {4}",
                    DateTime.Now, exception.GetType().Name, exception.Message, exception.Source, exception.StackTrace);
                _logger.LogError(message);
                throw;
            }
        }

        /// <summary>
        /// Ensures the secure.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task<HttpResponseMessage> EnsureSecure(bool refresh = false)
        {
            if (UserCredentials.Token == null || refresh)
            {
                var resourceOwnerResult = await GetResourceOwnerToken();
                if (resourceOwnerResult.IsError)
                {
                    // we would like to sort out security specific error responses from more general server failures
                    if (resourceOwnerResult.ErrorType != ResponseErrorType.Http || resourceOwnerResult.HttpStatusCode == HttpStatusCode.BadRequest)
                    {
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.Unauthorized,
                            Content = new StringContent(resourceOwnerResult.ErrorDescription ?? resourceOwnerResult.Error)
                        };
                    }
                    return new HttpResponseMessage
                    {
                        StatusCode = resourceOwnerResult.HttpStatusCode,
                        Content = new StringContent(resourceOwnerResult.ErrorDescription ?? resourceOwnerResult.Error)
                    };
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>
        /// get token as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;System.String&gt;.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public async Task<string> GetTokenAsync()
        {
            var response = await EnsureSecure();
            return response.IsSuccessStatusCode ? UserCredentials.Token : null;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        private void AddOriginalHeadersToRequest(HttpRequestMessage request)
        {
            if (_passThrougHeaders != null)
            {
                //TODO ignore the host header if it looks like an IP address

                foreach (var header in _passThrougHeaders)
                {
                    if (!request.Headers.Contains(header.Key) && !_ignoreHeaders.Any(x => header.Key.Equals(x, StringComparison.OrdinalIgnoreCase)))
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // By default we ignore the Host header so we can check if we're going to add it here.
                // We're only going to add Hosts which do not look like IP address.
                // i'm not sure if we really need this check, but at the moment we need it for our builds to run
                // our builds do an external call to the deployed imis install over http, and if we pass in the 
                // host address, when it is an IP, we get a SSL certificate chain validation issue

                // UPDATE - This is now using our "IsAzure" switch, i.e. is SSL enabled or not.
                // This setting caused upgrade issues, which we thought were related to the certs being in the private store, but 
                // that did not resolve all the issues
                if (!GlobalSettings.RequireSSL && _passThrougHeaders.TryGetValues("Host", out var hostHeaders))
                {
                    var pattern = @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b"; // looks like an IP address, i.e. 1.1.1.1 or 123.2.33.192
                    var keep = hostHeaders.Where(host => !Regex.Match(host, pattern).Success).ToList();
                    if (keep.Count > 0)
                        request.Headers.Add("Host", keep);
                }
            }
        }

        private async Task<TokenResponse> GetResourceOwnerToken()
        {
                return await GetOauthResourceOwnerToken();

            async Task<TokenResponse> GetOauthResourceOwnerToken()
            {
                var keyValuePairCollection = new Collection<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", UserCredentials.UserName),
                    new KeyValuePair<string, string>("password", UserCredentials.Password)
                };

                var formUrlEncodedContent = new FormUrlEncodedContent(keyValuePairCollection);
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(BaseUri, "Token"),
                    Method = HttpMethod.Post,
                    Content = formUrlEncodedContent
                };
                AddOriginalHeadersToRequest(request);
                var responseMessage = await HttpClient.SendAsync(request);

                //var responseMessage = await HttpClient.PostAsync(new Uri(_baseUri, "Token"), formUrlEncodedContent);

                if (responseMessage.IsSuccessStatusCode)
                {
                    // var resourceOwnerResult = new TokenResponse(await responseMessage.Content.ReadAsStringAsync());
                    var resourceOwnerResult = await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(responseMessage);
                    UserCredentials.Token = resourceOwnerResult.AccessToken;
                    return resourceOwnerResult;
                }

                return await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(responseMessage);


            }
        }
    }
}


