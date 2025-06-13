using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.Exceptions;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Security;

namespace Asi.DataMigrationService.Core.Extensions
{
    /// <summary>
    /// Class ServiceResponseExtensions.
    /// </summary>
    public static class ServiceResponseExtensions
    {
        /// <summary>
        /// To the HTTP response message.
        /// </summary>
        /// <param name="serviceResponse">The service response.</param>
        /// <returns>HttpResponseMessage.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static HttpResponseMessage ToHttpResponseMessage(this IServiceResponse serviceResponse)
        {
            if (serviceResponse == null) throw new ArgumentNullException(nameof(serviceResponse));
            var response = new HttpResponseMessage { ReasonPhrase = serviceResponse.ReasonPhrase };
            if (serviceResponse.Message != null) response.Content = new StringContent(serviceResponse.Message);
            switch (serviceResponse.StatusCode)
            {
                case StatusCode.Success:
                case StatusCode.Warning:
                    response.StatusCode = HttpStatusCode.OK;
                    break;
                case StatusCode.ValidationError:
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ReasonPhrase = "Validation error";
                    response.Content = new StringContent(JsonConvert.SerializeObject(serviceResponse.ValidationResults, _jsonSerializerSettings));
                    break;
                case StatusCode.NotAuthorized:
                    response.StatusCode = HttpStatusCode.Forbidden;
                    break;
                case StatusCode.NotAuthenticated:
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    break;
                case StatusCode.NotFound:
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;
                case StatusCode.DuplicateKey:
                    response.StatusCode = HttpStatusCode.Conflict;
                    break;
                case StatusCode.ServiceNotAvailable:
                    response.StatusCode = HttpStatusCode.NotImplemented;
                    response.ReasonPhrase = "Service not implemented";
                    break;
                case StatusCode.OperationNotAvailable:
                    response.StatusCode = HttpStatusCode.NotImplemented;
                    response.ReasonPhrase = "Operation not implemented";
                    break;
                case StatusCode.BadRequest:
                    response.StatusCode = HttpStatusCode.BadRequest;
                    break;
                case StatusCode.ServiceError:
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    break;
                case StatusCode.Timeout:
                    response.StatusCode = HttpStatusCode.RequestTimeout;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceResponse), "Unknown status code");
            }

            return response;
        }

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceResponse" /> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>ServiceResponse.</returns>
        public static IServiceResponse ToServiceResponse(this HttpResponseMessage response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            var sr = new ServiceResponse { ReasonPhrase = response.ReasonPhrase };
            sr.StatusCode = TranslateHttpCodeToServiceResponseCode(response);
            return sr;
        }

        /// <summary>   Initializes a new instance of the <see cref="ServiceResponse" /> class. </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="response"> The response. </param>
        ///
        /// <returns>   ServiceResponse. </returns>
        public static IServiceResponse<T> ToServiceResponse<T>(this HttpResponseMessage response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            var sr = new ServiceResponse<T> { ReasonPhrase = response.ReasonPhrase };
            sr.StatusCode = TranslateHttpCodeToServiceResponseCode(response);
            return sr;
        }

        private static StatusCode TranslateHttpCodeToServiceResponseCode(HttpResponseMessage response)
        {
            StatusCode sc;
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.Accepted:
                case HttpStatusCode.NoContent:
                    sc = StatusCode.Success;
                    break;
                case HttpStatusCode.Conflict:
                    sc = StatusCode.DuplicateKey;
                    break;
                case HttpStatusCode.BadRequest:
                    sc = response.ReasonPhrase switch
                    {
                        ("Validation error") => StatusCode.ValidationError,
                        _ => StatusCode.BadRequest,
                    };
                    break;
                case HttpStatusCode.NotFound:
                    sc = StatusCode.NotFound;
                    break;
                case HttpStatusCode.NotImplemented:
                    sc = response.ReasonPhrase switch
                    {
                        ("Service not implemented") => StatusCode.ServiceNotAvailable,
                        ("Operation not implemented") => StatusCode.OperationNotAvailable,
                        _ => StatusCode.ServiceNotAvailable,
                    };
                    break;
                case HttpStatusCode.Forbidden:
                    sc = StatusCode.NotAuthorized;
                    break;
                case HttpStatusCode.Unauthorized:
                    sc = StatusCode.NotAuthenticated;
                    break;
                case HttpStatusCode.RequestTimeout:
                    sc = StatusCode.Timeout;
                    break;
                case HttpStatusCode.InternalServerError:
                    sc = StatusCode.ServiceError;
                    break;
                default:
                    sc = response.IsSuccessStatusCode ? StatusCode.Success : StatusCode.BadRequest;
                    break;
            }
            return sc;
        }

        /// <summary>
        /// Error check response and throw exception if error.
        /// </summary>
        /// <param name="serviceResponse">The service response.</param>
        /// <returns>IServiceResponse.</returns>
        /// <remarks>
        /// NotFound status code is not considered an error.
        /// </remarks>
        public static IServiceResponse ErrorCheck(this IServiceResponse serviceResponse)
        {
            if (serviceResponse == null) throw new ArgumentNullException(nameof(serviceResponse));
            if (serviceResponse.IsSuccessStatusCode || serviceResponse.StatusCode == StatusCode.NotFound)
                return serviceResponse;
            throw ServiceResponseToException(serviceResponse);
        }

        /// <summary>
        /// Services the response to exception.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>Exception.</returns>
        /// <exception cref="System.ArgumentNullException">result</exception>
        public static Exception ServiceResponseToException(IServiceResponse result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            switch (result.StatusCode)
            {
                case StatusCode.DuplicateKey:
                    return new DuplicatePrimaryKeyException(result.Message);
                case StatusCode.ValidationError:
                    return new ValidationException(new ValidationResultsData(result.ValidationResults.Errors, result.ValidationResults.Warnings));
                case StatusCode.NotFound:
                    return new NotFoundException(result.Message);
                case StatusCode.ServiceNotAvailable:
                    return new ServiceNotFoundException(result.Message);
                case StatusCode.OperationNotAvailable:
                    return new NotSupportedException(result.Message);
                case StatusCode.NotAuthorized:
                case StatusCode.NotAuthenticated:
                    return new SecurityException(result.Message);
                case StatusCode.BadRequest:
                    return new ArgumentException(result.Message);
                //case StatusCode.ServiceError:
                default:
                    var r = SecureConnectionException.TestForSecureConnectionException(result.Exception);
                    return r.IsSecureConnectionException
                        ? new SecureConnectionException(result.Message, r.Detail, result.Exception)
                        : (Exception)new ArgumentException(result.Message);
            }
        }
    }
}