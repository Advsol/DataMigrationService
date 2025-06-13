using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Asi.Core.Interfaces;
using Asi.DataMigrationService.Core.Extensions;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using Castle.DynamicProxy;

namespace Asi.DataMigrationService.Core.Client
{
    public interface ICommonServiceHttpClientFactory
    {
        T Create<T>(Uri baseUri, IUserCredentials userCredentials);
        T Create<T>(Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers);
        ICommonServiceHttpClient Create(string entityTypeName, Uri baseUri, IUserCredentials userCredentials);
        ICommonServiceHttpClient Create(string entityTypeName, Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers);
    }
    public class CommonServiceHttpClientFactory : ICommonServiceHttpClientFactory
    {
        private readonly ISecureHttpClientFactory _secureHttpClientFactory;
        private readonly ProxyGenerator _proxyGenerator;

        public CommonServiceHttpClientFactory(ISecureHttpClientFactory secureHttpClientFactory, ProxyGenerator proxyGenerator)
        {
            _secureHttpClientFactory = secureHttpClientFactory;
            _proxyGenerator = proxyGenerator;
        }
        public T Create<T>(Uri baseUri, IUserCredentials userCredentials)
        {
            return Create<T>(baseUri, userCredentials, null);
        }

        public T Create<T>(Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers)
        {
            var httpClient = _secureHttpClientFactory.Create(baseUri, userCredentials, headers);
            return CreateProxy<T>(httpClient);
        }

        public ICommonServiceHttpClient Create(string entityTypeName, Uri baseUri, IUserCredentials userCredentials)
        {
            return Create(entityTypeName, baseUri, userCredentials, null);
        }

        public ICommonServiceHttpClient Create(string entityTypeName, Uri baseUri, IUserCredentials userCredentials, HttpRequestHeaders headers)
        {
            var httpClient = _secureHttpClientFactory.Create(baseUri, userCredentials, headers);
            return new CommonServiceHttpClient(httpClient, entityTypeName);
        }

        private T CreateProxy<T>(SecureHttpClient httpClient)
        {
            var serviceType = typeof(T);

            if (serviceType.IsInterface)
            {
                if (typeof(ICommonReadOnlyService).IsAssignableFrom(serviceType))
                {
                    var serviceTypeIsGeneric = serviceType.IsGenericType;
                    var commonServiceGenericInterface = serviceTypeIsGeneric
                        ? serviceType
                        : serviceType.GetInterfaces()
                            .FirstOrDefault(p => p.IsGenericType && typeof(ICommonService).IsAssignableFrom(p)) ?? serviceType.GetInterfaces()
                            .FirstOrDefault(p => p.IsGenericType && typeof(ICommonReadOnlyService).IsAssignableFrom(p));
                    if (commonServiceGenericInterface != null)
                    {
                        var contractType = commonServiceGenericInterface.GetGenericArguments()[0];
                        var type = CreateProxy(contractType, serviceTypeIsGeneric ? null : serviceType);
                        var entityTypeName = ServiceToEntityTypeName(type);
                        var obj = Activator.CreateInstance(type, httpClient); ;
                        var proxyInstance = _proxyGenerator.CreateInterfaceProxyWithTargetInterface(serviceType, obj);
                        return (T)proxyInstance;
                    }
                }
                else if (typeof(ICommonServiceContext).IsAssignableFrom(serviceType))
                {
                    var type = CreateProxy(null, serviceType);
                    var entityTypeName = ServiceToEntityTypeName(type);
                    var obj = Activator.CreateInstance(type, httpClient, entityTypeName);

                    var proxyInstance = _proxyGenerator.CreateInterfaceProxyWithTargetInterface(serviceType, obj);
                    return (T)proxyInstance;
                }
            }
            return default;
        }

        private static string ServiceToEntityTypeName(Type type)
        {
            var name = type.Name.Replace("HttpProxy", string.Empty).Replace("Service", string.Empty);
            if (name.StartsWith("I", StringComparison.Ordinal)) name = name.Substring(1);
            return name;
        }

        private static readonly ConcurrentDictionary<string, Type> _proxyRegistry = new ConcurrentDictionary<string, Type>();

        private static Type CreateProxy(Type contractType, Type serviceType)
        {
            var typeSignature = (serviceType ?? contractType).Name + "HttpProxy";
            if (_proxyRegistry.TryGetValue(typeSignature, out var type)) return type;
            lock (_proxyRegistry)
            {
                if (_proxyRegistry.TryGetValue(typeSignature, out type)) return type;
                var an = new AssemblyName("HttpCommonServiceProxies");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
                var baseType = contractType != null ? typeof(CommonServiceHttpClient<>).MakeGenericType(contractType) : typeof(CommonServiceHttpClient);

                var typeBuilder = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    baseType);
                CreatePassThroughConstructors(typeBuilder, baseType);

                // add interface main implementation
                typeBuilder.AddInterfaceImplementation(serviceType);
                // add any methods from the main implementation. Custom methods will in turn call the bse class Execute method.
                var methods = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var methodInfo in methods)
                {
                    var parameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
                    var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType,
                        parameterTypes);

                    var methodIL = methodBuilder.GetILGenerator();
                    methodIL.Emit(OpCodes.Ldarg_0);
                    methodIL.Emit(OpCodes.Ldstr, methodInfo.Name);

                    var pars = methodIL.DeclareLocal(typeof(object[]));

                    methodIL.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                    methodIL.Emit(OpCodes.Newarr, typeof(object));
                    methodIL.Emit(OpCodes.Stloc, pars);

                    for (var i = 0; i < parameterTypes.Length; ++i)
                    {
                        methodIL.Emit(OpCodes.Ldloc, pars);
                        methodIL.Emit(OpCodes.Ldc_I4, i);
                        methodIL.Emit(OpCodes.Ldarg, i + 1);
                        if (parameterTypes[i].IsValueType)
                        {
                            methodIL.Emit(OpCodes.Box, parameterTypes[i]);
                        }
                        methodIL.Emit(OpCodes.Stelem_Ref);
                    }

                    methodIL.Emit(OpCodes.Ldloc, pars);

                    MethodInfo executeMethodInfo = null;
                    if (methodInfo.Name.EndsWith("Async", StringComparison.Ordinal))
                    {
                        // must return Task<IServiceResponse> or Task<IServiceResponse<T>>
                        var taskType = methodInfo.ReturnType;
                        if (typeof(Task).IsAssignableFrom(taskType) && taskType.IsGenericType)
                        {
                            var responseType = taskType.GetGenericArguments()[0];
                            if (typeof(IServiceResponse).IsAssignableFrom(responseType))
                            {
                                if (responseType.IsGenericType)
                                {
                                    // Task<IServiceResponse<T>>
                                    var genType = responseType.GetGenericArguments()[0];
                                    executeMethodInfo = baseType.GetGenericMethod("Execute2Async", new[] { genType }, new[] { typeof(string), typeof(object[]) });
                                }
                                else
                                {
                                    // Task<IServiceResponse>
                                    executeMethodInfo = baseType.GetMethod("ExecuteAsync", new[] { typeof(string), typeof(object[]) });
                                }
                            }
                        }
                    }
                    else
                    {
                        executeMethodInfo = baseType.GetMethod("Execute", new[] { typeof(string), typeof(object[]) });
                    }
                    if (executeMethodInfo != null)
                    {
                        methodIL.Emit(OpCodes.Call, executeMethodInfo);
                        methodIL.Emit(OpCodes.Ret);
                    }
                }
                type = typeBuilder.CreateType();
                _proxyRegistry[typeSignature] = type;
                return type;
            }
        }

        /// <summary>Creates one constructor for each public constructor in the base class. Each constructor simply
        /// forwards its arguments to the base constructor, and matches the base constructor's signature.
        /// Supports optional values, and custom attributes on constructors and parameters.
        /// Does not support n-ary (variadic) constructors</summary>
        private static void CreatePassThroughConstructors(TypeBuilder builder, Type baseType)
        {
            foreach (var constructor in baseType.GetConstructors())
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length > 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false))
                {
                    continue;
                }

                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
                var requiredCustomModifiers = parameters.Select(p => p.GetRequiredCustomModifiers()).ToArray();
                var optionalCustomModifiers = parameters.Select(p => p.GetOptionalCustomModifiers()).ToArray();

                var ctor = builder.DefineConstructor(MethodAttributes.Public, constructor.CallingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                for (var i = 0; i < parameters.Length; ++i)
                {
                    var parameter = parameters[i];
                    var parameterBuilder = ctor.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
                    if (((int)parameter.Attributes & (int)ParameterAttributes.HasDefault) != 0)
                    {
                        parameterBuilder.SetConstant(parameter.RawDefaultValue);
                    }

                    foreach (var attribute in BuildCustomAttributes(parameter.GetCustomAttributesData()))
                    {
                        parameterBuilder.SetCustomAttribute(attribute);
                    }
                }

                foreach (var attribute in BuildCustomAttributes(constructor.GetCustomAttributesData()))
                {
                    ctor.SetCustomAttribute(attribute);
                }

                var emitter = ctor.GetILGenerator();
                emitter.Emit(OpCodes.Nop);

                // Load `this` and call base constructor with arguments
                emitter.Emit(OpCodes.Ldarg_0);
                for (var i = 1; i <= parameters.Length; ++i)
                {
                    emitter.Emit(OpCodes.Ldarg, i);
                }
                emitter.Emit(OpCodes.Call, constructor);

                emitter.Emit(OpCodes.Ret);
            }
        }

        private static CustomAttributeBuilder[] BuildCustomAttributes(IEnumerable<CustomAttributeData> customAttributes)
        {
            return customAttributes.Select(attribute =>
            {
                var attributeArgs = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
                var namedPropertyInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<PropertyInfo>().ToArray();
                var namedPropertyValues = attribute.NamedArguments.Where(a => a.MemberInfo is PropertyInfo).Select(a => a.TypedValue.Value).ToArray();
                var namedFieldInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<FieldInfo>().ToArray();
                var namedFieldValues = attribute.NamedArguments.Where(a => a.MemberInfo is FieldInfo).Select(a => a.TypedValue.Value).ToArray();
                return new CustomAttributeBuilder(attribute.Constructor, attributeArgs, namedPropertyInfos, namedPropertyValues, namedFieldInfos, namedFieldValues);
            }).ToArray();
        }
    }
}
